using Microsoft.JSInterop;

namespace AIExamIDE.Services;

public interface ILocalStorage
{
    ValueTask SetItemAsync<T>(string key, T value);
    ValueTask<T?> GetItemAsync<T>(string key);
    ValueTask RemoveItemAsync(string key);
}

public class LocalStorageService : ILocalStorage
{
    private readonly IJSRuntime _js;
    public LocalStorageService(IJSRuntime js) { _js = js; }

    public ValueTask SetItemAsync<T>(string key, T value)
        => _js.InvokeVoidAsync("localStorage.setItem", key, System.Text.Json.JsonSerializer.Serialize(value));

    public async ValueTask<T?> GetItemAsync<T>(string key)
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", key);
        if (string.IsNullOrEmpty(json)) return default;
        try { return System.Text.Json.JsonSerializer.Deserialize<T>(json); } catch { return default; }
    }

    public ValueTask RemoveItemAsync(string key)
        => _js.InvokeVoidAsync("localStorage.removeItem", key);
}

