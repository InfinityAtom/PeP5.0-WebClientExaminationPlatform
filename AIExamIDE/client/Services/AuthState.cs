using System;
using System.Threading;
using System.Threading.Tasks;
using AIExamIDE.Models;

namespace AIExamIDE.Services;

public class AuthState
{
    private readonly ILocalStorage _storage;
    private const string TokenKey = "auth_token";
    private const string UserKey = "auth_user";
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private bool _initialized;

    public event Action? StateChanged;

    public AuthState(ILocalStorage storage)
    {
        _storage = storage;
    }

    public string? Token { get; private set; }
    public UserInfo? User { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
    public bool IsTeacher => User?.Role == "teacher";
    public bool IsStudent => User?.Role == "student";

    public async Task SetAuthAsync(string token, UserInfo user)
    {
        await _mutex.WaitAsync();
        try
        {
            Token = token;
            User = user;
            await _storage.SetItemAsync(TokenKey, token);
            await _storage.SetItemAsync(UserKey, user);
            _initialized = true;
        }
        finally
        {
            _mutex.Release();
        }

        NotifyStateChanged();
    }

    public async Task LogoutAsync()
    {
        await _mutex.WaitAsync();
        try
        {
            Token = null;
            User = null;
            await _storage.RemoveItemAsync(TokenKey);
            await _storage.RemoveItemAsync(UserKey);
            _initialized = false;
        }
        finally
        {
            _mutex.Release();
        }

        NotifyStateChanged();
    }

    public async Task InitializeAsync(Func<Task<UserInfo?>>? fetchUserAsync = null)
    {
        if (_initialized && !string.IsNullOrEmpty(Token) && User != null)
        {
            return;
        }

        await _mutex.WaitAsync();
        try
        {
            if (string.IsNullOrEmpty(Token))
            {
                Token = await _storage.GetItemAsync<string>(TokenKey);
            }

            if (User is null)
            {
                User = await _storage.GetItemAsync<UserInfo>(UserKey);
            }

            if (User is null && !string.IsNullOrEmpty(Token) && fetchUserAsync is not null)
            {
                try
                {
                    var remoteUser = await fetchUserAsync();
                    if (remoteUser is not null)
                    {
                        User = remoteUser;
                        await _storage.SetItemAsync(UserKey, remoteUser);
                    }
                }
                catch
                {
                    // ignore fetch failures; user will stay null until next successful login
                }
            }

            _initialized = true;
        }
        finally
        {
            _mutex.Release();
        }

        NotifyStateChanged();
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
