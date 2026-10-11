using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using EgeTracker.Services;
using EgeTracker.ViewModels;

namespace EgeTracker.Views
{
    public partial class NotesView : UserControl
    {
        private bool _initialized;
        private bool _suppressEditorEvent;
        private bool _suppressSelectionEvent;
        private readonly AutoSaveService _autoSave = new(TimeSpan.FromSeconds(2));

        public NotesView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private NotesViewModel? Vm => DataContext as NotesViewModel;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _initialized = true;
                if (Vm != null) Vm.SelectedNoteChanged += OnSelectedNoteChanged;
                LoadCurrentNoteRtf();

                _autoSave.Start(() => Vm?.Save());
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _autoSave.Stop();
                SaveCurrentNoteRtf();
                if (Vm != null)
                {
                    Vm.SelectedNoteChanged -= OnSelectedNoteChanged;
                    Vm.Save();
                }
                _initialized = false;
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnSelectedNoteChanged(object? sender, EventArgs e)
        {
            SaveCurrentNoteRtf();
            LoadCurrentNoteRtf();
        }

        private void OnNoteDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is ListBoxItem item && item.DataContext is EgeTracker.Models.Note note)
                {
                    if (Vm != null) Vm.SelectedNote = note;
                    Vm?.RenameSelectedNote();
                }
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.B) { OnBoldClick(this, new RoutedEventArgs()); e.Handled = true; }
                else if (e.Key == Key.I) { OnItalicClick(this, new RoutedEventArgs()); e.Handled = true; }
                else if (e.Key == Key.U) { OnUnderlineClick(this, new RoutedEventArgs()); e.Handled = true; }
            }
        }

        private void OnBoldClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Editor == null) return;
                var sel = Editor.Selection;
                var current = sel.GetPropertyValue(TextElement.FontWeightProperty);
                var newWeight = Equals(current, FontWeights.Bold) ? FontWeights.Normal : FontWeights.Bold;
                sel.ApplyPropertyValue(TextElement.FontWeightProperty, newWeight);
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnItalicClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Editor == null) return;
                var sel = Editor.Selection;
                var current = sel.GetPropertyValue(TextElement.FontStyleProperty);
                var newStyle = Equals(current, FontStyles.Italic) ? FontStyles.Normal : FontStyles.Italic;
                sel.ApplyPropertyValue(TextElement.FontStyleProperty, newStyle);
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnUnderlineClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Editor == null) return;
                var sel = Editor.Selection;
                var deco = sel.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
                if (deco != null && deco.Count > 0)
                    sel.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
                else
                    sel.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Underline);
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnAlignLeftClick(object sender, RoutedEventArgs e) => ApplyAlignment(TextAlignment.Left);
        private void OnAlignCenterClick(object sender, RoutedEventArgs e) => ApplyAlignment(TextAlignment.Center);
        private void OnAlignRightClick(object sender, RoutedEventArgs e) => ApplyAlignment(TextAlignment.Right);

        private void ApplyAlignment(TextAlignment alignment)
        {
            try
            {
                if (Editor == null) return;
                var sel = Editor.Selection;
                var start = sel.Start.Paragraph;
                var end = sel.End.Paragraph;
                if (start == null && end == null)
                {
                    if (Editor.CaretPosition?.Paragraph != null)
                        Editor.CaretPosition.Paragraph.TextAlignment = alignment;
                    return;
                }
                var current = start;
                while (current != null)
                {
                    current.TextAlignment = alignment;
                    if (current == end) break;
                    if (current.NextBlock is Paragraph nextPara) current = nextPara;
                    else break;
                }
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnFontSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_initialized || _suppressSelectionEvent) return;
                if (Editor == null) return;
                if (FontSizeCombo?.SelectedItem is not ComboBoxItem item) return;
                if (!double.TryParse(item.Content?.ToString(), out double size)) return;
                Editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnFontSizeDownClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Editor == null) return;
                double current = GetCurrentFontSize();
                double next = Math.Max(8, current - 2);
                Editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, next);
                SyncFontSizeCombo(next);
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnFontSizeUpClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Editor == null) return;
                double current = GetCurrentFontSize();
                double next = Math.Min(96, current + 2);
                Editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, next);
                SyncFontSizeCombo(next);
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private double GetCurrentFontSize()
        {
            try
            {
                if (Editor?.Selection == null) return 16;
                var value = Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty);
                if (value is double d && d > 0) return d;
                return 16;
            }
            catch { return 16; }
        }

        private void SyncFontSizeCombo(double size)
        {
            try
            {
                if (FontSizeCombo == null) return;
                _suppressSelectionEvent = true;
                for (int i = 0; i < FontSizeCombo.Items.Count; i++)
                {
                    if (FontSizeCombo.Items[i] is ComboBoxItem item &&
                        double.TryParse(item.Content?.ToString(), out double val) &&
                        Math.Abs(val - size) < 1.0)
                    {
                        FontSizeCombo.SelectedIndex = i;
                        break;
                    }
                }
                _suppressSelectionEvent = false;
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnColorChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_initialized || _suppressSelectionEvent) return;
                if (Editor == null) return;
                if (ColorCombo?.SelectedItem is not ComboBoxItem item) return;
                string? hex = item.Tag?.ToString();
                if (string.IsNullOrWhiteSpace(hex)) return;
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                Editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnEditorSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!_initialized || _suppressSelectionEvent) return;
            try
            {
                var sel = Editor.Selection;
                if (sel == null) return;

                var weight = sel.GetPropertyValue(TextElement.FontWeightProperty);
                BoldBtn.Opacity = Equals(weight, FontWeights.Bold) ? 1.0 : 0.6;

                var style = sel.GetPropertyValue(TextElement.FontStyleProperty);
                ItalicBtn.Opacity = Equals(style, FontStyles.Italic) ? 1.0 : 0.6;

                var deco = sel.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
                UnderlineBtn.Opacity = (deco != null && deco.Count > 0) ? 1.0 : 0.6;

                SyncFontSizeCombo(GetCurrentFontSize());
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void LoadCurrentNoteRtf()
        {
            try
            {
                var vm = Vm;
                if (vm == null || Editor == null || Editor.Document == null) return;

                _suppressEditorEvent = true;
                _suppressSelectionEvent = true;

                Editor.Document.Blocks.Clear();
                string rtf = vm.SelectedNoteRtf;

                if (!string.IsNullOrEmpty(rtf))
                {
                    var range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(rtf));
                    range.Load(ms, DataFormats.Rtf);
                }
                else
                {
                    Editor.Document.Blocks.Add(new Paragraph(new Run("")));
                }
            }
            catch (Exception ex) { App.LogError(ex); }
            finally
            {
                _suppressEditorEvent = false;
                _suppressSelectionEvent = false;
            }
        }

        private void SaveCurrentNoteRtf()
        {
            try
            {
                var vm = Vm;
                if (vm == null || Editor == null || Editor.Document == null) return;
                var range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);
                using var ms = new MemoryStream();
                range.Save(ms, DataFormats.Rtf);
                ms.Position = 0;
                using var sr = new StreamReader(ms, Encoding.UTF8);
                vm.UpdateCurrentRtf(sr.ReadToEnd());
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_initialized || _suppressEditorEvent) return;
            try
            {
                SaveCurrentNoteRtf();
                _autoSave.MarkDirty();
            }
            catch (Exception ex) { App.LogError(ex); }
        }
    }
}