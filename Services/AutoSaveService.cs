using System;
using System.Windows.Threading;

namespace EgeTracker.Services
{
    /// <summary>
    /// Таймер автосохранения — срабатывает только когда есть несохранённые изменения.
    /// </summary>
    public class AutoSaveService
    {
        private readonly DispatcherTimer _timer;
        private bool _dirty;
        private Action? _saveAction;

        public AutoSaveService(TimeSpan interval)
        {
            _timer = new DispatcherTimer { Interval = interval };
            _timer.Tick += (_, __) =>
            {
                if (!_dirty) return;
                try
                {
                    _saveAction?.Invoke();
                    _dirty = false;
                }
                catch (Exception ex) { App.LogError(ex); }
            };
        }

        public void Start(Action saveAction)
        {
            _saveAction = saveAction;
            _timer.Start();
        }

        public void Stop() => _timer.Stop();
        public void MarkDirty() => _dirty = true;
    }
}