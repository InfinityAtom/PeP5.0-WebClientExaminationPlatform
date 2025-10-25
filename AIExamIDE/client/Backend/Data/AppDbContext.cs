using Microsoft.EntityFrameworkCore;

namespace AIExamIDE.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<ClassStudent> ClassStudents => Set<ClassStudent>();
    public DbSet<ExamRoom> ExamRooms => Set<ExamRoom>();
    public DbSet<ExamSession> ExamSessions => Set<ExamSession>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<PracticeTest> PracticeTests => Set<PracticeTest>();
    public DbSet<PracticeSubmission> PracticeSubmissions => Set<PracticeSubmission>();
    public DbSet<FallbackExam> FallbackExams => Set<FallbackExam>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .ToTable("users")
            .Property(u => u.CreatedAt)
            .HasColumnName("created_at");

        modelBuilder.Entity<User>()
            .Property(u => u.PasswordHash)
            .HasColumnName("password_hash");

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasColumnName("role");

        modelBuilder.Entity<Classroom>()
            .ToTable("classes")
            .Property(c => c.CreatedAt)
            .HasColumnName("created_at");

        modelBuilder.Entity<Classroom>()
            .Property(c => c.TeacherId)
            .HasColumnName("teacher_id");

        modelBuilder.Entity<ClassStudent>()
            .ToTable("class_students")
            .HasKey(cs => new { cs.ClassId, cs.StudentId });

        modelBuilder.Entity<ClassStudent>()
            .Property(cs => cs.ClassId)
            .HasColumnName("class_id");

        modelBuilder.Entity<ClassStudent>()
            .Property(cs => cs.StudentId)
            .HasColumnName("student_id");

        modelBuilder.Entity<ExamRoom>()
            .ToTable("exam_rooms")
            .Property(r => r.SeatmapJson)
            .HasColumnName("seatmap_json");

        modelBuilder.Entity<ExamRoom>()
            .Property(r => r.CreatedAt)
            .HasColumnName("created_at");

        modelBuilder.Entity<ExamRoom>()
            .Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        modelBuilder.Entity<ExamSession>()
            .ToTable("exam_sessions")
            .Property(s => s.TeacherId)
            .HasColumnName("teacher_id");

        modelBuilder.Entity<ExamSession>()
            .Property(s => s.RoomId)
            .HasColumnName("room_id");

        modelBuilder.Entity<ExamSession>()
            .Property(s => s.StartTime)
            .HasColumnName("start_time");

        modelBuilder.Entity<ExamSession>()
            .Property(s => s.EndTime)
            .HasColumnName("end_time");

        modelBuilder.Entity<ExamSession>()
            .Property(s => s.ExamType)
            .HasColumnName("exam_type");

        modelBuilder.Entity<ExamSession>()
            .Property(s => s.AiGenerated)
            .HasColumnName("ai_generated");

        modelBuilder.Entity<ExamSession>()
            .Property(s => s.Status)
            .HasColumnName("status");

        modelBuilder.Entity<ExamSession>()
            .Property(s => s.CreatedAt)
            .HasColumnName("created_at");

        modelBuilder.Entity<ExamSession>()
            .Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        modelBuilder.Entity<Booking>()
            .ToTable("bookings")
            .Property(b => b.SessionId)
            .HasColumnName("session_id");

        modelBuilder.Entity<Booking>()
            .Property(b => b.StudentId)
            .HasColumnName("student_id");

        modelBuilder.Entity<Booking>()
            .Property(b => b.SeatId)
            .HasColumnName("seat_id");

        modelBuilder.Entity<Booking>()
            .Property(b => b.Status)
            .HasColumnName("status");

        modelBuilder.Entity<Booking>()
            .Property(b => b.CreatedAt)
            .HasColumnName("created_at");

        modelBuilder.Entity<Submission>()
            .ToTable("submissions")
            .Property(s => s.BookingId)
            .HasColumnName("booking_id");

        modelBuilder.Entity<Submission>()
            .Property(s => s.FilesJson)
            .HasColumnName("files_json");

        modelBuilder.Entity<Submission>()
            .Property(s => s.TasksJson)
            .HasColumnName("tasks_json");

        modelBuilder.Entity<Submission>()
            .Property(s => s.CsvsJson)
            .HasColumnName("csvs_json");

        modelBuilder.Entity<Submission>()
            .Property(s => s.EvaluationJson)
            .HasColumnName("evaluation_json");

        modelBuilder.Entity<Submission>()
            .Property(s => s.GradeFinal)
            .HasColumnName("grade_final");

        modelBuilder.Entity<Submission>()
            .Property(s => s.FeedbackJson)
            .HasColumnName("feedback_json");

        modelBuilder.Entity<Submission>()
            .Property(s => s.CreatedAt)
            .HasColumnName("created_at");

        modelBuilder.Entity<Submission>()
            .Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        modelBuilder.Entity<PracticeTest>()
            .ToTable("practice_tests")
            .Property(pt => pt.TeacherId)
            .HasColumnName("teacher_id");

        modelBuilder.Entity<PracticeTest>()
            .Property(pt => pt.ContentJson)
            .HasColumnName("content_json");

        modelBuilder.Entity<PracticeTest>()
            .Property(pt => pt.CreatedAt)
            .HasColumnName("created_at");

        modelBuilder.Entity<PracticeSubmission>()
            .ToTable("practice_submissions")
            .Property(ps => ps.TestId)
            .HasColumnName("test_id");

        modelBuilder.Entity<PracticeSubmission>()
            .Property(ps => ps.StudentId)
            .HasColumnName("student_id");

        modelBuilder.Entity<PracticeSubmission>()
            .Property(ps => ps.DataJson)
            .HasColumnName("data_json");

        modelBuilder.Entity<PracticeSubmission>()
            .Property(ps => ps.EvaluationJson)
            .HasColumnName("evaluation_json");

        modelBuilder.Entity<PracticeSubmission>()
            .Property(ps => ps.Score)
            .HasColumnName("score");

        modelBuilder.Entity<PracticeSubmission>()
            .Property(ps => ps.CreatedAt)
            .HasColumnName("created_at");

        modelBuilder.Entity<FallbackExam>()
            .ToTable("fallback_exam")
            .HasKey(f => f.Id);

        modelBuilder.Entity<FallbackExam>()
            .Property(f => f.Json)
            .HasColumnName("json");
    }
}
