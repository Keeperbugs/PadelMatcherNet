using Microsoft.JSInterop;

namespace PadelMatcherNet.Services
{
    public enum ThemeMode
    {
        Light,
        Dark,
        Auto
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public ThemeMode Mode { get; set; }
        public bool IsDarkMode { get; set; }
    }

    public interface IUnifiedThemeService
    {
        Task<ThemeMode> GetThemeModeAsync();
        Task SetThemeModeAsync(ThemeMode mode);
        Task<bool> GetIsDarkModeAsync();
        event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    }

    public class UnifiedThemeService : IUnifiedThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string THEME_COOKIE_NAME = "padel_theme_mode";

        public UnifiedThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public async Task<ThemeMode> GetThemeModeAsync()
        {
            try
            {
                var cookieValue = await _jsRuntime.InvokeAsync<string>("getCookie", THEME_COOKIE_NAME);

                if (!string.IsNullOrEmpty(cookieValue))
                {
                    return cookieValue switch
                    {
                        "Light" => ThemeMode.Light,
                        "Dark" => ThemeMode.Dark,
                        "Auto" => ThemeMode.Auto,
                        _ => ThemeMode.Auto
                    };
                }
            }
            catch (InvalidOperationException)
            {
                // Prerendering - ritorna default senza errore
                return ThemeMode.Auto;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting theme mode: {ex.Message}");
            }

            return ThemeMode.Auto; // Default
        }

        public async Task SetThemeModeAsync(ThemeMode mode)
        {
            try
            {
                // 1. Salva nel cookie per 365 giorni
                await _jsRuntime.InvokeVoidAsync("setCookie", THEME_COOKIE_NAME, mode.ToString(), 365);

                // 2. Calcola se deve essere dark mode
                var isDarkMode = await CalculateIsDarkMode(mode);

                // 3. Applica il tema immediatamente via JavaScript
                await _jsRuntime.InvokeVoidAsync("applyThemeImmediately", isDarkMode);

                // 4. Notifica il cambiamento
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
                {
                    Mode = mode,
                    IsDarkMode = isDarkMode
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting theme mode: {ex.Message}");
            }
        }

        public async Task<bool> GetIsDarkModeAsync()
        {
            try
            {
                var mode = await GetThemeModeAsync();
                return await CalculateIsDarkMode(mode);
            }
            catch (InvalidOperationException)
            {
                // Prerendering - default safe
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting dark mode: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CalculateIsDarkMode(ThemeMode mode)
        {
            return mode switch
            {
                ThemeMode.Dark => true,
                ThemeMode.Light => false,
                ThemeMode.Auto => await DetectSystemDarkModeAsync(),
                _ => false
            };
        }

        private async Task<bool> DetectSystemDarkModeAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<bool>("detectSystemDarkMode");
            }
            catch
            {
                return false;
            }
        }
    }
}