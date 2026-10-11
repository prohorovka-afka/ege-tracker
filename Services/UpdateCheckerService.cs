using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    public class UpdateCheckerService
    {
        private readonly FipiParserService _parser = new();

        private static readonly string[] SupportedSubjects = { "russian", "math_profile", "informatics" };

        private static readonly Dictionary<string, string> SubjectNames = new()
        {
            ["russian"]      = "Русский язык",
            ["math_profile"] = "Математика (профиль)",
            ["informatics"]  = "Информатика",
        };

        public async Task<List<UpdateCheckResult>> CheckAndUpdateAllAsync(
            IProgress<string>? progress = null,
            CancellationToken ct = default)
        {
            var results = new List<UpdateCheckResult>();

            foreach (var subjectId in SupportedSubjects)
            {
                ct.ThrowIfCancellationRequested();

                progress?.Report($"Проверка: {SubjectNames[subjectId]}…");

                var result = new UpdateCheckResult
                {
                    SubjectId = subjectId,
                    SubjectName = SubjectNames[subjectId],
                };

                try
                {
                    var freshTasks = await _parser.FetchAllTasksAsync(subjectId, progress, ct)
                        .ConfigureAwait(false);

                    result.TotalFound = freshTasks.Count;

                    var existingTasks = LoadExistingTasks(subjectId);
                    result.AlreadyHave = existingTasks.Count;

                    var existingFipiIds = new HashSet<string>(
                        existingTasks
                            .Where(t => !string.IsNullOrEmpty(t.FipiId))
                            .Select(t => t.FipiId!));

                    var newTasks = freshTasks
                        .Where(t => !string.IsNullOrEmpty(t.FipiId) && !existingFipiIds.Contains(t.FipiId!))
                        .ToList();

                    result.NewTasks = newTasks.Count;

                    if (newTasks.Count > 0)
                    {
                        var allTasks = existingTasks.Concat(newTasks)
                            .GroupBy(t => t.FipiId ?? t.Id)
                            .Select(g => g.First())
                            .ToList();

                        await SaveTasksAsync(subjectId, allTasks).ConfigureAwait(false);
                        result.SavedToFile = true;

                        progress?.Report($"{SubjectNames[subjectId]}: +{newTasks.Count} новых");
                    }
                    else
                    {
                        progress?.Report($"{SubjectNames[subjectId]}: новых нет");
                    }
                }
                catch (Exception ex)
                {
                    App.LogError(ex);
                    result.ErrorMessage = ex.Message;
                    progress?.Report($"Ошибка: {SubjectNames[subjectId]} — {ex.Message}");
                }

                results.Add(result);
            }

            return results;
        }

        private static List<TaskItem> LoadExistingTasks(string subjectId)
        {
            try
            {
                string path = Path.Combine(App.BaseDir, "tasks", $"{subjectId}.json");
                if (!File.Exists(path)) return new List<TaskItem>();

                string json = File.ReadAllText(path);
                var tasks = JsonSerializer.Deserialize<List<TaskItem>>(json);
                return tasks ?? new List<TaskItem>();
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                return new List<TaskItem>();
            }
        }

        private static async Task SaveTasksAsync(string subjectId, List<TaskItem> tasks)
        {
            try
            {
                string dir = Path.Combine(App.BaseDir, "tasks");
                Directory.CreateDirectory(dir);

                string path = Path.Combine(dir, $"{subjectId}.json");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string json = JsonSerializer.Serialize(tasks, options);
                await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
            }
            catch (Exception ex) { App.LogError(ex); }
        }
    }
}