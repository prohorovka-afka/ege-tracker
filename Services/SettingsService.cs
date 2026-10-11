using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    public class SettingsService
    {
        private static string SettingsPath =>
            Path.Combine(App.BaseDir, "settings.json");

        public AppSettings Settings { get; private set; } = new AppSettings();

        public event EventHandler? ThemeChanged;
        public event EventHandler? BackgroundChanged;

        public async Task LoadAsync()
        {
            try
            {
                var loaded = await JsonHelper.ReadAsync<AppSettings>(SettingsPath).ConfigureAwait(false);
                Settings = loaded ?? new AppSettings();
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                Settings = new AppSettings();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                await JsonHelper.WriteAsync(SettingsPath, Settings).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                App.LogError(ex);
            }
        }

        public async Task ToggleThemeAsync(Application app)
        {
            Settings.Theme = Settings.Theme == "dark" ? "light" : "dark";
            ThemeService.Apply(app, Settings.Theme);
            ThemeChanged?.Invoke(this, EventArgs.Empty);
            await SaveAsync().ConfigureAwait(false);
        }

        public async Task SetThemeAsync(Application app, string theme)
        {
            if (Settings.Theme == theme) return;
            Settings.Theme = theme;
            ThemeService.Apply(app, Settings.Theme);
            ThemeChanged?.Invoke(this, EventArgs.Empty);
            await SaveAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Установить фон. Сохраняет и уведомляет подписчиков.
        /// </summary>
        public async Task SetBackgroundAsync(string? path, double opacity = 0.35)
        {
            Settings.BackgroundImagePath = path;
            Settings.BackgroundOpacity = opacity;
            BackgroundChanged?.Invoke(this, EventArgs.Empty);
            await SaveAsync().ConfigureAwait(false);
        }
    }
}