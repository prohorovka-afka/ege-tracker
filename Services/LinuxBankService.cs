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
    public class LinuxBankService
    {
        private readonly HttpClient _http;
        private readonly HtmlParser _htmlParser = new();

        private static readonly string[] Commands = new[]
        {
            "ls", "cd", "pwd", "cp", "mv", "rm", "mkdir", "rmdir", "touch", "cat",
            "grep", "find", "sed", "awk", "sort", "uniq", "head", "tail", "wc",
            "chmod", "chown", "chgrp", "umask", "ln", "tar", "gzip", "zip", "unzip",
            "ps", "top", "kill", "jobs", "fg", "bg", "nohup", "systemctl", "journalctl",
            "ip", "ss", "ping", "curl", "wget", "ssh", "scp", "rsync", "mount", "umount",
            "df", "du", "lsblk", "docker", "git", "vim", "nano", "echo", "export", "env",
            "man", "which", "whereis", "whoami", "id", "sudo", "su", "passwd", "useradd",
        };

        public LinuxBankService()
        {
            var handler = new System.Net.Http.HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip,
            };
            _http = new HttpClient(handler);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36");
            _http.Timeout = TimeSpan.FromSeconds(20);
        }

        public async Task<int> FetchAllAsync(
            IProgress<string>? progress = null,
            CancellationToken ct = default)
        {
            var tasks = new List<TaskItem>();
            int id = 1;
            int idx = 0;

            foreach (var cmd in Commands)
            {
                ct.ThrowIfCancellationRequested();
                idx++;

                progress?.Report($"[{idx}/{Commands.Length}] {cmd}…");

                try
                {
                    string description = await FetchManDescriptionAsync(cmd, ct).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(description))
                    {
                        progress?.Report($"  → нет описания");
                        continue;
                    }

                    tasks.Add(new TaskItem
                    {
                        Id = $"linux_{id:D4}",
                        Subject = "linux",
                        TaskNumber = 1,
                        KimPosition = 1,
                        TaskType = "short_answer",
                        Question = $"Что делает команда `{cmd}` в Linux?\n\nПодсказка: {Truncate(description, 120)}…",
                        CorrectAnswer = Truncate(description, 200),
                        MaxPrimaryScore = 1,
                        Prototypes = new List<string> { "1" },
                        FipiId = $"linux_man_{cmd}",
                    });
                    id++;

                    tasks.Add(new TaskItem
                    {
                        Id = $"linux_{id:D4}",
                        Subject = "linux",
                        TaskNumber = 2,
                        KimPosition = 2,
                        TaskType = "short_answer",
                        Question = $"Какая команда соответствует:\n\n«{Truncate(description, 150)}»",
                        CorrectAnswer = cmd,
                        MaxPrimaryScore = 1,
                        Prototypes = new List<string> { "2" },
                        FipiId = $"linux_cmd_{cmd}",
                    });
                    id++;

                    await Task.Delay(300, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    App.LogError(ex);
                    progress?.Report($"  → ошибка: {ex.Message}");
                }
            }

            if (tasks.Count > 0)
            {
                await SaveAsync("linux", tasks).ConfigureAwait(false);
                progress?.Report($"Сохранено {tasks.Count} → tasks/linux.json");
            }

            return tasks.Count;
        }

        private async Task<string> FetchManDescriptionAsync(string command, CancellationToken ct)
        {
            try
            {
                string url = $"https://man7.org/linux/man-pages/man1/{command}.1.html";

                var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    url = $"https://man7.org/linux/man-pages/man8/{command}.8.html";
                    response = await _http.GetAsync(url, ct).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode) return "";
                }

                var html = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var doc = await _htmlParser.ParseDocumentAsync(html, ct).ConfigureAwait(false);

                var description = doc.QuerySelectorAll("p")
                    .Select(p => p.TextContent.Trim())
                    .FirstOrDefault(t => t.Length > 50 && !t.Contains("man7.org"));

                return description ?? "";
            }
            catch { return ""; }
        }

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…";

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
    }
}