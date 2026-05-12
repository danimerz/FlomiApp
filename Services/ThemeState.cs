using Microsoft.JSInterop;

namespace FlomiApp.Services;

public class ThemeState
{
    private readonly IJSRuntime _jsRuntime;

    private const string StorageKey = "flomi-theme";

    public string CurrentTheme { get; private set; } = "light";

    public event Action? OnChange;

    public ThemeState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        var storedTheme = await _jsRuntime.InvokeAsync<string?>("themeManager.getTheme", StorageKey);

        if (!string.IsNullOrWhiteSpace(storedTheme))
        {
            CurrentTheme = storedTheme;
        }
        else
        {
            CurrentTheme = "light";
            await _jsRuntime.InvokeVoidAsync("themeManager.setTheme", StorageKey, CurrentTheme);
        }

        OnChange?.Invoke();
    }

    public async Task ToggleAsync()
    {
        CurrentTheme = CurrentTheme == "dark" ? "light" : "dark";

        await _jsRuntime.InvokeVoidAsync("themeManager.setTheme", StorageKey, CurrentTheme);

        OnChange?.Invoke();
    }

    public async Task SetThemeAsync(string theme)
    {
        if (theme != "light" && theme != "dark")
            return;

        CurrentTheme = theme;
        await _jsRuntime.InvokeVoidAsync("themeManager.setTheme", StorageKey, CurrentTheme);

        OnChange?.Invoke();
    }
}
