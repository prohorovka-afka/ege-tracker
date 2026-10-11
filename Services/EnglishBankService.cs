using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    public class EnglishBankService
    {
        private readonly HttpClient _http;
        private readonly HtmlParser _htmlParser = new();

        private static readonly string[] GitHubSources =
        {
            "https://raw.githubusercontent.com/DineshShanu/How-Easy-English-Grammar/main/src/data/lessons.beginner.json",
            "https://raw.githubusercontent.com/DineshShanu/How-Easy-English-Grammar/main/src/data/lessons.intermediate.json",
            "https://raw.githubusercontent.com/DineshShanu/How-Easy-English-Grammar/main/src/data/lessons.advanced.json",
        };

        public EnglishBankService()
        {
            var handler = new System.Net.Http.HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            };
            _http = new HttpClient(handler);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36");
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<int> FetchAllAsync(
            IProgress<string>? progress = null,
            CancellationToken ct = default)
        {
            var allTasks = new List<TaskItem>();
            int taskId = 1;

            foreach (var url in GitHubSources)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report($"Скачиваю {Path.GetFileName(url)}…");

                try
                {
                    var json = await _http.GetStringAsync(url, ct).ConfigureAwait(false);
                    var lessons = ParseGitHubLessons(json);

                    if (lessons.Count == 0)
                    {
                        progress?.Report($"  → пусто, пропускаю");
                        continue;
                    }

                    foreach (var lesson in lessons)
                        foreach (var q in lesson.Questions)
                            allTasks.Add(ConvertToTaskItem(q, lesson.Title, taskId++));

                    progress?.Report($"  → +{lessons.Sum(l => l.Questions.Count)}");
                }
                catch (Exception ex)
                {
                    App.LogError(ex);
                    progress?.Report($"  → ошибка: {ex.Message}");
                }
            }

            if (allTasks.Count == 0)
            {
                progress?.Report("GitHub пуст — fallback EnglishGrammar.org…");
                var htmlTasks = await FetchFromEnglishGrammarOrgAsync(taskId, progress, ct).ConfigureAwait(false);
                allTasks.AddRange(htmlTasks);
            }

            if (allTasks.Count > 0)
            {
                await SaveAsync("english", allTasks).ConfigureAwait(false);
                progress?.Report($"Сохранено {allTasks.Count} → tasks/english.json");
            }
            else
            {
                progress?.Report("Не удалось скачать ни одного задания.");
            }

            return allTasks.Count;
        }

        private static List<LessonData> ParseGitHubLessons(string json)
        {
            var result = new List<LessonData>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement lessonsArray;
                if (root.ValueKind == JsonValueKind.Array) lessonsArray = root;
                else if (root.TryGetProperty("lessons", out var l)) lessonsArray = l;
                else return result;

                foreach (var lesson in lessonsArray.EnumerateArray())
                {
                    var lessonData = new LessonData
                    {
                        Title = lesson.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    };

                    if (lesson.TryGetProperty("exercises", out var exercises) &&
                        exercises.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var ex in exercises.EnumerateArray())
                        {
                            var question = new ExerciseData
                            {
                                QuestionText = ex.TryGetProperty("question", out var q)
                                    ? q.GetString() ?? ""
                                    : ex.TryGetProperty("text", out var tx) ? tx.GetString() ?? "" : "",
                                AnswerText = ex.TryGetProperty("answer", out var a) ? a.GetString() ?? "" : "",
                                Type = ex.TryGetProperty("type", out var ty) ? ty.GetString() ?? "short_answer" : "short_answer",
                            };

                            if (ex.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                            {
                                question.Options = opts.EnumerateArray()
                                    .Select(o => o.GetString() ?? "")
                                    .Where(s => !string.IsNullOrEmpty(s))
                                    .ToList();
                            }

                            if (!string.IsNullOrWhiteSpace(question.QuestionText) &&
                                !string.IsNullOrWhiteSpace(question.AnswerText))
                            {
                                lessonData.Questions.Add(question);
                            }
                        }
                    }

                    if (lessonData.Questions.Count > 0) result.Add(lessonData);
                }
            }
            catch (Exception ex) { App.LogError(ex); }

            return result;
        }

        private async Task<List<TaskItem>> FetchFromEnglishGrammarOrgAsync(
            int startId, IProgress<string>? progress, CancellationToken ct)
        {
            var tasks = new List<TaskItem>();
            int taskId = startId;

            var pages = new[]
            {
                "https://www.englishgrammar.org/english-grammar-exercises/",
                "https://www.englishgrammar.org/grammar-exercise-1/",
                "https://www.englishgrammar.org/grammar-exercise-2/",
            };

            foreach (var page in pages)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report($"EnglishGrammar.org: {page.Split('/').Last()}…");

                try
                {
                    var html = await _http.GetStringAsync(page, ct).ConfigureAwait(false);
                    var doc = await _htmlParser.ParseDocumentAsync(html, ct).ConfigureAwait(false);

                    var questions = doc.QuerySelectorAll("p, li")
                        .Select(e => e.TextContent.Trim())
                        .Where(t => t.Contains("?") && t.Length > 20 && t.Length < 500)
                        .Take(20)
                        .ToList();

                    foreach (var q in questions)
                    {
                        tasks.Add(new TaskItem
                        {
                            Id = $"english_{taskId}",
                            Subject = "english",
                            TaskNumber = 1,
                            KimPosition = 1,
                            TaskType = "short_answer",
                            Question = q,
                            CorrectAnswer = "",
                            MaxPrimaryScore = 1,
                            Prototypes = new List<string> { "1" },
                            FipiId = $"english_eg_{taskId}",
                        });
                        taskId++;
                    }

                    progress?.Report($"  → +{questions.Count}");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
                catch (Exception ex) { App.LogError(ex); }
            }

            return tasks;
        }

        private static TaskItem ConvertToTaskItem(ExerciseData ex, string topicTitle, int id)
        {
            return new TaskItem
            {
                Id = $"english_{id:D5}",
                Subject = "english",
                TaskNumber = 1,
                KimPosition = 1,
                TaskType = ex.Options != null && ex.Options.Count > 0 ? "multiple_choice" : "short_answer",
                Question = string.IsNullOrEmpty(topicTitle) ? ex.QuestionText : $"[{topicTitle}] {ex.QuestionText}",
                ImagePath = null,
                Options = ex.Options,
                CorrectAnswer = ex.AnswerText,
                MaxPrimaryScore = 1,
                VideoUrl = null,
                Prototypes = new List<string> { "1" },
                FipiId = $"english_gh_{id}",
            };
        }

        private static async Task SaveAsync(string subjectId, List<TaskItem> tasks)
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

        private class LessonData
        {
            public string Title { get; set; } = "";
            public List<ExerciseData> Questions { get; set; } = new();
        }

        private class ExerciseData
        {
            public string QuestionText { get; set; } = "";
            public string AnswerText { get; set; } = "";
            public string Type { get; set; } = "short_answer";
            public List<string>? Options { get; set; }
        }
    }
}