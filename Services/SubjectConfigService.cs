using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    /// <summary>
    /// Загружает конфигурацию предметов из tasks/subjects.json.
    /// Если файла нет — использует встроенный дефолт.
    /// </summary>
    public class SubjectConfigService
    {
        private static string ConfigPath =>
            Path.Combine(App.BaseDir, "tasks", "subjects.json");

        public List<SubjectConfig> Subjects { get; private set; } = new List<SubjectConfig>();

        public async Task LoadAsync()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var loaded = await JsonHelper
                        .ReadAsync<SubjectsConfig>(ConfigPath)
                        .ConfigureAwait(false);

                    if (loaded?.Subjects != null && loaded.Subjects.Count > 0)
                    {
                        Subjects = loaded.Subjects;
                        System.Diagnostics.Debug.WriteLine(
                            $"[SubjectConfig] Loaded {Subjects.Count} subjects from {ConfigPath}");
                        return;
                    }
                }

                // Файл отсутствует или пустой — используем дефолт и создаём файл
                Subjects = BuildDefault();
                await SaveDefaultAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                Subjects = BuildDefault();
            }
        }

        private async Task SaveDefaultAsync()
        {
            try
            {
                string? dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var wrapper = new SubjectsConfig { Subjects = Subjects };
                await JsonHelper.WriteAsync(ConfigPath, wrapper).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                App.LogError(ex);
            }
        }

        public SubjectConfig? FindById(string id) =>
            Subjects.FirstOrDefault(s => s.Id == id);

        private static List<SubjectConfig> BuildDefault()
        {
            return new List<SubjectConfig>
            {
                new SubjectConfig
                {
                    Id = "math_profile",
                    Name = "Математика (профиль)",
                    IsEge = true,
                    ScoreScaleKey = "math_profile",
                    TasksCount = 19,
                    TaskNumbers = Enumerable.Range(1, 19).ToList()
                },
                new SubjectConfig
                {
                    Id = "russian",
                    Name = "Русский язык",
                    IsEge = true,
                    ScoreScaleKey = "russian",
                    TasksCount = 27,
                    TaskNumbers = Enumerable.Range(1, 27).ToList()
                },
                new SubjectConfig
                {
                    Id = "informatics",
                    Name = "Информатика",
                    IsEge = true,
                    ScoreScaleKey = "informatics",
                    TasksCount = 27,
                    TaskNumbers = Enumerable.Range(1, 27).ToList()
                },
                new SubjectConfig
                {
                    Id = "linux",
                    Name = "Linux",
                    IsEge = false,
                    ScoreScaleKey = null,
                    TasksCount = 18,
                    TaskNumbers = Enumerable.Range(1, 18).ToList()
                },
                new SubjectConfig
                {
                    Id = "english",
                    Name = "Английский язык",
                    IsEge = false,
                    ScoreScaleKey = null,
                    TasksCount = 18,
                    TaskNumbers = Enumerable.Range(1, 18).ToList()
                }
            };
        }
    }
}