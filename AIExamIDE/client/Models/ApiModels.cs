namespace AIExamIDE.Models;

public class ExamResponse
{
    public string ExamId { get; set; } = "";
    public ExamMetadata Exam { get; set; } = new();
    public List<ExamFile> Files { get; set; } = new();
}

public class RunResponse
{
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
}