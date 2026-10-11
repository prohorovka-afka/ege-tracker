using System;
using System.IO;
using System.Threading.Tasks;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    public class AppStateService
    {
        private static string StatePath => Path.Combine(App.BaseDir, "AppState.json");

        public AppState State { get; private set; } = new AppState();

        public async Task LoadAsync()
        {
            try
            {
                var loaded = await JsonHelper.ReadAsync<AppState>(StatePath).ConfigureAwait(false);
                State = loaded ?? new AppState();
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                State = new AppState();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                State.SavedAt = DateTime.Now;
                await JsonHelper.WriteAsync(StatePath, State).ConfigureAwait(false);
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        /// <summary>
        /// Fire-and-forget сохранение — вызывается из мест, где await не нужен.
        /// </summary>
        public void SaveDebounced()
        {
            _ = SaveAsync();
        }
    }
}