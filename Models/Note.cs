using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    /// <summary>
    /// Заметка пользователя (RTF-содержимое).
    /// </summary>
    public class Note : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _title = "Новая заметка";
        private string _rtfContent = string.Empty;
        private DateTime _modified = DateTime.Now;

        [JsonPropertyName("id")]
        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("title")]
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// RTF-содержимое (base64 или напрямую? храним строкой как есть).
        /// </summary>
        [JsonPropertyName("rtf")]
        public string RtfContent
        {
            get => _rtfContent;
            set { _rtfContent = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("modified")]
        public DateTime Modified
        {
            get => _modified;
            set { _modified = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}