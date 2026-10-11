using System;
using System.Windows.Controls;

namespace EgeTracker.Services
{
    /// <summary>
    /// Сервис навигации между вкладками. Контент подставляется в ContentControl главного окна.
    /// Анимация перехода выполняется в MainWindow (View) по событию Navigated.
    /// </summary>
    public class NavigationService
    {
        private ContentControl? _host;

        public object? CurrentView { get; private set; }

        public event EventHandler<NavigationEventArgs>? Navigated;

        public void AttachHost(ContentControl host)
        {
            _host = host;
        }

        public void NavigateTo(object view)
        {
            if (view == null) return;
            var args = new NavigationEventArgs(CurrentView, view);
            CurrentView = view;
            if (_host != null) _host.Content = view;
            Navigated?.Invoke(this, args);
        }
    }

    public class NavigationEventArgs : EventArgs
    {
        public object? From { get; }
        public object To { get; }

        public NavigationEventArgs(object? from, object to)
        {
            From = from;
            To = to;
        }
    }
}