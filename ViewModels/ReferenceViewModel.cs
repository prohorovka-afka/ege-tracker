using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EgeTracker.Models;
using EgeTracker.Services;

namespace EgeTracker.ViewModels
{
    public class ReferenceViewModel : ViewModelBase
    {
        public ObservableCollection<ReferenceItem> Items { get; } = new();

        private ReferenceItem? _selectedItem;
        public ReferenceItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetField(ref _selectedItem, value))
                {
                    Header = value?.SubjectName ?? string.Empty;
                    ItemSelected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private string _header = string.Empty;
        public string Header
        {
            get => _header;
            set => SetField(ref _header, value);
        }

        public ICommand ReloadCommand { get; }
        public ICommand OpenExternalCommand { get; }

        public event EventHandler? ItemSelected;

        public ReferenceViewModel()
        {
            foreach (var item in App.ReferenceService.Items)
                Items.Add(item);

            SelectedItem = Items.FirstOrDefault();

            ReloadCommand = new RelayCommand(_ => ItemSelected?.Invoke(this, EventArgs.Empty));
            OpenExternalCommand = new RelayCommand(_ => OpenExternal());
        }

        private void OpenExternal()
        {
            if (SelectedItem == null) return;
            App.ReferenceService.OpenExternal(SelectedItem);
        }
    }
}