using System.Text.Json.Serialization;
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

public class EvaluationResult
{
    [JsonPropertyName("exam")]
    public EvaluationExam Exam { get; set; } = new();

    [JsonPropertyName("evaluation")]
    public EvaluationDetails Evaluation { get; set; } = new();

    [JsonPropertyName("final_grade")]
    public int Final_Grade { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = "";
}

public class EvaluationExam
{
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = "";

    [JsonPropertyName("csv_files")]
    public List<string> Csv_Files { get; set; } = new();

    [JsonPropertyName("overview")]
    public string Overview { get; set; } = "";
}

public class EvaluationDetails
{
    [JsonPropertyName("task1")]
    public EvaluationTask Task1 { get; set; } = new();

    [JsonPropertyName("task2")]
    public EvaluationTask Task2 { get; set; } = new();

    [JsonPropertyName("task3")]
    public EvaluationTask Task3 { get; set; } = new();

    [JsonPropertyName("task4")]
    public EvaluationTask Task4 { get; set; } = new();
}

public class EvaluationTask
{
    [JsonPropertyName("percentage")]
    public int Percentage { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = "";
}