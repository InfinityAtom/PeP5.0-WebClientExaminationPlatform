using AIExamIDE.Models;

namespace AIExamIDE.Backend.Contracts;

public record RegisterRequest(string Email, string Name, string Password, string Role);

public record LoginRequest(string Email, string Password);

public record CreateRoomRequest(string Name, SeatMap Seatmap);

public record UpdateRoomRequest(string? Name, SeatMap? Seatmap);

public record CreateSessionRequest(
    int RoomId,
    string Date,
    string? Title,
    string? StartTime,
    string? EndTime,
    string? ExamType,
    bool? AiGenerated);

public record UpdateSessionRequest(
    string? Title,
    string? Date,
    string? StartTime,
    string? EndTime,
    string? ExamType,
    bool? AiGenerated,
    string? Status);

public record CreateClassRequest(string Name);

public record AddClassStudentRequest(int? StudentId, string? StudentEmail, string? StudentName);

public record CreatePracticeTestRequest(string Title, string Type, string? Prompt, object? Content);

public record SubmitPracticeTestRequest(object? Data, List<int>? Answers, Dictionary<int, List<int>>? MultiAnswers = null);

public record BookSeatRequest(int SessionId, string SeatId);
public record UpdateBookingRequest(string SeatId);

public record UpdateSubmissionRequest(int? GradeFinal, Dictionary<string, SubmissionTaskAdjustment>? PerTask, object? Feedback);

public record SubmissionTaskAdjustment(double? Percentage, string? Status, string? Explanation);

public record CreateSubmissionRequest(int? BookingId, object? Files, object? Tasks, object? Csvs, object? Evaluation, int? GradeFinal, object? Feedback);

public record UpdateEvaluationRequest(int BookingId, object Evaluation, int? GradeFinal, object? Feedback);
