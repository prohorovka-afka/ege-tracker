using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using EgeTracker.Services;
using EgeTracker.Views;

namespace EgeTracker
{
    public partial class App : Application
    {
        public static string BaseDir { get; private set; } = AppDomain.CurrentDomain.BaseDirectory;

        public static SettingsService SettingsService { get; private set; } = null!;
        public static AppStateService AppStateService { get; private set; } = null!;
        public static SubjectConfigService SubjectConfigService { get; private set; } = null!;
        public static SubjectThemesService SubjectThemesService { get; private set; } = null!;
        public static ProgressService ProgressService { get; private set; } = null!;
        public static TaskService TaskService { get; private set; } = null!;
        public static ScoreService ScoreService { get; private set; } = null!;
        public static VariantGeneratorService VariantGeneratorService { get; private set; } = null!;
        public static ReferenceService ReferenceService { get; private set; } = null!;

        private SplashWindow? _splash;

        public App()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                SettingsService = new SettingsService();
                await SettingsService.LoadAsync().ConfigureAwait(true);

                AppStateService = new AppStateService();
                await AppStateService.LoadAsync().ConfigureAwait(true);

                ThemeService.ApplyFromSettings(this, SettingsService.Settings);

                _splash = new SplashWindow();
                _splash.Show();

                await InitializeOtherServicesAsync().ConfigureAwait(true);

                if (_splash != null)
                {
                    await _splash.FinishAsync().ConfigureAwait(true);
                    _splash = null;
                }

                var main = new MainWindow();
                MainWindow = main;
                main.Show();

                ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            catch (Exception ex)
            {
                LogError(ex);
                MessageBox.Show("Ошибка запуска приложения. Подробности в error.log",
                    "EgeTracker", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private static async Task InitializeOtherServicesAsync()
        {
            SubjectConfigService = new SubjectConfigService();
            await SubjectConfigService.LoadAsync().ConfigureAwait(false);

            SubjectThemesService = new SubjectThemesService();
            await SubjectThemesService.LoadAsync().ConfigureAwait(false);

            TaskService = new TaskService();
            await TaskService.LoadAsync().ConfigureAwait(false);

            ScoreService = new ScoreService();
            await ScoreService.LoadAsync().ConfigureAwait(false);

            ProgressService = new ProgressService();
            await ProgressService.LoadAsync().ConfigureAwait(false);

            VariantGeneratorService = new VariantGeneratorService();

            ReferenceService = new ReferenceService();
            await ReferenceService.LoadAsync().ConfigureAwait(false);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                if (SettingsService != null) await SettingsService.SaveAsync().ConfigureAwait(false);
                if (AppStateService != null) await AppStateService.SaveAsync().ConfigureAwait(false);
                if (ProgressService != null) await ProgressService.SaveAsync().ConfigureAwait(false);
            }
            catch (Exception ex) { LogError(ex); }
            finally { base.OnExit(e); }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError(e.Exception);
            e.Handled = true;
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex) LogError(ex);
        }

        public static void LogError(Exception ex)
        {
            try
            {
                string path = Path.Combine(BaseDir, "error.log");
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n---\n";
                File.AppendAllText(path, line);
            }
            catch { }
        }
    }
}