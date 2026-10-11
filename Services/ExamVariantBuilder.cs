using System;
using System.Collections.Generic;
using System.Linq;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    /// <summary>
    /// Автоматическое составление экзаменационного варианта из банка ФИПИ.
    /// Каждая позиция КИМ получает одно случайное задание.
    /// </summary>
    public class ExamVariantBuilder
    {
        public static int GetDurationMinutes(string subjectId) => subjectId switch
        {
            "russian"      => 210,
            "math_profile" => 235,
            "informatics"  => 235,
            _              => 180
        };

        public List<TaskItem> BuildExamVariant(string subjectId)
        {
            var config = App.SubjectConfigService.FindById(subjectId);
            var positions = config?.TaskNumbers ?? new List<int>();
            if (positions.Count == 0) return new List<TaskItem>();

            var rng = new Random();
            var result = new List<TaskItem>();

            foreach (var kimPos in positions)
            {
                var pool = App.TaskService.GetPrototypeTasks(subjectId, kimPos).ToList();

                if (pool.Count > 0)
                {
                    result.Add(pool[rng.Next(pool.Count)]);
                }
                else
                {
                    result.Add(new TaskItem
                    {
                        Id = $"exam_stub_{subjectId}_{kimPos}",
                        Subject = subjectId,
                        TaskNumber = kimPos,
                        KimPosition = kimPos,
                        TaskType = "short_answer",
                        Question = $"Задание №{kimPos} пока недоступно: нет JSON в папке tasks/.",
                        ImagePath = null,
                        Options = null,
                        CorrectAnswer = "",
                        MaxPrimaryScore = 1,
                        VideoUrl = null,
                        Prototypes = new List<string> { kimPos.ToString() },
                        FipiId = $"stub_{kimPos}",
                    });
                }
            }

            return result;
        }
    }
}