using System;
using System.Windows;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    /// <summary>
    /// Смена светлой/тёмной темы через замену первого MergedDictionary в App.Resources.
    /// Архитектура: App.xaml.MergedDictionaries[0] = тема, [1] = CommonStyles.
    /// </summary>
    public static class ThemeService
    {
        private const string LightUri = "pack://application:,,,/Resources/ThemeLight.xaml";
        private const string DarkUri  = "pack://application:,,,/Resources/ThemeDark.xaml";

        public static void Apply(Application app, string theme)
        {
            if (app == null) return;

            try
            {
                string source = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase)
                    ? DarkUri
                    : LightUri;

                var newDict = new ResourceDictionary { Source = new Uri(source, UriKind.Absolute) };

                // Заменяем [0] — тему. CommonStyles остаётся на [1].
                if (app.Resources.MergedDictionaries.Count > 0)
                {
                    app.Resources.MergedDictionaries[0] = newDict;
                }
                else
                {
                    app.Resources.MergedDictionaries.Add(newDict);
                }

                System.Diagnostics.Debug.WriteLine($"[ThemeService] Applied theme: {theme}");
            }
            catch (Exception ex)
            {
                App.LogError(ex);
            }
        }

        public static void ApplyFromSettings(Application app, AppSettings settings)
        {
            string theme = settings?.Theme ?? "light";
            Apply(app, theme);
        }
    }
}