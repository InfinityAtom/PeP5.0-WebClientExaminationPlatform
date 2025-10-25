namespace AIExamIDE.Backend.Data;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Classroom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ClassStudent
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
}

public class ExamRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SeatmapJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ExamSession
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public int RoomId { get; set; }
    public string? Title { get; set; }
    public string Date { get; set; } = string.Empty;
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string ExamType { get; set; } = "java";
    public bool AiGenerated { get; set; } = true;
    public string Status { get; set; } = "scheduled";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Booking
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public string SeatId { get; set; } = string.Empty;
    public string Status { get; set; } = "booked";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PracticeTest
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "ide";
    public string? Prompt { get; set; }
    public string? ContentJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PracticeSubmission
{
    public int Id { get; set; }
    public int TestId { get; set; }
    public int StudentId { get; set; }
    public string? DataJson { get; set; }
    public string? EvaluationJson { get; set; }
    public int? Score { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FallbackExam
{
    public int Id { get; set; }
    public string Json { get; set; } = "{}";
}
