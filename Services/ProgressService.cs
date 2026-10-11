using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    /// <summary>
    /// Управление прогрессом: предметы, темы, результаты.
    /// Темы берутся из SubjectThemesService (ЕГЭ 2027).
    /// </summary>
    public class ProgressService
    {
        private ProgressData _data = new();

        private static string ProgressPath => Path.Combine(App.BaseDir, "progress.json");

        public IReadOnlyList<Subject> Subjects => _data.Subjects;
        public IReadOnlyList<TaskResult> TaskResults => _data.TaskResults;
        public IReadOnlyList<VariantResult> VariantResults => _data.VariantResults;

        public async Task LoadAsync()
        {
            var loaded = await JsonHelper.ReadAsync<ProgressData>(ProgressPath).ConfigureAwait(false);
            if (loaded == null || loaded.Subjects.Count == 0)
            {
                _data = BuildDefaultProgress();
                await SaveAsync().ConfigureAwait(false);
            }
            else
            {
                _data = loaded;
                SyncThemesFromSpec();
            }
        }

        public async Task SaveAsync()
        {
            await JsonHelper.WriteAsync(ProgressPath, _data).ConfigureAwait(false);
        }

        public Subject? FindSubject(string id) =>
            _data.Subjects.FirstOrDefault(s => s.Id == id);

        public void AddSubject(Subject subject)
        {
            _data.Subjects.Add(subject);
            _ = SaveAsync();
        }

        public void RemoveSubject(string id)
        {
            var s = FindSubject(id);
            if (s != null) { _data.Subjects.Remove(s); _ = SaveAsync(); }
        }

        public void AddTopic(string subjectId, Topic topic)
        {
            var s = FindSubject(subjectId);
            if (s != null) { s.Topics.Add(topic); _ = SaveAsync(); }
        }

        public void RemoveTopic(string subjectId, string topicId)
        {
            var s = FindSubject(subjectId);
            var t = s?.Topics.FirstOrDefault(x => x.Id == topicId);
            if (s != null && t != null) { s.Topics.Remove(t); _ = SaveAsync(); }
        }

        public void RegisterTaskResult(TaskResult result, string? topicId = null)
        {
            _data.TaskResults.Add(result);

            var subject = FindSubject(result.SubjectId);
            if (subject != null)
            {
                subject.TasksSolved++;
                if (result.IsCorrect) subject.TasksCorrect++;
                subject.PrimaryScore += result.PrimaryScore;
                subject.ProgressPercent = subject.TasksSolved == 0
                    ? 0
                    : 100.0 * subject.TasksCorrect / subject.TasksSolved;

                if (!string.IsNullOrEmpty(topicId))
                {
                    var topic = subject.Topics.FirstOrDefault(t => t.Id == topicId);
                    if (topic != null)
                    {
                        topic.TasksSolved++;
                        if (result.IsCorrect) topic.TasksCorrect++;
                        topic.ProgressPercent = topic.TasksSolved == 0
                            ? 0
                            : 100.0 * topic.TasksCorrect / topic.TasksSolved;
                    }
                }

                if (subject.IsEge && !string.IsNullOrEmpty(subject.ScoreScaleKey))
                {
                    var sec = App.ScoreService.ConvertToSecondary(subject.ScoreScaleKey!, subject.PrimaryScore);
                    if (sec.HasValue) subject.SecondaryScore = sec.Value;
                }
            }

            _ = SaveAsync();
        }

        public void RegisterVariantResult(VariantResult vr)
        {
            _data.VariantResults.Add(vr);

            var subject = FindSubject(vr.SubjectId);
            if (subject != null)
            {
                subject.PrimaryScore = vr.PrimaryScore;
                if (subject.IsEge && !string.IsNullOrEmpty(subject.ScoreScaleKey))
                {
                    var sec = App.ScoreService.ConvertToSecondary(subject.ScoreScaleKey!, vr.PrimaryScore);
                    if (sec.HasValue) subject.SecondaryScore = sec.Value;
                }
                subject.ProgressPercent = vr.PercentCorrect;
            }

            _ = SaveAsync();
        }

        private ProgressData BuildDefaultProgress()
        {
            var data = new ProgressData();

            foreach (var cfg in App.SubjectConfigService.Subjects)
            {
                var subj = new Subject
                {
                    Id = cfg.Id,
                    Name = cfg.Name,
                    IsEge = cfg.IsEge,
                    ScoreScaleKey = cfg.ScoreScaleKey,
                };

                var themes = App.SubjectThemesService.GetThemes(cfg.Id);
                foreach (var t in themes)
                {
                    subj.Topics.Add(new Topic { Id = t.Id, Name = t.Name });
                }

                data.Subjects.Add(subj);
            }

            return data;
        }

        private void SyncThemesFromSpec()
        {
            foreach (var subj in _data.Subjects)
            {
                var spec = App.SubjectThemesService.GetThemes(subj.Id);
                if (spec.Count == 0) continue;

                var existing = subj.Topics.ToDictionary(t => t.Id, t => t);

                subj.Topics.Clear();
                foreach (var s in spec)
                {
                    if (existing.TryGetValue(s.Id, out var old))
                    {
                        old.Name = s.Name;
                        subj.Topics.Add(old);
                    }
                    else
                    {
                        subj.Topics.Add(new Topic { Id = s.Id, Name = s.Name });
                    }
                }
            }

            _ = SaveAsync();
        }
    }
}