using System.Text.Json;
using AIExamIDE.Backend.Data;
using AIExamIDE.Backend.Services;
using FrontModels = AIExamIDE.Models;

namespace AIExamIDE.Backend.Contracts;

public static class DtoMapping
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static FrontModels.UserInfo ToUserInfo(this User user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role
        };

    public static FrontModels.ExamRoom ToExamRoomDto(this ExamRoom room)
    {
        var seatmap = new FrontModels.SeatMap();
        if (!string.IsNullOrWhiteSpace(room.SeatmapJson))
        {
            try
            {
                seatmap = JsonSerializer.Deserialize<FrontModels.SeatMap>(room.SeatmapJson, SerializerOptions) ?? seatmap;
            }
            catch
            {
                // keep default seatmap if parsing fails
            }
        }

        return new FrontModels.ExamRoom
        {
            Id = room.Id,
            Name = room.Name,
            Seatmap = seatmap
        };
    }

    public static FrontModels.ExamSession ToExamSessionDto(this ExamSession session, ExamRoom? room = null)
    {
        var dto = new FrontModels.ExamSession
        {
            Id = session.Id,
            RoomId = session.RoomId,
            Title = session.Title,
            Date = session.Date,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            ExamType = session.ExamType,
            AiGenerated = session.AiGenerated,
            RoomName = room?.Name
        };

        if (room is not null)
        {
            dto.Room = room.ToExamRoomDto();
        }

        return dto;
    }

    public static FrontModels.Booking ToBookingDto(this Booking booking, ExamSession? session = null, ExamRoom? room = null)
    {
        var dto = new FrontModels.Booking
        {
            Id = booking.Id,
            SessionId = booking.SessionId,
            StudentId = booking.StudentId,
            SeatId = booking.SeatId,
            Status = booking.Status,
            Date = session?.Date,
            Title = session?.Title,
            StartTime = session?.StartTime,
            EndTime = session?.EndTime,
            RoomId = session?.RoomId,
            ExamType = session?.ExamType,
            AiGenerated = session?.AiGenerated,
            RoomName = room?.Name,
            SeatName = room is not null ? ResolveSeatName(room, booking.SeatId) : null
        };

        return dto;
    }

    private static string? ResolveSeatName(ExamRoom room, string seatId)
    {
        if (string.IsNullOrWhiteSpace(room.SeatmapJson) || string.IsNullOrWhiteSpace(seatId)) return null;
        try
        {
            var seatmap = JsonSerializer.Deserialize<FrontModels.SeatMap>(room.SeatmapJson, SerializerOptions);
            var desk = seatmap?.Desks?.FirstOrDefault(d => d.Id == seatId);
            return desk?.Name;
        }
        catch
        {
            return null;
        }
    }

    public static FrontModels.Submission ToSubmissionDto(this Submission submission, Booking? booking = null, User? student = null)
    {
        return new FrontModels.Submission
        {
            Id = submission.Id,
            BookingId = submission.BookingId,
            FilesJson = submission.FilesJson,
            TasksJson = submission.TasksJson,
            CsvsJson = submission.CsvsJson,
            EvaluationJson = submission.EvaluationJson,
            GradeFinal = submission.GradeFinal,
            FeedbackJson = submission.FeedbackJson,
            CreatedAt = submission.CreatedAt.ToString("o"),
            UpdatedAt = submission.UpdatedAt.ToString("o"),
            SessionId = booking?.SessionId,
            StudentId = booking?.StudentId,
            StudentEmail = student?.Email,
            StudentName = student?.Name
        };
    }

    public static FrontModels.PracticeTest ToPracticeTestDto(this PracticeTest test)
    {
        var dto = new FrontModels.PracticeTest
        {
            Id = test.Id,
            TeacherId = test.TeacherId,
            Title = test.Title,
            Type = test.Type,
            Prompt = test.Prompt,
            ContentJson = test.ContentJson,
            CreatedAt = test.CreatedAt.ToString("o")
        };

        if (!string.IsNullOrWhiteSpace(test.ContentJson))
        {
            try
            {
                dto.Content = JsonSerializer.Deserialize<FrontModels.PracticeTestContent>(test.ContentJson!, SerializerOptions);
            }
            catch
            {
                dto.Content = null;
            }
        }

        return dto;
    }

    public static FrontModels.PracticeSubmission ToPracticeSubmissionDto(this PracticeSubmission submission)
    {
        return new FrontModels.PracticeSubmission
        {
            Id = submission.Id,
            TestId = submission.TestId,
            StudentId = submission.StudentId,
            DataJson = submission.DataJson,
            EvaluationJson = submission.EvaluationJson,
            Score = submission.Score,
            CreatedAt = submission.CreatedAt.ToString("o")
        };
    }

    public static FrontModels.ExamClass ToExamClassDto(this Classroom classroom, int studentCount)
    {
        return new FrontModels.ExamClass
        {
            Id = classroom.Id,
            Name = classroom.Name,
            TeacherId = classroom.TeacherId,
            CreatedAt = classroom.CreatedAt.ToString("o"),
            StudentCount = studentCount
        };
    }

    public static FrontModels.TeacherReport ToTeacherReportDto(this TeacherCounts counts)
    {
        return new FrontModels.TeacherReport
        {
            Totals = new FrontModels.ReportTotals
            {
                Sessions = counts.Sessions,
                UpcomingSessions = counts.UpcomingSessions,
                Rooms = counts.Rooms,
                Bookings = counts.Bookings,
                Submissions = counts.Submissions
            },
            Averages = new FrontModels.ReportAverages
            {
                FinalGrade = counts.AverageFinalGrade.HasValue
                    ? (int?)Math.Round(counts.AverageFinalGrade.Value)
                    : null
            }
        };
    }
}
