using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EgeTracker.Models;
using EgeTracker.Services;

namespace EgeTracker.ViewModels
{
    public class NotesViewModel : ViewModelBase
    {
        public ObservableCollection<Note> Notes { get; } = new();

        private Note? _selectedNote;
        public Note? SelectedNote
        {
            get => _selectedNote;
            set
            {
                if (SetField(ref _selectedNote, value))
                {
                    OnPropertyChanged(nameof(SelectedNoteRtf));
                    SelectedNoteChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string SelectedNoteRtf => SelectedNote?.RtfContent ?? string.Empty;

        public ICommand AddNoteCommand { get; }
        public ICommand DeleteNoteCommand { get; }
        public ICommand RenameNoteCommand { get; }
        public ICommand SaveCommand { get; }

        public event EventHandler? SelectedNoteChanged;

        private static string NotesPath =>
            Path.Combine(App.BaseDir, "notes", "notes.json");

        public NotesViewModel()
        {
            LoadNotes();

            if (Notes.Count == 0)
                Notes.Add(new Note { Title = "Заметка 1" });

            SelectedNote = Notes.FirstOrDefault();

            AddNoteCommand    = new RelayCommand(_ => AddNote());
            DeleteNoteCommand = new RelayCommand(_ => DeleteSelectedNote());
            RenameNoteCommand = new RelayCommand(_ => RenameSelectedNote());
            SaveCommand       = new RelayCommand(_ => Save());
        }

        // Публичные методы — вызываются из NotesView.xaml.cs
        public void RenameSelectedNote()
        {
            try
            {
                if (SelectedNote == null) return;

                string newTitle = ShowInputDialog(
                    "Переименовать заметку",
                    "Введите новое имя:",
                    SelectedNote.Title);

                if (string.IsNullOrWhiteSpace(newTitle)) return;

                SelectedNote.Title = newTitle.Trim();
                SelectedNote.Modified = DateTime.Now;
                OnPropertyChanged(nameof(Notes));
                Save();
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        public void DeleteSelectedNote()
        {
            try
            {
                if (SelectedNote == null) return;

                var result = MessageBox.Show(
                    $"Удалить заметку «{SelectedNote.Title}»?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                Notes.Remove(SelectedNote);
                SelectedNote = Notes.FirstOrDefault();
                Save();
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void LoadNotes()
        {
            try
            {
                if (!File.Exists(NotesPath)) return;
                var list = JsonHelper.ReadAsync<ObservableCollection<Note>>(NotesPath)
                    .GetAwaiter().GetResult();
                if (list != null)
                    foreach (var n in list)
                        Notes.Add(n);
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(NotesPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                JsonHelper.WriteAsync(NotesPath, Notes).GetAwaiter().GetResult();
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void AddNote()
        {
            var n = new Note { Title = $"Заметка {Notes.Count + 1}" };
            Notes.Add(n);
            SelectedNote = n;
            Save();
        }

        public void UpdateCurrentRtf(string rtf)
        {
            if (SelectedNote == null) return;
            SelectedNote.RtfContent = rtf;
            SelectedNote.Modified = DateTime.Now;
        }

        private static string ShowInputDialog(string title, string prompt, string defaultValue)
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current?.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            if (Application.Current?.Resources["BrushSurface"] is System.Windows.Media.Brush bg)
                window.Background = bg;

            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

            var label = new System.Windows.Controls.TextBlock
            {
                Text = prompt,
                Margin = new Thickness(0, 0, 0, 8)
            };
            if (Application.Current?.Resources["BrushTextPrimary"] is System.Windows.Media.Brush fg)
                label.Foreground = fg;
            System.Windows.Controls.Grid.SetRow(label, 0);
            grid.Children.Add(label);

            var textBox = new System.Windows.Controls.TextBox
            {
                Text = defaultValue,
                Padding = new Thickness(8, 6, 8, 6)
            };
            System.Windows.Controls.Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            var buttons = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };

            string result = string.Empty;

            var okBtn = new System.Windows.Controls.Button
            {
                Content = "ОК", Width = 80, Margin = new Thickness(0, 0, 8, 0)
            };
            okBtn.Click += (s, e) => { result = textBox.Text; window.DialogResult = true; window.Close(); };
            buttons.Children.Add(okBtn);

            var cancelBtn = new System.Windows.Controls.Button { Content = "Отмена", Width = 80 };
            cancelBtn.Click += (s, e) => { window.DialogResult = false; window.Close(); };
            buttons.Children.Add(cancelBtn);

            System.Windows.Controls.Grid.SetRow(buttons, 2);
            grid.Children.Add(buttons);

            window.Content = grid;
            textBox.Focus();
            textBox.SelectAll();

            return window.ShowDialog() == true ? result : string.Empty;
        }
    }
}