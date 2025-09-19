using AIExamIDE.Models;
using System.Text.Json;
using System.Linq;

namespace AIExamIDE.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ExamResponse> GenerateExamAsync()
    {
        var response = await _httpClient.PostAsync("/exam", null);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ExamResponse>(json, _jsonOptions)!;
    }

    public async Task<RunResponse> RunCodeAsync(List<ExamFile> files, string? mainFile = null)
    {
        var request = new { Files = files, MainFile = mainFile };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/run", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RunResponse>(responseJson, _jsonOptions)!;
    }

    public async Task ResetExamAsync()
    {
        var response = await _httpClient.PostAsync("/reset", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task SubmitExamAsync(List<ExamFile> files)
    {
        var request = new { Files = files };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/submit", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<EvaluationResult> EvaluateAsync(List<ExamFile> files, ExamMetadata exam)
    {
        var csvNames = files.Where(f => !f.IsDirectory && f.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                            .Select(f => f.Name)
                            .Distinct()
                            .ToList();

        var req = new { Files = files, Exam = new {
            domain = exam.Domain,
            overview = exam.Overview,
            csv_files = csvNames
        }};
        var json = JsonSerializer.Serialize(req, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/evaluate", content);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EvaluationResult>(responseJson, _jsonOptions)!;
    }
}