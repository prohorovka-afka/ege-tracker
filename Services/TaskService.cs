using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    /// <summary>
    /// Загрузка заданий из папки tasks/ рядом с .exe.
    /// </summary>
    public class TaskService
    {
        private readonly List<TaskItem> _all = new List<TaskItem>();

        public IReadOnlyList<TaskItem> All => _all;

        private static string TasksDir => Path.Combine(App.BaseDir, "tasks");
        private static string ImagesDir => Path.Combine(TasksDir, "images");

        public async Task LoadAsync()
        {
            _all.Clear();
            try
            {
                if (!Directory.Exists(TasksDir)) Directory.CreateDirectory(TasksDir);
                if (!Directory.Exists(ImagesDir)) Directory.CreateDirectory(ImagesDir);

                var files = Directory.GetFiles(TasksDir, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var f in files)
                {
                    // Пропускаем score_scale.json — это не задания.
                    if (string.Equals(Path.GetFileName(f), "score_scale.json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var list = await JsonHelper.ReadAsync<List<TaskItem>>(f).ConfigureAwait(false);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            if (item != null && !string.IsNullOrEmpty(item.Id))
                                _all.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.LogError(ex);
            }
        }

        public IEnumerable<TaskItem> GetBySubject(string subjectId) =>
            _all.Where(t => string.Equals(t.Subject, subjectId, StringComparison.OrdinalIgnoreCase));

        public IEnumerable<int> GetTaskNumbers(string subjectId) =>
            GetBySubject(subjectId).Select(t => t.TaskNumber).Distinct().OrderBy(n => n);

        /// <summary>
        /// Взять случайный прототип задачи с данным номером (или все).
        /// </summary>
        public List<TaskItem> GetPrototypeTasks(string subjectId, int taskNumber) =>
            GetBySubject(subjectId).Where(t => t.TaskNumber == taskNumber).ToList();

        public TaskItem? GetRandomByPrototype(string subjectId, int taskNumber, Random rng)
        {
            var list = GetPrototypeTasks(subjectId, taskNumber);
            if (list.Count == 0) return null;
            return list[rng.Next(list.Count)];
        }

        /// <summary>
        /// Получить 15 аналогов (или сколько есть) для отработки ошибки.
        /// </summary>
        public List<TaskItem> GetSimilarTasks(string subjectId, int taskNumber, int max = 15)
        {
            var list = GetPrototypeTasks(subjectId, taskNumber);
            var rng = new Random();
            return list.OrderBy(_ => rng.Next()).Take(max).ToList();
        }

        /// <summary>
        /// Сформировать вариант для предмета: по одному заданию на каждую позицию КИМ.
        /// </summary>
        public List<TaskItem> BuildVariant(string subjectId)
        {
            var rng = new Random();
            var result = new List<TaskItem>();
            var positions = GetBySubject(subjectId)
                .Select(t => t.KimPosition)
                .Distinct()
                .OrderBy(p => p);

            foreach (var pos in positions)
            {
                var pool = GetBySubject(subjectId).Where(t => t.KimPosition == pos).ToList();
                if (pool.Count > 0)
                {
                    result.Add(pool[rng.Next(pool.Count)]);
                }
            }
            return result;
        }
    }
}