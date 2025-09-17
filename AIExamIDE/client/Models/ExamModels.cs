namespace AIExamIDE.Models;

public class ExamMetadata
{
    public string Domain { get; set; } = "";
    public string Overview { get; set; } = "";
    public List<ExamTask> Tasks { get; set; } = new();
    public int? Duration { get; set; } = 50; // Duration in minutes, default to 60
}

public class ExamTask
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public class ExamFile
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsDirectory { get; set; } = false;
    public List<ExamFile> Children { get; set; } = new();
}