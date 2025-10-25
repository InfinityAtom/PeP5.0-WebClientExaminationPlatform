using AIExamIDE.Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AIExamIDE.Backend.Services;

public class AppRepository
{
    private readonly AppDbContext _db;

    public AppRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User> CreateUserAsync(string email, string name, string passwordHash, string role)
    {
        var user = new User
        {
            Email = email,
            Name = name,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public Task<User?> GetUserByEmailAsync(string email) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public Task<User?> GetUserByIdAsync(int id) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<List<User>> ListUsersByRoleAsync(string role) =>
        _db.Users
            .Where(u => u.Role.ToLower() == role.ToLower())
            .OrderByDescending(u => u.Id)
            .ToListAsync();

    public Task<List<User>> ListUsersByIdsAsync(IEnumerable<int> ids)
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.Count == 0) return Task.FromResult(new List<User>());
        return _db.Users.Where(u => distinct.Contains(u.Id)).ToListAsync();
    }

    public async Task<ExamRoom> CreateRoomAsync(string name, string seatmapJson)
    {
        var room = new ExamRoom
        {
            Name = name,
            SeatmapJson = seatmapJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ExamRooms.Add(room);
        await _db.SaveChangesAsync();
        return room;
    }

    public Task<List<ExamRoom>> ListRoomsAsync() =>
        _db.ExamRooms
           .OrderByDescending(r => r.Id)
           .ToListAsync();

    public Task<ExamRoom?> GetRoomAsync(int id) =>
        _db.ExamRooms.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<ExamRoom?> UpdateRoomAsync(int id, string? name, string? seatmapJson)
    {
        var room = await GetRoomAsync(id);
        if (room is null) return null;
        if (name is not null) room.Name = name;
        if (seatmapJson is not null) room.SeatmapJson = seatmapJson;
        room.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return room;
    }

    public async Task<bool> DeleteRoomAsync(int id)
    {
        var room = await GetRoomAsync(id);
        if (room is null) return false;
        _db.ExamRooms.Remove(room);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ExamSession> CreateSessionAsync(int teacherId, int roomId, string? title, string date, string? startTime, string? endTime, string examType, bool aiGenerated)
    {
        var session = new ExamSession
        {
            TeacherId = teacherId,
            RoomId = roomId,
            Title = title,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            ExamType = string.IsNullOrWhiteSpace(examType) ? "java" : examType,
            AiGenerated = aiGenerated,
            Status = "scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ExamSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public Task<List<ExamSession>> ListSessionsByTeacherAsync(int teacherId) =>
        _db.ExamSessions
           .Where(s => s.TeacherId == teacherId)
           .OrderByDescending(s => s.Date)
           .ThenByDescending(s => s.StartTime)
           .ToListAsync();

    public Task<ExamSession?> GetSessionAsync(int id) =>
        _db.ExamSessions.FirstOrDefaultAsync(s => s.Id == id);

    public Task<ExamSession?> GetSessionByTeacherAsync(int id, int teacherId) =>
        _db.ExamSessions.FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == teacherId);

    public async Task<ExamSession?> UpdateSessionAsync(int id, string? title, string? date, string? startTime, string? endTime, string? examType, bool? aiGenerated, string? status)
    {
        var session = await GetSessionAsync(id);
        if (session is null) return null;
        if (title is not null) session.Title = title;
        if (date is not null) session.Date = date;
        if (startTime is not null) session.StartTime = startTime;
        if (endTime is not null) session.EndTime = endTime;
        if (examType is not null) session.ExamType = examType;
        if (aiGenerated.HasValue) session.AiGenerated = aiGenerated.Value;
        if (status is not null) session.Status = status;
        session.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task<bool> DeleteSessionAsync(int id)
    {
        var session = await GetSessionAsync(id);
        if (session is null) return false;
        
        // Delete related entities first (cascade delete should handle this, but explicit is safer)
        var bookings = await _db.Bookings.Where(b => b.SessionId == id).ToListAsync();
        foreach (var booking in bookings)
        {
            // Delete any submissions related to this booking
            var submissions = await _db.Submissions.Where(s => s.BookingId == booking.Id).ToListAsync();
            _db.Submissions.RemoveRange(submissions);
        }
        
        // Delete all bookings for this session
        _db.Bookings.RemoveRange(bookings);
        
        // Delete the session itself
        _db.ExamSessions.Remove(session);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ExamSession>> ListScheduledSessionsFromDateAsync(string date)
    {
        try
        {
            var all = await _db.ExamSessions
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            // Filter in-memory to avoid EF translation issues with string.Compare
            var filtered = all
                .Where(s => (s.Status == "scheduled" || string.IsNullOrEmpty(s.Status)) && string.CompareOrdinal(s.Date, date) >= 0)
                .ToList();
            return filtered;
        }
        catch (Exception ex)
        {
            // Fallback for legacy databases missing the 'status' column entirely
            if (ex.Message.Contains("no such column", StringComparison.OrdinalIgnoreCase) &&
                ex.Message.Contains("status", StringComparison.OrdinalIgnoreCase))
            {
                var all = await _db.ExamSessions
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.StartTime)
                    .ToListAsync();
                return all.Where(s => string.CompareOrdinal(s.Date, date) >= 0).ToList();
            }
            throw;
        }
    }

    public Task<List<string>> ListBookedSeatsAsync(int sessionId) =>
        _db.Bookings
           .Where(b => b.SessionId == sessionId)
           .Select(b => b.SeatId)
           .ToListAsync();

    public Task<Booking?> FindExistingBookingAsync(int sessionId, string seatId, int studentId) =>
        _db.Bookings.FirstOrDefaultAsync(b =>
            b.SessionId == sessionId &&
            (b.SeatId == seatId || b.StudentId == studentId));

    public async Task<Booking> CreateBookingAsync(int sessionId, int studentId, string seatId)
    {
        var booking = new Booking
        {
            SessionId = sessionId,
            StudentId = studentId,
            SeatId = seatId,
            Status = "booked",
            CreatedAt = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return booking;
    }

    public async Task<Booking?> UpdateBookingSeatAsync(int bookingId, int studentId, string newSeatId)
    {
        if (string.IsNullOrWhiteSpace(newSeatId)) return null;
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.StudentId == studentId);
        if (booking is null) return null;

        // Prevent seat collision: ensure no other booking already uses newSeatId for same session
        var seatTaken = await _db.Bookings.AnyAsync(b => b.SessionId == booking.SessionId && b.SeatId == newSeatId && b.Id != bookingId);
        if (seatTaken) return null; // caller should translate to conflict

        booking.SeatId = newSeatId;
        await _db.SaveChangesAsync();
        return booking;
    }

    public async Task<bool> DeleteBookingAsync(int bookingId, int studentId)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.StudentId == studentId);
        if (booking is null) return false;
        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<List<Booking>> ListBookingsByStudentAsync(int studentId) =>
        _db.Bookings.Where(b => b.StudentId == studentId)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();

    public async Task<List<Booking>> ListBookingsBySessionAsync(int sessionId)
    {
        // Include user data for teacher view
        var bookings = await _db.Bookings
            .Where(b => b.SessionId == sessionId)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();

        // Attach lightweight student info (avoid heavy joins / projections for now)
        var userIds = bookings.Select(b => b.StudentId).Distinct().ToList();
        var users = await _db.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
        var userLookup = users.ToDictionary(u => u.Id, u => u);

        foreach (var b in bookings)
        {
            if (userLookup.TryGetValue(b.StudentId, out var u))
            {
                // Use Tags/Notes pattern: temporarily stash student name/email using dynamic properties if desired later.
                // For now teacher page will fetch user separately if needed.
            }
        }
        return bookings;
    }

    public Task<Booking?> GetBookingByIdAsync(int id) =>
        _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Submission> CreateSubmissionAsync(int? bookingId, string? filesJson, string? tasksJson, string? csvsJson, string? evaluationJson, int? gradeFinal, string? feedbackJson)
    {
        var submission = new Submission
        {
            BookingId = bookingId,
            FilesJson = filesJson,
            TasksJson = tasksJson,
            CsvsJson = csvsJson,
            EvaluationJson = evaluationJson,
            GradeFinal = gradeFinal,
            FeedbackJson = feedbackJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    _db.Submissions.Add(submission);
    await _db.SaveChangesAsync();
    return submission;
}

    public Task<Submission?> GetLatestSubmissionByBookingAsync(int bookingId) =>
        _db.Submissions
           .Where(s => s.BookingId == bookingId)
           .OrderByDescending(s => s.CreatedAt)
           .FirstOrDefaultAsync();

    public Task<List<Submission>> ListSubmissionsAsync() =>
        _db.Submissions
           .OrderByDescending(s => s.CreatedAt)
           .ToListAsync();

    public Task<Submission?> GetSubmissionAsync(int id) =>
        _db.Submissions.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Submission?> UpdateSubmissionAsync(int id, int? gradeFinal, string? evaluationJson, string? feedbackJson)
    {
        var submission = await GetSubmissionAsync(id);
        if (submission is null) return null;

        if (gradeFinal.HasValue) submission.GradeFinal = gradeFinal;
        if (evaluationJson is not null) submission.EvaluationJson = evaluationJson;
        if (feedbackJson is not null) submission.FeedbackJson = feedbackJson;
        submission.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return submission;
    }

    public async Task<Classroom> CreateClassAsync(int teacherId, string name)
    {
        var classroom = new Classroom
        {
            TeacherId = teacherId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        _db.Classrooms.Add(classroom);
        await _db.SaveChangesAsync();
    return classroom;
}

public Task<List<Classroom>> ListClassesAsync(int teacherId) =>
    _db.Classrooms
       .Where(c => c.TeacherId == teacherId)
       .OrderByDescending(c => c.Id)
       .ToListAsync();

    public Task<Classroom?> GetClassAsync(int id) =>
        _db.Classrooms.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Classroom?> UpdateClassAsync(int id, string name)
    {
        var classroom = await _db.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
        if (classroom is null) return null;
        classroom.Name = name;
        await _db.SaveChangesAsync();
        return classroom;
    }

    public async Task<bool> DeleteClassAsync(int id)
    {
        var classroom = await _db.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
        if (classroom is null) return false;
        _db.Classrooms.Remove(classroom);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<List<ClassStudent>> ListClassStudentsAsync(int classId) =>
        _db.ClassStudents
           .Where(cs => cs.ClassId == classId)
           .ToListAsync();

    public Task<List<Classroom>> ListClassesByStudentAsync(int studentId) =>
        _db.ClassStudents
           .Where(cs => cs.StudentId == studentId)
           .Select(cs => cs.ClassId)
           .Distinct()
           .Join(_db.Classrooms, id => id, c => c.Id, (id, c) => c)
           .OrderByDescending(c => c.Id)
           .ToListAsync();

public Task<int> CountClassStudentsAsync(int classId) =>
    _db.ClassStudents.CountAsync(cs => cs.ClassId == classId);

    public async Task AddClassStudentAsync(int classId, int studentId)
    {
        var exists = await _db.ClassStudents.AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
        if (exists) return;
        _db.ClassStudents.Add(new ClassStudent { ClassId = classId, StudentId = studentId });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveClassStudentAsync(int classId, int studentId)
    {
        var entity = await _db.ClassStudents.FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
        if (entity is null) return;
        _db.ClassStudents.Remove(entity);
        await _db.SaveChangesAsync();
    }

public async Task<PracticeTest> CreatePracticeTestAsync(int teacherId, string title, string type, string? prompt, string? contentJson)
{
    var test = new PracticeTest
    {
        TeacherId = teacherId,
        Title = title,
        Type = type,
        Prompt = prompt,
        ContentJson = contentJson,
        CreatedAt = DateTime.UtcNow
    };

    _db.PracticeTests.Add(test);
    await _db.SaveChangesAsync();
    return test;
}

public Task<List<PracticeTest>> ListPracticeTestsByTeacherAsync(int teacherId) =>
    _db.PracticeTests
       .Where(pt => pt.TeacherId == teacherId)
       .OrderByDescending(pt => pt.CreatedAt)
       .ToListAsync();

public Task<List<PracticeTest>> ListAllPracticeTestsAsync() =>
    _db.PracticeTests
       .OrderByDescending(pt => pt.CreatedAt)
       .ToListAsync();

public Task<PracticeTest?> GetPracticeTestAsync(int id) =>
    _db.PracticeTests.FirstOrDefaultAsync(pt => pt.Id == id);

    public async Task<PracticeTest?> UpdatePracticeTestAsync(int id, string title, string type, string? prompt, string? contentJson)
    {
        var test = await _db.PracticeTests.FirstOrDefaultAsync(pt => pt.Id == id);
        if (test is null) return null;
        test.Title = title;
        test.Type = type;
        test.Prompt = prompt;
        test.ContentJson = contentJson;
        await _db.SaveChangesAsync();
        return test;
    }

    public async Task<bool> DeletePracticeTestAsync(int id)
    {
        var test = await _db.PracticeTests.FirstOrDefaultAsync(pt => pt.Id == id);
        if (test is null) return false;
        _db.PracticeTests.Remove(test);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PracticeSubmission> CreatePracticeSubmissionAsync(int testId, int studentId, string? dataJson, string? evaluationJson, int? score)
    {
        var submission = new PracticeSubmission
        {
            TestId = testId,
            StudentId = studentId,
            DataJson = dataJson,
            EvaluationJson = evaluationJson,
            Score = score,
            CreatedAt = DateTime.UtcNow
        };
        _db.PracticeSubmissions.Add(submission);
        await _db.SaveChangesAsync();
        return submission;
    }

    public Task<FallbackExam?> GetFallbackExamAsync() =>
        _db.FallbackExams.FirstOrDefaultAsync(fe => fe.Id == 1);

    public async Task SetFallbackExamAsync(string json)
    {
        var existing = await GetFallbackExamAsync();
        if (existing is null)
        {
            _db.FallbackExams.Add(new FallbackExam { Id = 1, Json = json });
        }
        else
        {
            existing.Json = json;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<TeacherCounts> CountsByTeacherAsync(int teacherId)
    {
        var teacherSessions = await _db.ExamSessions
            .Where(s => s.TeacherId == teacherId)
            .ToListAsync();

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var sessions = teacherSessions.Count;
        var upcomingSessions = teacherSessions.Count(s => string.CompareOrdinal(s.Date, today) >= 0);

        var rooms = await _db.ExamRooms.CountAsync();
        var bookings = await _db.Bookings.CountAsync();
        var submissions = await _db.Submissions.CountAsync();

        double? avgGrade = await _db.Submissions
            .Where(s => s.GradeFinal != null)
            .AverageAsync(s => (double?)s.GradeFinal);

        if (avgGrade.HasValue)
        {
            avgGrade = Math.Round(avgGrade.Value);
        }

        return new TeacherCounts(
            sessions,
            upcomingSessions,
            rooms,
            bookings,
            submissions,
            avgGrade
        );
    }
}

public record TeacherCounts(
    int Sessions,
    int UpcomingSessions,
    int Rooms,
    int Bookings,
    int Submissions,
    double? AverageFinalGrade);
