using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    /// <summary>
    /// Встроенный парсер ФИПИ через HttpClient + AngleSharp.
    /// </summary>
    public class FipiParserService
    {
        private const string BaseUrl = "https://ege.fipi.ru/bank";
        private const string BasePageUrl = "https://ege.fipi.ru/bank/questions.php";

        private static readonly Dictionary<string, string> ProjIds = new()
        {
            ["russian"]      = "AF0ED3F2557F8FFC4C06F80B6803FD26",
            ["math_profile"] = "AC437B34557F88EA4115D2F374B0A07B",
            ["informatics"]  = "B9ACA5BBB2E19E434CD6BEC25284C67F",
        };

        private static readonly Dictionary<string, int> TotalPages = new()
        {
            ["russian"]      = 115,
            ["math_profile"] = 115,
            ["informatics"]  = 80,
        };

        private readonly HttpClient _http;
        private readonly HtmlParser _htmlParser = new();
        private readonly string _tasksDir;
        private readonly string _imagesDir;

        public FipiParserService()
        {
            _tasksDir = Path.Combine(App.BaseDir, "tasks");
            _imagesDir = Path.Combine(_tasksDir, "images");

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = true,
                CookieContainer = new CookieContainer(),
            };

            _http = new HttpClient(handler);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _http.DefaultRequestHeaders.Accept.ParseAdd(
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en;q=0.8");
            _http.DefaultRequestHeaders.Referrer = new Uri("https://ege.fipi.ru/bank/");
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<List<TaskItem>> FetchAllTasksAsync(
            string subjectId,
            IProgress<string>? progress = null,
            CancellationToken ct = default)
        {
            var result = new List<TaskItem>();
            var seenIds = new HashSet<string>();

            if (!ProjIds.TryGetValue(subjectId, out var proj))
            {
                progress?.Report($"Неизвестный предмет: {subjectId}");
                return result;
            }

            int totalPages = TotalPages.GetValueOrDefault(subjectId, 115);

            for (int page = 0; page < totalPages; page++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    progress?.Report($"Страница {page + 1} из {totalPages}…");

                    string url = page == 0
                        ? $"{BaseUrl}/questions.php?proj={proj}&init_filter_themes=1&page=0&pagesize=10"
                        : $"{BaseUrl}/questions.php?proj={proj}&page={page}&pagesize=10";

                    byte[] bytes = await _http.GetByteArrayAsync(url, ct).ConfigureAwait(false);
                    string html = Encoding.GetEncoding("windows-1251").GetString(bytes);

                    var doc = await _htmlParser.ParseDocumentAsync(html, ct).ConfigureAwait(false);
                    var qblocks = doc.QuerySelectorAll("div.qblock");

                    if (qblocks.Length == 0)
                    {
                        progress?.Report($"Страница {page + 1}: заданий нет — завершаю.");
                        break;
                    }

                    foreach (var qblock in qblocks)
                    {
                        try
                        {
                            var task = await ParseOneTaskAsync(subjectId, qblock, doc, ct).ConfigureAwait(false);
                            if (task != null && seenIds.Add(task.FipiId ?? task.Id))
                                result.Add(task);
                        }
                        catch (Exception ex) { App.LogError(ex); }
                    }

                    progress?.Report($"Страница {page + 1}: +{qblocks.Length} (всего {result.Count})");
                    await Task.Delay(1200, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    App.LogError(ex);
                    progress?.Report($"Ошибка стр. {page + 1}: {ex.Message}");
                }
            }

            return result;
        }

        private async Task<TaskItem?> ParseOneTaskAsync(
            string subjectId, IElement qblock, IDocument doc, CancellationToken ct)
        {
            string id = qblock.GetAttribute("id") ?? "";
            if (!id.StartsWith("q")) return null;
            string shortId = id.Substring(1);

            var guidInput = qblock.QuerySelector("input[name='guid']");
            string guid = guidInput?.GetAttribute("value") ?? "";

            var textCell = qblock.QuerySelector("td.cell_0");
            if (textCell == null) return null;

            string question = CleanText(textCell);
            var imageUrls = ExtractImages(textCell);

            string infoId = "i" + shortId;
            var infoBlock = doc.QuerySelector($"div#{infoId}");
            string kes = "";
            string taskType = "short_answer";

            if (infoBlock != null)
            {
                var kesRows = infoBlock.QuerySelectorAll("td.param-row div");
                kes = string.Join("; ", kesRows.Select(d => d.TextContent.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

                var allTd = infoBlock.QuerySelectorAll("td").ToList();
                for (int i = 0; i < allTd.Count; i++)
                {
                    if (allTd[i].TextContent.Contains("Тип ответа") && i + 1 < allTd.Count)
                    {
                        var typeText = allTd[i + 1].TextContent.Trim();
                        if (typeText.Contains("Развернутый") || typeText.Contains("Развёрнутый"))
                            taskType = "extended_answer";
                        else if (typeText.Contains("Выбор"))
                            taskType = "multiple_choice";
                        else
                            taskType = "short_answer";
                        break;
                    }
                }
            }

            int kimPosition = 0;
            var kimMatch = Regex.Match(kes, @"^(\d+)");
            if (kimMatch.Success) kimPosition = int.Parse(kimMatch.Groups[1].Value);

            string? imagePath = null;
            for (int i = 0; i < imageUrls.Count; i++)
            {
                var saved = await DownloadImageAsync(imageUrls[i], subjectId, shortId, i, ct).ConfigureAwait(false);
                if (saved != null && imagePath == null) imagePath = saved;
                await Task.Delay(200, ct).ConfigureAwait(false);
            }

            return new TaskItem
            {
                Id = $"{subjectId}_{shortId}",
                Subject = subjectId,
                TaskNumber = kimPosition,
                KimPosition = kimPosition,
                TaskType = taskType,
                Question = question,
                ImagePath = imagePath,
                Options = null,
                CorrectAnswer = "",
                MaxPrimaryScore = taskType == "short_answer" ? 1 : 2,
                VideoUrl = null,
                Prototypes = kimPosition > 0 ? new List<string> { kimPosition.ToString() } : new(),
                FipiId = shortId,
            };
        }

        private static string CleanText(IElement element)
        {
            var sb = new StringBuilder();
            foreach (var node in element.ChildNodes)
                ExtractTextRecursive(node, sb);
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private static void ExtractTextRecursive(INode node, StringBuilder sb)
        {
            if (node is IText textNode) { sb.Append(textNode.Data); }
            else if (node is IElement el)
            {
                string tag = el.TagName.ToUpperInvariant();
                if (tag == "SCRIPT" || tag == "STYLE") return;
                if (tag.StartsWith("M:")) { sb.Append(el.TextContent); return; }
                foreach (var child in el.ChildNodes)
                    ExtractTextRecursive(child, sb);
            }
        }

        private static List<string> ExtractImages(IElement textCell)
        {
            var images = new List<string>();
            foreach (var img in textCell.QuerySelectorAll("img"))
            {
                var src = img.GetAttribute("src");
                if (string.IsNullOrWhiteSpace(src) || src.StartsWith("data:")) continue;
                var fullUrl = ResolveUrl(src);
                if (!images.Contains(fullUrl)) images.Add(fullUrl);
            }
            foreach (var script in textCell.QuerySelectorAll("script"))
            {
                var scriptText = script.TextContent;
                var patterns = new[]
                {
                    @"ShowPictureQ\('([^']+)'\)",
                    @"ShowPictureQ2WH\('([^']+)'",
                    @"ShowPicture2\('([^']+)'",
                };
                foreach (var p in patterns)
                    foreach (Match m in Regex.Matches(scriptText, p))
                    {
                        var fullUrl = ResolveUrl(m.Groups[1].Value);
                        if (!images.Contains(fullUrl)) images.Add(fullUrl);
                    }
            }
            return images;
        }

        private static string ResolveUrl(string path)
        {
            if (path.StartsWith("http://") || path.StartsWith("https://")) return path;
            if (path.StartsWith("/")) return "https://ege.fipi.ru" + path;
            return new Uri(new Uri(BasePageUrl), path).ToString();
        }

        private async Task<string?> DownloadImageAsync(
            string url, string subject, string taskId, int index, CancellationToken ct)
        {
            try
            {
                var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;

                var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                if (bytes.Length == 0) return null;

                string ext = ".png";
                var ctMedia = response.Content.Headers.ContentType?.MediaType ?? "";
                if (ctMedia.Contains("jpeg") || ctMedia.Contains("jpg")) ext = ".jpg";
                else if (ctMedia.Contains("gif")) ext = ".gif";
                else
                {
                    var urlExt = Path.GetExtension(url.Split('?')[0]);
                    if (!string.IsNullOrEmpty(urlExt)) ext = urlExt;
                }

                string suffix = index > 0 ? $"_{index}" : "";
                string filename = $"{subject}_{taskId}{suffix}{ext}";
                string localPath = Path.Combine(_imagesDir, filename);

                Directory.CreateDirectory(_imagesDir);
                await File.WriteAllBytesAsync(localPath, bytes, ct).ConfigureAwait(false);

                return $"tasks/images/{filename}";
            }
            catch { return null; }
        }
    }
}