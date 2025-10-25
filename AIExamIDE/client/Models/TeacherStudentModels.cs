namespace AIExamIDE.Models;

public class AuthResponse
{
    public string Token { get; set; } = "";
    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = ""; // teacher | student
}

public class ExamRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public SeatMap Seatmap { get; set; } = new();
}

public class SeatMap
{
    public List<Desk> Desks { get; set; } = new();
}

public class Desk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Desk";
    public string Hostname { get; set; } = "";
    public string? Ip { get; set; }
    public string? Os { get; set; }
    public string? Notes { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public class ExamSession
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string? Title { get; set; }
    public string Date { get; set; } = ""; // YYYY-MM-DD
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string ExamType { get; set; } = "java";
    public bool AiGenerated { get; set; } = true;
    public string? RoomName { get; set; }
    
    // Extended properties for student view
    public ExamRoom? Room { get; set; }
    public List<string>? BookedSeats { get; set; }
}

public class Booking
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public string SeatId { get; set; } = "";
    public string Status { get; set; } = "booked";
    public string? SeatName { get; set; }
    public string? StudentName { get; set; }
    public string? StudentEmail { get; set; }
    
    // Extended properties from API joins
    public string? Date { get; set; }
    public string? Title { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int? RoomId { get; set; }
    public string? ExamType { get; set; }
    public bool? AiGenerated { get; set; }
    public string? RoomName { get; set; }
}

public class Submission
{
    public int Id { get; set; }
    public int? BookingId { get; set; }
    public string? FilesJson { get; set; }
    public string? TasksJson { get; set; }
    public string? CsvsJson { get; set; }
    public string? EvaluationJson { get; set; }
    public int? GradeFinal { get; set; }
    public string? FeedbackJson { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
    
    // Extended properties from API joins
    public int? SessionId { get; set; }
    public int? StudentId { get; set; }
    public string? StudentEmail { get; set; }
    public string? StudentName { get; set; }
}

public class ExamClass
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int TeacherId { get; set; }
    public string CreatedAt { get; set; } = "";
    public int StudentCount { get; set; }
}

public class ClassStudent
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public UserInfo? Student { get; set; }
}

public class PracticeTest
{
    public int Id { get; set; }
    public int? TeacherId { get; set; }
    public string Title { get; set; } = "";
    public string Type { get; set; } = ""; // ide | mcq
    public string? Prompt { get; set; }
    public string? ContentJson { get; set; }
    public string CreatedAt { get; set; } = "";
    
    // Parsed content for easier access
    public PracticeTestContent? Content { get; set; }
}

public class PracticeTestContent
{
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public List<PracticeTestItem> Items { get; set; } = new();
}

public class PracticeTestItem
{
    public string? Question { get; set; }
    public List<string>? Options { get; set; }
    public int? CorrectIndex { get; set; }
    public string? Description { get; set; }
    public List<ExamFile>? Files { get; set; }
}

public class PracticeSubmission
{
    public int Id { get; set; }
    public int TestId { get; set; }
    public int StudentId { get; set; }
    public string? DataJson { get; set; }
    public string? EvaluationJson { get; set; }
    public int? Score { get; set; }
    public string CreatedAt { get; set; } = "";
}

public class TeacherReport
{
    public ReportTotals Totals { get; set; } = new();
    public ReportAverages Averages { get; set; } = new();
}

public class ReportTotals
{
    public int Sessions { get; set; }
    public int UpcomingSessions { get; set; }
    public int Rooms { get; set; }
    public int Bookings { get; set; }
    public int Submissions { get; set; }
}

public class ReportAverages
{
    public int? FinalGrade { get; set; }
}

