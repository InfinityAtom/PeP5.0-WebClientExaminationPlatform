using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIExamIDE.Backend.Contracts;
using AIExamIDE.Models;

namespace AIExamIDE.Services;

public class ApiClient
{
    private readonly HttpClient _examHttp;
    private readonly HttpClient _coreHttp;
    private readonly AuthState _auth;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(IHttpClientFactory httpClientFactory, AuthState auth)
    {
        _examHttp = httpClientFactory.CreateClient("ExamApi");
        _coreHttp = httpClientFactory.CreateClient("CoreApi");
        _auth = auth;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    #region Helpers

    private void ApplyAuth(HttpClient client)
    {
        if (!string.IsNullOrEmpty(_auth.Token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
        }
        else
        {
            client.DefaultRequestHeaders.Authorization = null;
        }
    }

    private StringContent CreateJsonContent(object value) =>
        new(JsonSerializer.Serialize(value, _jsonOptions), Encoding.UTF8, "application/json");

    private async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _jsonOptions)!;
    }

    private async Task<T?> ReadNullableAsync<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        ApplyAuth(_coreHttp);
        var response = await _coreHttp.GetAsync("/auth/me");
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }
        return await ReadNullableAsync<UserInfo>(response);
    }

    #endregion

    #region Exam Engine (Node)

    public async Task<ExamResponse> GenerateExamAsync()
    {
        ApplyAuth(_examHttp);
        var response = await _examHttp.PostAsync("/exam", null);
        return await ReadAsync<ExamResponse>(response);
    }

    public async Task<RunResponse> RunCodeAsync(List<ExamFile> files, string? mainFile = null)
    {
        ApplyAuth(_examHttp);
        var payload = new { Files = files, MainFile = mainFile };
        var response = await _examHttp.PostAsync("/run", CreateJsonContent(payload));
        return await ReadAsync<RunResponse>(response);
    }

    public async Task ResetExamAsync()
    {
        ApplyAuth(_examHttp);
        var response = await _examHttp.PostAsync("/reset", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task SubmitExamAsync(List<ExamFile> files)
    {
        ApplyAuth(_examHttp);
        var payload = new { Files = files };
        var response = await _examHttp.PostAsync("/submit", CreateJsonContent(payload));
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> SubmitExamWithBookingAsync(List<ExamFile> files, int? bookingId = null)
    {
        ApplyAuth(_examHttp);
        var payload = new { Files = files, BookingId = bookingId };
        var response = await _examHttp.PostAsync("/submit", CreateJsonContent(payload));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<EvaluationResult> EvaluateAsync(List<ExamFile> files, ExamMetadata exam)
    {
        ApplyAuth(_examHttp);
        var csvNames = files.Where(f => !f.IsDirectory && f.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                            .Select(f => f.Name)
                            .Distinct()
                            .ToList();

        var payload = new
        {
            Files = files,
            Exam = new
            {
                domain = exam.Domain,
                overview = exam.Overview,
                csv_files = csvNames
            }
        };

        var response = await _examHttp.PostAsync("/evaluate", CreateJsonContent(payload));
        return await ReadAsync<EvaluationResult>(response);
    }

    public async Task<EvaluationResult> EvaluateSubmissionWithBookingAsync(List<ExamFile> files, ExamMetadata exam, int? bookingId = null)
    {
        ApplyAuth(_examHttp);
        var csvNames = files.Where(f => !f.IsDirectory && f.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                            .Select(f => f.Name)
                            .Distinct()
                            .ToList();

        var payload = new
        {
            Files = files,
            Exam = new
            {
                domain = exam.Domain,
                overview = exam.Overview,
                csv_files = csvNames
            },
            BookingId = bookingId
        };

        var response = await _examHttp.PostAsync("/evaluate_submission", CreateJsonContent(payload));
        return await ReadAsync<EvaluationResult>(response);
    }

    #endregion

    #region Authentication

    public async Task<AuthResponse> RegisterAsync(string email, string name, string password, string role)
    {
        _coreHttp.DefaultRequestHeaders.Authorization = null;
        var payload = new { email, name, password, role };
        var response = await _coreHttp.PostAsync("/auth/register", CreateJsonContent(payload));
        var auth = await ReadAsync<AuthResponse>(response);
        await _auth.SetAuthAsync(auth.Token, auth.User);
        return auth;
    }

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        _coreHttp.DefaultRequestHeaders.Authorization = null;
        var payload = new { email, password };
        var response = await _coreHttp.PostAsync("/auth/login", CreateJsonContent(payload));
        var auth = await ReadAsync<AuthResponse>(response);
        await _auth.SetAuthAsync(auth.Token, auth.User);
        return auth;
    }

    #endregion

    #region Teacher - Rooms

    public async Task<List<ExamRoom>> GetRoomsAsync()
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<List<ExamRoom>>(await _coreHttp.GetAsync("/api/teacher/rooms"));
    }

    public async Task<ExamRoom> CreateRoomAsync(ExamRoom room)
    {
        ApplyAuth(_coreHttp);
        var payload = new { name = room.Name, seatmap = room.Seatmap };
        return await ReadAsync<ExamRoom>(await _coreHttp.PostAsync("/api/teacher/rooms", CreateJsonContent(payload)));
    }

    public async Task<ExamRoom> GetRoomAsync(int id)
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<ExamRoom>(await _coreHttp.GetAsync($"/api/teacher/rooms/{id}"));
    }

    public async Task<ExamRoom> UpdateRoomAsync(int id, ExamRoom room)
    {
        ApplyAuth(_coreHttp);
        var payload = new { name = room.Name, seatmap = room.Seatmap };
        return await ReadAsync<ExamRoom>(await _coreHttp.PutAsync($"/api/teacher/rooms/{id}", CreateJsonContent(payload)));
    }

    public async Task<bool> DeleteRoomAsync(int id)
    {
        ApplyAuth(_coreHttp);
        var response = await _coreHttp.DeleteAsync($"/api/teacher/rooms/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        response.EnsureSuccessStatusCode();
        return true;
    }

    #endregion

    #region Teacher - Sessions

    public async Task<ExamSession> CreateSessionAsync(int roomId, string date, string? title = null, string? start = null, string? end = null, bool aiGenerated = true)
    {
        ApplyAuth(_coreHttp);
        var payload = new
        {
            roomId,
            title,
            date,
            startTime = start,
            endTime = end,
            examType = "java",
            aiGenerated
        };
        return await ReadAsync<ExamSession>(await _coreHttp.PostAsync("/api/teacher/sessions", CreateJsonContent(payload)));
    }

    public async Task<List<ExamSession>> GetMySessionsAsync()
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<List<ExamSession>>(await _coreHttp.GetAsync("/api/teacher/sessions"));
    }

    public async Task<ExamSession> GetSessionAsync(int id)
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<ExamSession>(await _coreHttp.GetAsync($"/api/teacher/sessions/{id}"));
    }

    public async Task<List<Booking>> GetSessionBookingsAsync(int sessionId)
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<List<Booking>>(await _coreHttp.GetAsync($"/api/teacher/sessions/{sessionId}/bookings"));
    }

    public async Task<ExamSession> UpdateSessionAsync(int id, ExamSession session)
    {
        ApplyAuth(_coreHttp);
        var payload = new
        {
            title = session.Title,
            date = session.Date,
            startTime = session.StartTime,
            endTime = session.EndTime,
            examType = session.ExamType,
            aiGenerated = session.AiGenerated
        };
        return await ReadAsync<ExamSession>(await _coreHttp.PutAsync($"/api/teacher/sessions/{id}", CreateJsonContent(payload)));
    }

    public async Task<bool> DeleteSessionAsync(int id, bool forceDelete = false)
    {
        ApplyAuth(_coreHttp);
        var url = $"/api/teacher/sessions/{id}";
        if (forceDelete)
        {
            url += "?force=true";
        }
        var response = await _coreHttp.DeleteAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        response.EnsureSuccessStatusCode();
        return true;
    }

    #endregion

    #region Teacher - Submissions & Reports

    public async Task<List<Submission>> GetSubmissionsAsync(int? sessionId = null)
    {
        ApplyAuth(_coreHttp);
        var url = "/api/teacher/submissions";
        if (sessionId.HasValue)
        {
            url += $"?sessionId={sessionId}";
        }
        return await ReadAsync<List<Submission>>(await _coreHttp.GetAsync(url));
    }

    public async Task<Submission> GetSubmissionAsync(int id)
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<Submission>(await _coreHttp.GetAsync($"/api/teacher/submissions/{id}"));
    }

    public async Task<Submission> UpdateSubmissionAsync(int id, int? finalGrade = null, Dictionary<string, SubmissionTaskAdjustment>? perTask = null, object? feedback = null)
    {
        ApplyAuth(_coreHttp);
        var payload = new UpdateSubmissionRequest(finalGrade, perTask, feedback);
        return await ReadAsync<Submission>(await _coreHttp.PatchAsync($"/api/teacher/submissions/{id}", CreateJsonContent(payload)));
    }

    public async Task<TeacherReport> GetReportsAsync()
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<TeacherReport>(await _coreHttp.GetAsync("/api/teacher/reports"));
    }

    #endregion

    #region Teacher - Fallback Exam

    public async Task<object?> GetFallbackExamAsync()
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<object?>(await _coreHttp.GetAsync("/api/teacher/fallback-exam"));
    }

    public async Task<bool> SetFallbackExamAsync(object examData)
    {
        ApplyAuth(_coreHttp);
        var response = await _coreHttp.PostAsync("/api/teacher/fallback-exam", CreateJsonContent(examData));
        response.EnsureSuccessStatusCode();
        return true;
    }

    #endregion

    #region Teacher - Classes

    public async Task<List<ExamClass>> GetClassesAsync()
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<List<ExamClass>>(await _coreHttp.GetAsync("/api/teacher/classes"));
    }

    public async Task<ExamClass> CreateClassAsync(string name)
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<ExamClass>(await _coreHttp.PostAsync("/api/teacher/classes", CreateJsonContent(new CreateClassRequest(name))));
    }

    public async Task<ExamClass> UpdateClassAsync(int id, string name)
    {
        ApplyAuth(_coreHttp);
        var payload = new { name };
        return await ReadAsync<ExamClass>(await _coreHttp.PutAsync($"/api/teacher/classes/{id}", CreateJsonContent(payload)));
    }

    public async Task<bool> DeleteClassAsync(int id)
    {
        ApplyAuth(_coreHttp);
        var response = await _coreHttp.DeleteAsync($"/api/teacher/classes/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<List<ClassStudent>> GetClassStudentsAsync(int classId)
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<List<ClassStudent>>(await _coreHttp.GetAsync($"/api/teacher/classes/{classId}/students"));
    }

    public async Task AddStudentToClassAsync(int classId, int? studentId = null, string? studentEmail = null, string? studentName = null)
    {
        ApplyAuth(_coreHttp);
        var payload = new AddClassStudentRequest(studentId, studentEmail, studentName);
        var response = await _coreHttp.PostAsync($"/api/teacher/classes/{classId}/students", CreateJsonContent(payload));
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveStudentFromClassAsync(int classId, int studentId)
    {
        ApplyAuth(_coreHttp);
        var response = await _coreHttp.DeleteAsync($"/api/teacher/classes/{classId}/students/{studentId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<UserInfo>> SearchStudentsAsync(string query)
    {
        ApplyAuth(_coreHttp);
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return new List<UserInfo>();
        var url = $"/api/teacher/students/search?q={Uri.EscapeDataString(query.Trim())}";
        return await ReadAsync<List<UserInfo>>(await _coreHttp.GetAsync(url));
    }

    public async Task<List<UserInfo>> GetAllStudentsAsync()
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<List<UserInfo>>(await _coreHttp.GetAsync("/api/teacher/students/all"));
    }

    #endregion

    #region Teacher - Practice Tests

    public async Task<List<PracticeTest>> GetPracticeTestsAsync()
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<List<PracticeTest>>(await _coreHttp.GetAsync("/api/teacher/practice-tests"));
    }

    public async Task<PracticeTest> CreatePracticeTestAsync(string title, string type, string? prompt, object? content)
    {
        ApplyAuth(_coreHttp);
        var payload = new CreatePracticeTestRequest(title, type, prompt, content);
        return await ReadAsync<PracticeTest>(await _coreHttp.PostAsync("/api/teacher/practice-tests", CreateJsonContent(payload)));
    }

    public async Task<PracticeTest> UpdatePracticeTestAsync(int id, PracticeTest practiceTest)
    {
        ApplyAuth(_coreHttp);
        var payload = new CreatePracticeTestRequest(practiceTest.Title, practiceTest.Type, practiceTest.Prompt, practiceTest.Content);
        return await ReadAsync<PracticeTest>(await _coreHttp.PutAsync($"/api/teacher/practice-tests/{id}", CreateJsonContent(payload)));
    }

    public async Task<bool> DeletePracticeTestAsync(int id)
    {
        ApplyAuth(_coreHttp);
        var response = await _coreHttp.DeleteAsync($"/api/teacher/practice-tests/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        response.EnsureSuccessStatusCode();
        return true;
    }

    #endregion

    #region Student - Sessions & Bookings

    public async Task<List<ExamSession>> GetAvailableSessionsAsync()
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<List<ExamSession>>(await _coreHttp.GetAsync("/api/student/sessions"));
    }

    public async Task<Booking> BookSeatAsync(int sessionId, string seatId)
    {
        ApplyAuth(_coreHttp);
        var payload = new BookSeatRequest(sessionId, seatId);
        return await ReadAsync<Booking>(await _coreHttp.PostAsync("/api/student/bookings", CreateJsonContent(payload)));
    }

    public async Task<List<Booking>> GetMyBookingsAsync()
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<List<Booking>>(await _coreHttp.GetAsync("/api/student/bookings"));
    }

    public async Task<Booking?> UpdateMyBookingSeatAsync(int bookingId, string newSeatId)
    {
        ApplyAuth(_coreHttp);
        var payload = new UpdateBookingRequest(newSeatId);
        var response = await _coreHttp.PutAsync($"/api/student/bookings/{bookingId}", CreateJsonContent(payload));
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException("Seat already booked or invalid.");
        }
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await ReadAsync<Booking>(response);
    }

    public async Task<bool> DeleteMyBookingAsync(int bookingId)
    {
        ApplyAuth(_coreHttp);
        var response = await _coreHttp.DeleteAsync($"/api/student/bookings/{bookingId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return true;
        }
        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<List<ExamClass>> GetMyClassesAsync()
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<List<ExamClass>>(await _coreHttp.GetAsync("/api/student/classes"));
    }

    #endregion

    #region Student - Practice Tests

    public async Task<List<PracticeTest>> GetAvailablePracticeTestsAsync()
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<List<PracticeTest>>(await _coreHttp.GetAsync("/api/student/practice-tests"));
    }

    public async Task<PracticeTest> GetPracticeTestAsync(int id)
    {
        ApplyAuth(_coreHttp);
        return await ReadAsync<PracticeTest>(await _coreHttp.GetAsync($"/api/student/practice-tests/{id}"));
    }

    public async Task<PracticeSubmission> SubmitPracticeTestAsync(int id, List<int>? answers, object? data = null, Dictionary<int, List<int>>? multiAnswers = null)
    {
        ApplyAuth(_coreHttp);
        var payload = new SubmitPracticeTestRequest(data, answers, multiAnswers);
        return await ReadAsync<PracticeSubmission>(await _coreHttp.PostAsync($"/api/student/practice-tests/{id}/submit", CreateJsonContent(payload)));
    }

    #endregion
}
