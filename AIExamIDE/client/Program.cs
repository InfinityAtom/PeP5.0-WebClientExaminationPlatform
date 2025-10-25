using System.Security.Claims;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Text.Json;
using AIExamIDE.Backend;
using AIExamIDE.Backend.Auth;
using AIExamIDE.Backend.Contracts;
using AIExamIDE.Backend.Data;
using AIExamIDE.Backend.Services;
using AIExamIDE.Components;
using AIExamIDE.Services;
using Models = AIExamIDE.Models;
using BackendExamRoom = AIExamIDE.Backend.Data.ExamRoom;
using BackendExamSession = AIExamIDE.Backend.Data.ExamSession;
using BackendBooking = AIExamIDE.Backend.Data.Booking;
using BackendSubmission = AIExamIDE.Backend.Data.Submission;
using BackendUser = AIExamIDE.Backend.Data.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomLeft;
    // Keep other defaults; can tweak max snackbars or showCloseIcon if needed
});

// Increase Blazor Server (SignalR) hub message size to handle large pasted CSV text reliably
// Default is ~32 KB; we raise to 2 MB to support larger sample datasets.
builder.Services.AddServerSideBlazor().AddHubOptions(o =>
{
    // Adjust this higher if you expect even larger CSV inputs.
    o.MaximumReceiveMessageSize = 2 * 1024 * 1024; // 2 MB
});

builder.Services.AddScoped<ExamState>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<ILocalStorage, LocalStorageService>();
builder.Services.AddScoped<CsvService>();

var examApiBaseRaw = builder.Configuration["ExamApi:BaseUrl"]
    ?? builder.Configuration["ApiBaseUrl"]
    ?? Environment.GetEnvironmentVariable("EXAM_API_BASE_URL")
    ?? Environment.GetEnvironmentVariable("API_BASE_URL")
    ?? builder.Configuration["Urls"];
var examApiBaseUrl = string.IsNullOrWhiteSpace(examApiBaseRaw)
    ? "http://localhost:3000"
    : (examApiBaseRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "http://localhost:3000");

var coreApiBaseRaw = builder.Configuration["CoreApi:BaseUrl"]
    ?? builder.Configuration["CoreBaseUrl"]
    ?? Environment.GetEnvironmentVariable("CORE_API_BASE_URL")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
    ?? builder.Configuration["Urls"];
var coreApiBaseUrl = string.IsNullOrWhiteSpace(coreApiBaseRaw)
    ? "http://localhost:5000"
    : (coreApiBaseRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? "http://localhost:5000");

builder.Services.AddHttpClient("ExamApi", client =>
{
    client.BaseAddress = new Uri(examApiBaseUrl);
});

builder.Services.AddHttpClient("CoreApi", client =>
{
    client.BaseAddress = new Uri(coreApiBaseUrl);
});

builder.Services.AddScoped<ApiClient>();

// Backend services
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = $"Data Source={Path.Combine(dataDir, "ai_exam.db")}";
}
else
{
    var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
    if (!Path.IsPathRooted(sqliteBuilder.DataSource))
    {
        sqliteBuilder.DataSource = Path.Combine(dataDir, Path.GetFileName(sqliteBuilder.DataSource));
    }
    connectionString = sqliteBuilder.ToString();
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

var jwtOptions = new JwtOptions();
builder.Configuration.GetSection("Jwt").Bind(jwtOptions);
if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
{
    throw new InvalidOperationException("JWT secret must be configured in appsettings.");
}
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<AppRepository>();

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = signingKey
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Teacher", policy => policy.RequireRole("teacher"));
    options.AddPolicy("Student", policy => policy.RequireRole("student"));
});

var app = builder.Build();
SchemaInitializer.EnsureDatabase(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

const string teacherPolicy = "Teacher";
const string studentPolicy = "Student";
var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

app.MapPost("/auth/register", async (RegisterRequest request, AppRepository repo, JwtTokenService tokenService) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name) ||
        string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Role))
    {
        return Results.BadRequest(new { error = "Missing required fields" });
    }
    if (request.Role is not ("teacher" or "student"))
    {
        return Results.BadRequest(new { error = "Invalid role" });
    }

    var existing = await repo.GetUserByEmailAsync(request.Email);
    if (existing is not null)
    {
        return Results.Conflict(new { error = "Email already registered" });
    }

    var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10);
    var user = await repo.CreateUserAsync(request.Email, request.Name, hash, request.Role);
    var token = tokenService.CreateToken(user.Id, user.Email, user.Name, user.Role);

    return Results.Ok(new Models.AuthResponse
    {
        Token = token,
        User = user.ToUserInfo()
    });
});

app.MapPost("/auth/login", async (LoginRequest request, AppRepository repo, JwtTokenService tokenService) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Missing credentials" });
    }

    var user = await repo.GetUserByEmailAsync(request.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    var token = tokenService.CreateToken(user.Id, user.Email, user.Name, user.Role);
    return Results.Ok(new Models.AuthResponse
    {
        Token = token,
        User = user.ToUserInfo()
    });
});

app.MapGet("/auth/me", async (ClaimsPrincipal user, AppRepository repo) =>
{
    var userId = user.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var entity = await repo.GetUserByIdAsync(userId.Value);
    if (entity is null) return Results.NotFound(new { error = "User not found" });
    return Results.Ok(entity.ToUserInfo());
}).RequireAuthorization();

app.MapPost("/api/teacher/rooms", async (CreateRoomRequest request, AppRepository repo) =>
{
    var seatmapJson = JsonSerializer.Serialize(request.Seatmap ?? new Models.SeatMap(), jsonOptions);
    var room = await repo.CreateRoomAsync(request.Name, seatmapJson);
    return Results.Ok(room.ToExamRoomDto());
}).RequireAuthorization(teacherPolicy);

app.MapGet("/api/teacher/rooms", async (AppRepository repo) =>
{
    var rooms = await repo.ListRoomsAsync();
    return Results.Ok(rooms.Select(r => r.ToExamRoomDto()));
}).RequireAuthorization(teacherPolicy);

app.MapGet("/api/teacher/rooms/{id:int}", async (int id, AppRepository repo) =>
{
    var room = await repo.GetRoomAsync(id);
    if (room is null) return Results.NotFound(new { error = "Room not found" });
    return Results.Ok(room.ToExamRoomDto());
}).RequireAuthorization(teacherPolicy);

app.MapGet("/api/teacher/rooms/{id:int}/print", async (int id, AppRepository repo) =>
{
    var room = await repo.GetRoomAsync(id);
    if (room is null) return Results.NotFound();
    var dto = room.ToExamRoomDto();
    var desks = dto.Seatmap.Desks ?? new List<Models.Desk>();
    var html = SeatmapPrintTemplate.Render(dto.Name, desks);
    return Results.Content(html, "text/html");
}).RequireAuthorization(teacherPolicy);

app.MapPut("/api/teacher/rooms/{id:int}", async (int id, UpdateRoomRequest request, AppRepository repo) =>
{
    string? seatmapJson = null;
    if (request.Seatmap is not null)
    {
        seatmapJson = JsonSerializer.Serialize(request.Seatmap, jsonOptions);
    }

    var room = await repo.UpdateRoomAsync(id, request.Name, seatmapJson);
    if (room is null) return Results.NotFound(new { error = "Room not found" });
    return Results.Ok(room.ToExamRoomDto());
}).RequireAuthorization(teacherPolicy);

app.MapDelete("/api/teacher/rooms/{id:int}", async (int id, AppRepository repo) =>
{
    var ok = await repo.DeleteRoomAsync(id);
    return ok ? Results.Ok(new { success = true }) : Results.NotFound(new { error = "Room not found" });
}).RequireAuthorization(teacherPolicy);

app.MapGet("/api/teacher/fallback-exam", async (AppRepository repo) =>
{
    var existing = await repo.GetFallbackExamAsync();
    var json = existing?.Json ?? "{}";
    var doc = JsonSerializer.Deserialize<object>(json) ?? new { };
    return Results.Ok(doc);
}).RequireAuthorization(teacherPolicy);

app.MapPost("/api/teacher/fallback-exam", async (object payload, AppRepository repo) =>
{
    var json = JsonSerializer.Serialize(payload, jsonOptions);
    await repo.SetFallbackExamAsync(json);
    return Results.Ok(new { success = true });
}).RequireAuthorization(teacherPolicy);

app.MapPost("/api/teacher/sessions", async (CreateSessionRequest request, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(request.Date))
    {
        return Results.BadRequest(new { error = "Missing roomId or date" });
    }

    var examType = string.IsNullOrWhiteSpace(request.ExamType) ? "java" : request.ExamType!;
    var session = await repo.CreateSessionAsync(
        teacherId.Value,
        request.RoomId,
        request.Title,
        request.Date,
        request.StartTime,
        request.EndTime,
        examType,
        request.AiGenerated ?? true);

    var room = await repo.GetRoomAsync(session.RoomId);
    return Results.Ok(session.ToExamSessionDto(room));
}).RequireAuthorization(teacherPolicy);

app.MapGet("/api/teacher/sessions", async (ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var sessions = await repo.ListSessionsByTeacherAsync(teacherId.Value);
    var rooms = await repo.ListRoomsAsync();
    var lookup = rooms.ToDictionary(r => r.Id);
    var result = sessions.Select(s =>
    {
        lookup.TryGetValue(s.RoomId, out var room);
        var dto = s.ToExamSessionDto(room);
        if (room is not null)
        {
            dto.RoomName = room.Name;
        }
        return dto;
    });
    return Results.Ok(result);
}).RequireAuthorization(teacherPolicy);

    app.MapGet("/api/student/classes", async (ClaimsPrincipal user, AppRepository repo) =>
    {
        var studentId = user.GetUserId();
        if (studentId is null) return Results.Unauthorized();
        var classes = await repo.ListClassesByStudentAsync(studentId.Value);
        // Precompute counts
        var countsTasks = classes.Select(c => repo.CountClassStudentsAsync(c.Id)).ToList();
        var counts = await Task.WhenAll(countsTasks);
        var dtos = classes.Select((c, idx) => new Models.ExamClass
        {
            Id = c.Id,
            Name = c.Name,
            TeacherId = c.TeacherId,
            StudentCount = counts[idx]
        });
        return Results.Ok(dtos);
    }).RequireAuthorization(studentPolicy);

app.MapGet("/api/teacher/sessions/{id:int}", async (int id, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var session = await repo.GetSessionByTeacherAsync(id, teacherId.Value);
    if (session is null) return Results.NotFound(new { error = "Session not found" });
    var room = await repo.GetRoomAsync(session.RoomId);
    return Results.Ok(session.ToExamSessionDto(room));
}).RequireAuthorization(teacherPolicy);

// Teacher: list bookings for a session (relocated top-level)
app.MapGet("/api/teacher/sessions/{id:int}/bookings", async (int id, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (!teacherId.HasValue) return Results.Unauthorized();
    var session = await repo.GetSessionByTeacherAsync(id, teacherId.Value);
    if (session is null) return Results.NotFound(new { error = "Session not found" });
    var room = await repo.GetRoomAsync(session.RoomId);
    var bookings = await repo.ListBookingsBySessionAsync(id);
    var studentIds = bookings.Select(b => b.StudentId).Distinct();
    var students = await repo.ListUsersByIdsAsync(studentIds);
    var studentLookup = students.ToDictionary(u => u.Id, u => u);
    var result = new List<Models.Booking>();
    foreach (var b in bookings)
    {
        var dto = b.ToBookingDto(session, room);
        if (studentLookup.TryGetValue(b.StudentId, out var stu))
        {
            dto.StudentName = stu.Name;
            dto.StudentEmail = stu.Email;
        }
        result.Add(dto);
    }
    return Results.Ok(result);
}).RequireAuthorization(teacherPolicy);

app.MapPut("/api/teacher/sessions/{id:int}", async (int id, UpdateSessionRequest request, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();

    var existing = await repo.GetSessionByTeacherAsync(id, teacherId.Value);
    if (existing is null) return Results.NotFound(new { error = "Session not found" });

    var session = await repo.UpdateSessionAsync(id, request.Title, request.Date, request.StartTime, request.EndTime, request.ExamType, request.AiGenerated, request.Status);
    var room = await repo.GetRoomAsync(session!.RoomId);
    return Results.Ok(session.ToExamSessionDto(room));
}).RequireAuthorization(teacherPolicy);

app.MapDelete("/api/teacher/sessions/{id:int}", async (int id, HttpContext context, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();

    // Robust force flag parsing: accept force=true, force=1, force=on, forceDelete=true; treat empty value as true if key present
    static bool IsTruthy(string? v) => string.IsNullOrWhiteSpace(v) || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1" || v.Equals("on", StringComparison.OrdinalIgnoreCase);
    bool force = false;
    if (context.Request.Query.TryGetValue("force", out var forceVals))
    {
        force = IsTruthy(forceVals.ToString());
    }
    else if (context.Request.Query.TryGetValue("forceDelete", out var forceDeleteVals))
    {
        force = IsTruthy(forceDeleteVals.ToString());
    }

    var existing = await repo.GetSessionByTeacherAsync(id, teacherId.Value);
    if (existing is null) return Results.NotFound(new { error = "Session not found" });

    var bookings = await repo.ListBookingsBySessionAsync(id);
    if (bookings.Any() && !force)
    {
        return Results.Conflict(new {
            error = "Session has active bookings",
            bookingCount = bookings.Count,
            forceParsed = force,
            query = context.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString()),
            message = "Add ?force=true to delete session with bookings"
        });
    }

    var success = await repo.DeleteSessionAsync(id);
    if (!success)
    {
        return Results.NotFound(new { error = "Session not found or could not be deleted" });
    }

    return Results.Ok(new {
        success = true,
        deletedBookings = bookings.Count,
        forceUsed = force,
        message = bookings.Any() ? $"Session deleted along with {bookings.Count} booking(s)" : "Session deleted successfully"
    });
}).RequireAuthorization(teacherPolicy);

app.MapPost("/api/teacher/classes", async (CreateClassRequest request, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Missing name" });
    }
    var classroom = await repo.CreateClassAsync(teacherId.Value, request.Name);
    var count = await repo.CountClassStudentsAsync(classroom.Id);
    return Results.Ok(classroom.ToExamClassDto(count));
}).RequireAuthorization(teacherPolicy);

app.MapGet("/api/teacher/classes", async (ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var classes = await repo.ListClassesAsync(teacherId.Value);
    var result = new List<Models.ExamClass>();
    foreach (var c in classes)
    {
        var count = await repo.CountClassStudentsAsync(c.Id);
        result.Add(c.ToExamClassDto(count));
    }
    return Results.Ok(result);
}).RequireAuthorization(teacherPolicy);

app.MapPut("/api/teacher/classes/{id:int}", async (int id, CreateClassRequest request, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var existing = await repo.GetClassAsync(id);
    if (existing is null || existing.TeacherId != teacherId.Value) return Results.NotFound(new { error = "Class not found" });
    if (string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { error = "Missing name" });
    var updated = await repo.UpdateClassAsync(id, request.Name);
    var count = await repo.CountClassStudentsAsync(id);
    return Results.Ok(updated!.ToExamClassDto(count));
}).RequireAuthorization(teacherPolicy);

app.MapDelete("/api/teacher/classes/{id:int}", async (int id, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var existing = await repo.GetClassAsync(id);
    if (existing is null || existing.TeacherId != teacherId.Value) return Results.NotFound(new { error = "Class not found" });
    await repo.DeleteClassAsync(id);
    return Results.Ok(new { success = true });
}).RequireAuthorization(teacherPolicy);

app.MapGet("/api/teacher/classes/{id:int}/students", async (int id, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var existing = await repo.GetClassAsync(id);
    if (existing is null || existing.TeacherId != teacherId.Value) return Results.NotFound(new { error = "Class not found" });
    var students = await repo.ListClassStudentsAsync(id);
    var result = new List<Models.ClassStudent>();
    foreach (var cs in students)
    {
        var student = await repo.GetUserByIdAsync(cs.StudentId);
        result.Add(new Models.ClassStudent
        {
            ClassId = cs.ClassId,
            StudentId = cs.StudentId,
            Student = student?.ToUserInfo()
        });
    }
    return Results.Ok(result);
}).RequireAuthorization(teacherPolicy);

// Computer configuration endpoints
app.MapGet("/api/teacher/classes/{classId:int}/computers", async (int classId, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var classroom = await repo.GetClassAsync(classId);
    if (classroom is null || classroom.TeacherId != teacherId.Value) return Results.NotFound(new { error = "Class not found" });
    // Placeholder association: return all desks from teacher's rooms; in future filter by class
    var rooms = await repo.ListRoomsAsync();
    var computers = new List<object>();
    foreach (var r in rooms)
    {
        Models.SeatMap seatmap;
        try
        {
            seatmap = string.IsNullOrWhiteSpace(r.SeatmapJson)
                ? new Models.SeatMap()
                : (JsonSerializer.Deserialize<Models.SeatMap>(r.SeatmapJson, jsonOptions) ?? new Models.SeatMap());
        }
        catch
        {
            seatmap = new Models.SeatMap();
        }
        foreach (var d in seatmap.Desks)
        {
            computers.Add(new
            {
                d.Id,
                d.Name,
                d.Hostname,
                d.Ip,
                RoomId = r.Id,
                RoomName = r.Name
            });
        }
    }
    return Results.Ok(computers);
}).RequireAuthorization(teacherPolicy);

app.MapPut("/api/teacher/computers/{deskId}", async (string deskId, UpdateComputerRequest request, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var rooms = await repo.ListRoomsAsync();
    foreach (var r in rooms)
    {
        Models.SeatMap seatmap;
        try
        {
            seatmap = string.IsNullOrWhiteSpace(r.SeatmapJson)
                ? new Models.SeatMap()
                : (JsonSerializer.Deserialize<Models.SeatMap>(r.SeatmapJson, jsonOptions) ?? new Models.SeatMap());
        }
        catch
        {
            seatmap = new Models.SeatMap();
        }
        var desk = seatmap.Desks.FirstOrDefault(d => d.Id == deskId);
        if (desk is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.Hostname)) desk.Hostname = request.Hostname.Trim();
            if (!string.IsNullOrWhiteSpace(request.Ip)) desk.Ip = request.Ip.Trim();
            var updatedSeatmapJson = JsonSerializer.Serialize(seatmap, jsonOptions);
            await repo.UpdateRoomAsync(r.Id, null, updatedSeatmapJson);
            return Results.Ok(new { desk.Id, desk.Name, desk.Hostname, desk.Ip, RoomId = r.Id, RoomName = r.Name });
        }
    }
    return Results.NotFound(new { error = "Computer not found" });
}).RequireAuthorization(teacherPolicy);

app.MapPost("/api/teacher/classes/{id:int}/students", async (int id, AddClassStudentRequest request, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var classroom = await repo.GetClassAsync(id);
    if (classroom is null || classroom.TeacherId != teacherId.Value) return Results.NotFound(new { error = "Class not found" });

    int? studentId = request.StudentId;

    if (studentId is null && !string.IsNullOrWhiteSpace(request.StudentEmail))
    {
        var email = request.StudentEmail.Trim();
        var existing = await repo.GetUserByEmailAsync(email);
        if (existing is not null)
        {
            studentId = existing.Id;
        }
        else
        {
            var name = string.IsNullOrWhiteSpace(request.StudentName) ? email : request.StudentName;
            var tempPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8));
            var hash = BCrypt.Net.BCrypt.HashPassword(tempPassword, workFactor: 8);
            var created = await repo.CreateUserAsync(email, name, hash, "student");
            studentId = created.Id;
        }
    }

    if (studentId is null)
    {
        return Results.BadRequest(new { error = "Missing student reference" });
    }

    await repo.AddClassStudentAsync(id, studentId.Value);
    return Results.Ok(new { success = true, classId = id, studentId = studentId.Value });
}).RequireAuthorization(teacherPolicy);

app.MapDelete("/api/teacher/classes/{id:int}/students/{studentId:int}", async (int id, int studentId, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var classroom = await repo.GetClassAsync(id);
    if (classroom is null || classroom.TeacherId != teacherId.Value) return Results.NotFound(new { error = "Class not found" });
    await repo.RemoveClassStudentAsync(id, studentId);
    return Results.Ok(new { success = true });
}).RequireAuthorization(teacherPolicy);

app.MapPost("/api/teacher/practice-tests", async (CreatePracticeTestRequest request, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Type))
    {
        return Results.BadRequest(new { error = "Missing title or type" });
    }

    string? contentJson = null;
    if (request.Content is not null)
    {
        contentJson = JsonSerializer.Serialize(request.Content, jsonOptions);
    }

    var test = await repo.CreatePracticeTestAsync(teacherId.Value, request.Title, request.Type, request.Prompt, contentJson);
    return Results.Ok(test.ToPracticeTestDto());
}).RequireAuthorization(teacherPolicy);

app.MapGet("/api/teacher/practice-tests", async (ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var tests = await repo.ListPracticeTestsByTeacherAsync(teacherId.Value);
    return Results.Ok(tests.Select(t => t.ToPracticeTestDto()));
}).RequireAuthorization(teacherPolicy);

app.MapPut("/api/teacher/practice-tests/{id:int}", async (int id, CreatePracticeTestRequest request, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Type))
    {
        return Results.BadRequest(new { error = "Missing title or type" });
    }

    string? contentJson = null;
    if (request.Content is not null)
    {
        contentJson = JsonSerializer.Serialize(request.Content, jsonOptions);
    }

    var existing = await repo.GetPracticeTestAsync(id);
    if (existing is null || existing.TeacherId != teacherId.Value) return Results.NotFound(new { error = "Practice test not found" });

    var updated = await repo.UpdatePracticeTestAsync(id, request.Title, request.Type, request.Prompt, contentJson);
    return Results.Ok(updated!.ToPracticeTestDto());
}).RequireAuthorization(teacherPolicy);

app.MapDelete("/api/teacher/practice-tests/{id:int}", async (int id, ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var existing = await repo.GetPracticeTestAsync(id);
    if (existing is null || existing.TeacherId != teacherId.Value) return Results.NotFound(new { error = "Practice test not found" });
    await repo.DeletePracticeTestAsync(id);
    return Results.Ok(new { success = true });
}).RequireAuthorization(teacherPolicy);

app.MapGet("/api/student/practice-tests", async (AppRepository repo) =>
{
    var tests = await repo.ListAllPracticeTestsAsync();
    return Results.Ok(tests.Select(t => t.ToPracticeTestDto()));
}).RequireAuthorization(studentPolicy);

app.MapGet("/api/student/practice-tests/{id:int}", async (int id, AppRepository repo) =>
{
    var test = await repo.GetPracticeTestAsync(id);
    if (test is null) return Results.NotFound(new { error = "Practice test not found" });
    return Results.Ok(test.ToPracticeTestDto());
}).RequireAuthorization(studentPolicy);

app.MapPost("/api/student/practice-tests/{id:int}/submit", async (int id, SubmitPracticeTestRequest request, ClaimsPrincipal user, AppRepository repo) =>
{
    var studentId = user.GetUserId();
    if (studentId is null) return Results.Unauthorized();

    var test = await repo.GetPracticeTestAsync(id);
    if (test is null) return Results.NotFound(new { error = "Practice test not found" });

    int? score = null;
    object evaluation = new { };

    if (test.Type == "mcq" && !string.IsNullOrWhiteSpace(test.ContentJson))
    {
        // Extended schema attempt first (list of extended question objects)
        try
        {
            var jsonDoc = JsonDocument.Parse(test.ContentJson!);
            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var answersList = request.Answers ?? new List<int>();
                // Prefer explicit MultiAnswers contract
                var multiSelections = request.MultiAnswers is not null
                    ? request.MultiAnswers.ToDictionary(k => k.Key, v => v.Value.Distinct().OrderBy(x => x).ToList())
                    : new Dictionary<int, List<int>>();
                JsonElement? dataRoot = null;
                if (request.Data is JsonElement directElem)
                {
                    dataRoot = directElem;
                }
                else if (request.Data is not null)
                {
                    try
                    {
                        var raw = JsonSerializer.Serialize(request.Data, jsonOptions);
                        using var doc = JsonDocument.Parse(raw);
                        dataRoot = doc.RootElement.Clone();
                    }
                    catch { /* ignore */ }
                }
                if (multiSelections.Count == 0 && dataRoot.HasValue && dataRoot.Value.ValueKind == JsonValueKind.Object)
                {
                    // Try primary key 'multiple'
                    JsonElement multElem;
                    bool foundMultiple = false;
                    if (dataRoot.Value.TryGetProperty("multiple", out multElem)) foundMultiple = true;
                    else if (dataRoot.Value.TryGetProperty("Multiple", out multElem)) foundMultiple = true; // case variant
                    else if (dataRoot.Value.TryGetProperty("multiAnswers", out multElem)) foundMultiple = true; // alternate naming
                    else if (dataRoot.Value.TryGetProperty("multi", out multElem)) foundMultiple = true; // short

                    if (foundMultiple)
                    {
                        if (multElem.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in multElem.EnumerateObject())
                            {
                                if (int.TryParse(prop.Name, out var qIndex))
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.Array)
                                    {
                                        var arr = prop.Value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number).Select(x => x.GetInt32()).Distinct().ToList();
                                        if (arr.Count > 0 || !multiSelections.ContainsKey(qIndex))
                                            multiSelections[qIndex] = arr;
                                    }
                                    else if (prop.Value.ValueKind == JsonValueKind.String)
                                    {
                                        // Support comma-separated string of indices
                                        var text = prop.Value.GetString() ?? "";
                                        var arr = text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                      .Select(s => int.TryParse(s.Trim(), out var v) ? v : -1)
                                                      .Where(v => v >= 0)
                                                      .Distinct().ToList();
                                        if (arr.Count > 0 || !multiSelections.ContainsKey(qIndex))
                                            multiSelections[qIndex] = arr;
                                    }
                                }
                            }
                        }
                        else if (multElem.ValueKind == JsonValueKind.Array)
                        {
                            // Treat as positional array-of-arrays: [[1,2],[0],[...]] matching question order.
                            int qi = 0;
                            foreach (var arrElem in multElem.EnumerateArray())
                            {
                                if (arrElem.ValueKind == JsonValueKind.Array)
                                {
                                    var arr = arrElem.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number).Select(x => x.GetInt32()).Distinct().ToList();
                                    if (arr.Count > 0 || !multiSelections.ContainsKey(qi))
                                        multiSelections[qi] = arr;
                                }
                                qi++;
                            }
                        }
                    }
                }

                var questionEvaluations = new List<object>();
                int totalPointsPossible = 0;
                int totalPointsAwarded = 0;
                int questionIndex = 0;

                foreach (var qElem in jsonDoc.RootElement.EnumerateArray())
                {
                    // Gather fields with resilience to legacy schema
                    string questionText = qElem.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? "" : (qElem.TryGetProperty("question", out var legacyText) ? legacyText.GetString() ?? "" : "");
                    string questionType = qElem.TryGetProperty("questionType", out var typeProp) ? (typeProp.GetString() ?? "single") : "single";
                    int points = qElem.TryGetProperty("points", out var ptsProp) && ptsProp.ValueKind == JsonValueKind.Number ? ptsProp.GetInt32() : 1;
                    int penalty = qElem.TryGetProperty("penalty", out var penProp) && penProp.ValueKind == JsonValueKind.Number ? penProp.GetInt32() : 0;
                    var options = new List<string>();
                    if (qElem.TryGetProperty("options", out var optsProp) && optsProp.ValueKind == JsonValueKind.Array)
                    {
                        options = optsProp.EnumerateArray().Where(o => o.ValueKind == JsonValueKind.String).Select(o => o.GetString() ?? "").ToList();
                    }

                    // Correct answer(s) extraction
                    int correctSingle = qElem.TryGetProperty("correctAnswer", out var caProp) && caProp.ValueKind == JsonValueKind.Number ? caProp.GetInt32() : (qElem.TryGetProperty("correctIndex", out var legacyIdx) && legacyIdx.ValueKind == JsonValueKind.Number ? legacyIdx.GetInt32() : 0);
                    var correctMultiple = new List<int>();
                    if (qElem.TryGetProperty("correctAnswers", out var cmProp) && cmProp.ValueKind == JsonValueKind.Array)
                    {
                        correctMultiple = cmProp.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number).Select(x => x.GetInt32()).Distinct().OrderBy(x => x).ToList();
                    }

                    // Student answers
                    int studentSingle = (questionType == "multiple") ? -1 : (questionIndex < answersList.Count ? answersList[questionIndex] : -1);
                    var studentMultiple = multiSelections.ContainsKey(questionIndex) ? multiSelections[questionIndex] : new List<int>();

                    int pointsAwarded = 0;
                    var mistakes = new List<string>();

                    if (questionType == "single" || questionType == "truefalse")
                    {
                        bool correct = studentSingle == correctSingle && studentSingle >= 0 && studentSingle < options.Count;
                        if (correct)
                        {
                            pointsAwarded = points;
                        }
                        else
                        {
                            if (studentSingle == -1)
                                mistakes.Add("No answer selected");
                            else
                                mistakes.Add("Incorrect answer");
                            if (penalty > 0) pointsAwarded = Math.Max(0, points - penalty); // simple penalty model (could be 0 - penalty, but keep non-negative)
                        }
                    }
                    else if (questionType == "multiple")
                    {
                        if (correctMultiple.Count == 0)
                        {
                            mistakes.Add("No correct answers defined");
                        }
                        else
                        {
                            var studentSet = studentMultiple.Distinct().Where(i => i >= 0 && i < options.Count).ToHashSet();
                            var correctSet = correctMultiple.ToHashSet();
                            int correctSelected = studentSet.Count(i => correctSet.Contains(i));
                            int incorrectSelected = studentSet.Count(i => !correctSet.Contains(i));
                            int missed = correctSet.Count(i => !studentSet.Contains(i));

                            double fraction = correctSet.Count > 0 ? (double)correctSelected / correctSet.Count : 0.0;
                            double rawPoints = points * fraction;
                            double penaltyPoints = penalty * incorrectSelected;
                            pointsAwarded = (int)Math.Round(Math.Max(0, rawPoints - penaltyPoints));

                            if (incorrectSelected > 0) mistakes.Add($"Selected {incorrectSelected} incorrect option(s)");
                            if (missed > 0) mistakes.Add($"Missed {missed} correct option(s)");
                            if (studentSet.Count == 0) mistakes.Add("No options selected");
                        }
                    }

                    totalPointsPossible += points;
                    totalPointsAwarded += pointsAwarded;

                    questionEvaluations.Add(new
                    {
                        index = questionIndex,
                        type = questionType,
                        text = questionText,
                        options,
                        studentSingleAnswer = studentSingle,
                        studentMultipleAnswers = studentMultiple.OrderBy(x => x).ToList(),
                        correctSingleAnswer = correctSingle,
                        correctMultipleAnswers = correctMultiple,
                        pointsPossible = points,
                        pointsAwarded,
                        mistakes
                    });

                    questionIndex++;
                }

                if (questionIndex > 0)
                {
                    score = (int)Math.Round((double)totalPointsAwarded / Math.Max(1, totalPointsPossible) * 100);
                    evaluation = new
                    {
                        totalQuestions = questionIndex,
                        totalPointsPossible,
                        totalPointsAwarded,
                        percentage = score,
                        questions = questionEvaluations
                    };
                }
            }
            else
            {
                // Legacy wrapped content fallback
                var content = JsonSerializer.Deserialize<Models.PracticeTestContent>(test.ContentJson!);
                var items = content?.Items ?? new List<Models.PracticeTestItem>();
                if (items.Count > 0)
                {
                    var answers = request.Answers ?? new List<int>();
                    var correct = items.Select((item, index) => (item, index))
                        .Count(pair => pair.item.CorrectIndex.HasValue && pair.index < answers.Count && answers[pair.index] == pair.item.CorrectIndex.Value);
                    score = (int)Math.Round((double)correct / items.Count * 100);
                    evaluation = new { total = items.Count, correct, score };
                }
            }
        }
        catch
        {
            evaluation = new { error = "Unable to evaluate submission" };
        }
    }

    var submission = await repo.CreatePracticeSubmissionAsync(id, studentId.Value,
        request.Data is null ? null : JsonSerializer.Serialize(request.Data, jsonOptions),
        JsonSerializer.Serialize(evaluation, jsonOptions),
        score);

    return Results.Ok(submission.ToPracticeSubmissionDto());
}).RequireAuthorization(studentPolicy);

app.MapGet("/api/student/sessions", async (AppRepository repo) =>
{
    var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var sessions = await repo.ListScheduledSessionsFromDateAsync(today);
    var rooms = await repo.ListRoomsAsync();
    var roomLookup = rooms.ToDictionary(r => r.Id);

    var result = new List<Models.ExamSession>();
    foreach (var session in sessions)
    {
        roomLookup.TryGetValue(session.RoomId, out var room);
        var dto = session.ToExamSessionDto(room);
        var booked = await repo.ListBookedSeatsAsync(session.Id);
        dto.BookedSeats = booked;
        result.Add(dto);
    }

    return Results.Ok(result);
}).RequireAuthorization(studentPolicy);

app.MapPost("/api/student/bookings", async (BookSeatRequest request, ClaimsPrincipal user, AppRepository repo) =>
{
    var studentId = user.GetUserId();
    if (studentId is null) return Results.Unauthorized();
    if (request.SessionId <= 0 || string.IsNullOrWhiteSpace(request.SeatId))
    {
        return Results.BadRequest(new { error = "Missing sessionId or seatId" });
    }

    var session = await repo.GetSessionAsync(request.SessionId);
    if (session is null) return Results.NotFound(new { error = "Session not found" });

    var existing = await repo.FindExistingBookingAsync(request.SessionId, request.SeatId, studentId.Value);
    if (existing is not null)
    {
        return Results.Conflict(new { error = "Seat or student already booked for this session" });
    }

    var booking = await repo.CreateBookingAsync(request.SessionId, studentId.Value, request.SeatId);
    var room = await repo.GetRoomAsync(session.RoomId);
    var dto = booking.ToBookingDto(session, room);
    return Results.Ok(dto);
}).RequireAuthorization(studentPolicy);

app.MapPut("/api/student/bookings/{id:int}", async (int id, UpdateBookingRequest request, ClaimsPrincipal user, AppRepository repo) =>
{
    var studentId = user.GetUserId();
    if (studentId is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(request.SeatId)) return Results.BadRequest(new { error = "SeatId required" });

    var existing = await repo.GetBookingByIdAsync(id);
    if (existing is null || existing.StudentId != studentId.Value)
        return Results.NotFound(new { error = "Booking not found" });

    // Try update (returns null if seat taken or invalid)
    var updated = await repo.UpdateBookingSeatAsync(id, studentId.Value, request.SeatId);
    if (updated is null)
        return Results.Conflict(new { error = "Seat already booked or invalid" });

    var session = await repo.GetSessionAsync(updated.SessionId);
    BackendExamRoom? room = session is null ? null : await repo.GetRoomAsync(session.RoomId);
    return Results.Ok(updated.ToBookingDto(session, room));
}).RequireAuthorization(studentPolicy);

app.MapDelete("/api/student/bookings/{id:int}", async (int id, ClaimsPrincipal user, AppRepository repo) =>
{
    var studentId = user.GetUserId();
    if (studentId is null) return Results.Unauthorized();
    var success = await repo.DeleteBookingAsync(id, studentId.Value);
    if (!success) return Results.NotFound(new { error = "Booking not found" });
    return Results.NoContent();
}).RequireAuthorization(studentPolicy);

app.MapGet("/api/student/bookings", async (ClaimsPrincipal user, AppRepository repo) =>
{
    var studentId = user.GetUserId();
    if (studentId is null) return Results.Unauthorized();
    var bookings = await repo.ListBookingsByStudentAsync(studentId.Value);
    var result = new List<Models.Booking>();
    foreach (var booking in bookings)
    {
        var session = await repo.GetSessionAsync(booking.SessionId);
        BackendExamRoom? room = null;
        if (session is not null)
        {
            room = await repo.GetRoomAsync(session.RoomId);
        }
        result.Add(booking.ToBookingDto(session, room));
    }
    return Results.Ok(result);
}).RequireAuthorization(studentPolicy);

app.MapGet("/api/teacher/submissions", async (int? sessionId, AppRepository repo) =>
{
    var submissions = await repo.ListSubmissionsAsync();
    var result = new List<Models.Submission>();
    foreach (var submission in submissions)
    {
        if (sessionId.HasValue)
        {
            var booking = submission.BookingId.HasValue
                ? await repo.GetBookingByIdAsync(submission.BookingId.Value)
                : null;
            if (booking?.SessionId != sessionId.Value)
            {
                continue;
            }
            var student = booking is null ? null : await repo.GetUserByIdAsync(booking.StudentId);
            result.Add(submission.ToSubmissionDto(booking, student));
        }
        else
        {
            BackendBooking? booking = null;
            BackendUser? student = null;
            if (submission.BookingId.HasValue)
            {
                booking = await repo.GetBookingByIdAsync(submission.BookingId.Value);
                student = booking is null ? null : await repo.GetUserByIdAsync(booking.StudentId);
            }
            result.Add(submission.ToSubmissionDto(booking, student));
        }
    }
    return Results.Ok(result);
}).RequireAuthorization(teacherPolicy);

app.MapGet("/api/teacher/submissions/{id:int}", async (int id, AppRepository repo) =>
{
    var submission = await repo.GetSubmissionAsync(id);
    if (submission is null) return Results.NotFound(new { error = "Submission not found" });
    BackendBooking? booking = null;
    BackendUser? student = null;
    if (submission.BookingId.HasValue)
    {
        booking = await repo.GetBookingByIdAsync(submission.BookingId.Value);
        if (booking is not null)
        {
            student = await repo.GetUserByIdAsync(booking.StudentId);
        }
    }
    return Results.Ok(submission.ToSubmissionDto(booking, student));
}).RequireAuthorization(teacherPolicy);

app.MapPatch("/api/teacher/submissions/{id:int}", async (int id, UpdateSubmissionRequest request, AppRepository repo) =>
{
    string? evaluationJson = null;
    if (request.PerTask is not null)
    {
        evaluationJson = JsonSerializer.Serialize(new { perTask = request.PerTask }, jsonOptions);
    }

    string? feedbackJson = null;
    if (request.Feedback is not null)
    {
        feedbackJson = JsonSerializer.Serialize(request.Feedback, jsonOptions);
    }

    var submission = await repo.UpdateSubmissionAsync(id, request.GradeFinal, evaluationJson, feedbackJson);
    if (submission is null) return Results.NotFound(new { error = "Submission not found" });

    BackendBooking? booking = null;
    BackendUser? student = null;
    if (submission.BookingId.HasValue)
    {
        booking = await repo.GetBookingByIdAsync(submission.BookingId.Value);
        if (booking is not null)
        {
            student = await repo.GetUserByIdAsync(booking.StudentId);
        }
    }

    return Results.Ok(submission.ToSubmissionDto(booking, student));
}).RequireAuthorization(teacherPolicy);

app.MapGet("/api/teacher/reports", async (ClaimsPrincipal user, AppRepository repo) =>
{
    var teacherId = user.GetUserId();
    if (teacherId is null) return Results.Unauthorized();
    var counts = await repo.CountsByTeacherAsync(teacherId.Value);
    return Results.Ok(counts.ToTeacherReportDto());
}).RequireAuthorization(teacherPolicy);

// Student search by email substring (teacher)
app.MapGet("/api/teacher/students/search", async (string? q, AppRepository repo) =>
{
    var query = (q ?? string.Empty).Trim();
    // Return empty list if query too short to avoid dumping all users
    if (query.Length < 2)
    {
        return Results.Ok(new List<Models.UserInfo>()); // require at least 2 chars
    }
    var all = await repo.ListUsersByRoleAsync("student");
    var matches = all
        .Where(u => (!string.IsNullOrWhiteSpace(u.Email) && u.Email.Contains(query, StringComparison.OrdinalIgnoreCase))
                 || (!string.IsNullOrWhiteSpace(u.Name) && u.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
        .Take(25) // limit result set
        .Select(u => u.ToUserInfo())
        .ToList();
    return Results.Ok(matches);
}).RequireAuthorization(teacherPolicy);

// List all students (teacher) - pageless for now (consider pagination if large)
app.MapGet("/api/teacher/students/all", async (AppRepository repo) =>
{
    var all = await repo.ListUsersByRoleAsync("student");
    var list = all
        .OrderBy(u => u.Name)
        .Select(u => u.ToUserInfo())
        .ToList();
    return Results.Ok(list);
}).RequireAuthorization(teacherPolicy);

// Endpoint for Node exam backend to persist submissions
app.MapPost("/internal/submissions", async (CreateSubmissionRequest request, AppRepository repo) =>
{
    var submission = await repo.CreateSubmissionAsync(
        request.BookingId,
        request.Files is null ? null : JsonSerializer.Serialize(request.Files, jsonOptions),
        request.Tasks is null ? null : JsonSerializer.Serialize(request.Tasks, jsonOptions),
        request.Csvs is null ? null : JsonSerializer.Serialize(request.Csvs, jsonOptions),
        request.Evaluation is null ? null : JsonSerializer.Serialize(request.Evaluation, jsonOptions),
        request.GradeFinal,
        request.Feedback is null ? null : JsonSerializer.Serialize(request.Feedback, jsonOptions));

    return Results.Ok(new { id = submission.Id });
});

app.MapPost("/internal/evaluations", async (UpdateEvaluationRequest request, AppRepository repo) =>
{
    if (request.BookingId <= 0)
    {
        return Results.BadRequest(new { error = "Invalid bookingId" });
    }

    var evaluationJson = JsonSerializer.Serialize(request.Evaluation, jsonOptions);
    var feedbackJson = request.Feedback is null ? null : JsonSerializer.Serialize(request.Feedback, jsonOptions);

    var submission = await repo.GetLatestSubmissionByBookingAsync(request.BookingId);
    if (submission is null)
    {
        submission = await repo.CreateSubmissionAsync(request.BookingId, null, null, null, evaluationJson, request.GradeFinal, feedbackJson);
    }
    else
    {
        submission = await repo.UpdateSubmissionAsync(submission.Id, request.GradeFinal, evaluationJson, feedbackJson);
    }

    BackendBooking? booking = await repo.GetBookingByIdAsync(request.BookingId);
    BackendUser? teacher = null;
    BackendUser? student = null;
    if (booking is not null)
    {
        var session = await repo.GetSessionAsync(booking.SessionId);
        if (session is not null)
        {
            teacher = await repo.GetUserByIdAsync(session.TeacherId);
        }
        student = await repo.GetUserByIdAsync(booking.StudentId);
    }

    return Results.Ok(new
    {
        submissionId = submission?.Id,
        teacherEmail = teacher?.Email,
        teacherName = teacher?.Name,
        studentEmail = student?.Email,
        studentName = student?.Name
    });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
