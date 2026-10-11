using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EgeTracker.Services;
using EgeTracker.ViewModels;

namespace EgeTracker.Views
{
    public partial class ReferenceView : UserControl
    {
        private bool _webViewReady;

        public ReferenceView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private ReferenceViewModel? Vm => DataContext as ReferenceViewModel;

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await Browser.EnsureCoreWebView2Async();

                _webViewReady = true;

                Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;

                // Подписываемся на смену выбранного предмета
                if (Vm != null)
                    Vm.ItemSelected += OnItemSelected;

                // Загружаем текущий
                LoadCurrent();
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                MessageBox.Show(
                    "Не удалось инициализировать WebView2.\n" +
                    "Убедитесь, что установлен WebView2 Runtime: https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (Vm != null)
                Vm.ItemSelected -= OnItemSelected;

            _webViewReady = false;
        }

        private void OnItemSelected(object? sender, EventArgs e)
        {
            LoadCurrent();
        }

        public void LoadCurrent()
        {
            if (!_webViewReady || Vm?.SelectedItem == null) return;

            try
            {
                string html = App.ReferenceService.LoadHtml(Vm.SelectedItem);
                if (string.IsNullOrEmpty(html))
                {
                    html = BuildNotFoundHtml(Vm.SelectedItem.SubjectName);
                }

                // Тема (тёмная/светлая)
                bool isDark = App.SettingsService.Settings.Theme == "dark";
                html = InjectTheme(html, isDark);

                Browser.NavigateToString(html);
            }
            catch (Exception ex)
            {
                App.LogError(ex);
            }
        }

        /// <summary>
        /// Обновление темы (вызывается из ViewModel).
        /// </summary>
        public void ReloadWithTheme()
        {
            LoadCurrent();
        }

        private static string InjectTheme(string html, bool isDark)
        {
            string css = isDark
                ? @"<style>
                    body { background: #1A2332; color: #E5E7EB; }
                    h1, h2, h3 { color: #4F8DF7; }
                    a { color: #6FA4FF; }
                    table { border-color: #334155; }
                    th { background: #263547; }
                    td, th { border: 1px solid #334155; }
                    code, pre { background: #263547; color: #F1F5F9; }
                    blockquote { border-left: 4px solid #4F8DF7; color: #A5B4CB; }
                    .formula { background: #263547; padding: 8px; border-radius: 6px; }
                  </style>"
                : @"<style>
                    body { background: #FFFFFF; color: #111827; }
                    h1, h2, h3 { color: #2563EB; }
                    a { color: #1D4ED8; }
                    table { border-color: #E5E7EB; }
                    th { background: #EEF2F7; }
                    td, th { border: 1px solid #E5E7EB; }
                    code, pre { background: #EEF2F7; color: #111827; }
                    blockquote { border-left: 4px solid #2563EB; color: #6B7280; }
                    .formula { background: #EEF2F7; padding: 8px; border-radius: 6px; }
                  </style>";

            return html.Contains("</head>")
                ? html.Replace("</head>", css + "</head>")
                : css + html;
        }

        private static string BuildNotFoundHtml(string subjectName)
        {
            return @"<!DOCTYPE html>
<html><head><meta charset='utf-8'></head><body style='font-family: Segoe UI; padding: 20px;'>
<h2>Материал не найден</h2>
<p>Для предмета <b>" + subjectName + @"</b> справочник пока не загружен.</p>
<p>Чтобы добавить материал:</p>
<ol>
  <li>Откройте папку <code>Reference/</code> рядом с .exe</li>
  <li>Создайте файл <code>" + subjectName + @".html</code> или положите картинку в <code>Reference/images/</code></li>
  <li>Перезапустите приложение</li>
</ol>
<p>Пример HTML-шаблона — в файлах <code>Reference/*.html</code>, которые создались автоматически.</p>
</body></html>";
        }
    }
}