using AIExamIDE.Models;
using System.Text.Json;

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
}