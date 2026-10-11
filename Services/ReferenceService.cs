using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    public class ReferenceService
    {
        private static string ReferenceDir => Path.Combine(App.BaseDir, "Reference");
        private static string ImagesDir => Path.Combine(ReferenceDir, "images");

        public List<ReferenceItem> Items { get; private set; } = new();

        public async Task LoadAsync()
        {
            try
            {
                Directory.CreateDirectory(ReferenceDir);
                Directory.CreateDirectory(ImagesDir);

                Items = new List<ReferenceItem>
                {
                    new() { SubjectId = "math_profile", SubjectName = "Математика (профиль)", Title = "Формулы и теоремы", FilePath = "Reference/math_profile.html", Kind = "html" },
                    new() { SubjectId = "russian",      SubjectName = "Русский язык",         Title = "Правила и нормы",     FilePath = "Reference/russian.html",      Kind = "html" },
                    new() { SubjectId = "informatics",  SubjectName = "Информатика",          Title = "Алгоритмы и код",     FilePath = "Reference/informatics.html",  Kind = "html" },
                    new() { SubjectId = "english",      SubjectName = "Английский язык",      Title = "Grammar & Vocabulary", FilePath = "Reference/english.html",     Kind = "html" },
                    new() { SubjectId = "linux",        SubjectName = "Linux",                Title = "Команды и файлы",     FilePath = "Reference/linux.html",        Kind = "html" },
                };

                await Task.Yield();
            }
            catch (Exception ex)
            {
                App.LogError(ex);
            }
        }

        /// <summary>
        /// Загрузить HTML-файл. Если нет — возвращает пустую строку.
        /// </summary>
        public string LoadHtml(ReferenceItem item)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.FilePath)) return "";
                string fullPath = Path.Combine(App.BaseDir, item.FilePath);
                if (!File.Exists(fullPath)) return "";
                return File.ReadAllText(fullPath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                return "";
            }
        }

        public bool OpenExternal(ReferenceItem item)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.FilePath)) return false;
                string fullPath = Path.Combine(App.BaseDir, item.FilePath);
                if (!File.Exists(fullPath)) return false;

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true,
                });
                return true;
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                return false;
            }
        }
    }
}