using System;
using System.Collections.Generic;
using System.Linq;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    /// <summary>
    /// Генерирует варианты ЕГЭ 2027 в правильном порядке КИМ.
    /// Для каждого предмета — 100 вариантов.
    /// </summary>
    public class VariantGeneratorService
    {
        public const int VariantsPerSubject = 100;

        public List<VariantTemplate> GenerateVariants(string subjectId)
        {
            var positions = GetKimPositions(subjectId);
            if (positions.Count == 0) return new List<VariantTemplate>();

            var result = new List<VariantTemplate>();
            for (int i = 1; i <= VariantsPerSubject; i++)
            {
                result.Add(new VariantTemplate
                {
                    VariantNumber = i,
                    SubjectId = subjectId,
                    KimPositions = new List<int>(positions)
                });
            }
            return result;
        }

        /// <summary>
        /// Позиции КИМ в правильном порядке для предмета (ЕГЭ 2027).
        /// </summary>
        private List<int> GetKimPositions(string subjectId)
        {
            return subjectId switch
            {
                // Русский язык — 27 заданий (без изменений 2027)
                "russian" => Enumerable.Range(1, 27).ToList(),

                // Информатика — 27 заданий (структура сохранена)
                "informatics" => Enumerable.Range(1, 27).ToList(),

                // Математика профиль — 20 заданий (НОВОЕ в 2027: было 19)
                // Добавлены №6 (вероятность), №13 (финансы), №17 (моделирование)
                "math_profile" => Enumerable.Range(1, 20).ToList(),

                // Практические курсы
                "linux" => Enumerable.Range(1, 18).ToList(),
                "english" => Enumerable.Range(1, 18).ToList(),

                _ => new List<int>()
            };
        }

        /// <summary>
        /// Собрать задания варианта: для каждой позиции КИМ — случайное из банка.
        /// Если данных нет — заглушка.
        /// </summary>
        public List<TaskItem> BuildVariantTasks(string subjectId, VariantTemplate template)
        {
            var rng = new Random(template.VariantNumber);
            var result = new List<TaskItem>();

            foreach (var kimPos in template.KimPositions)
            {
                var pool = App.TaskService
                    .GetPrototypeTasks(subjectId, kimPos)
                    .ToList();

                if (pool.Count > 0)
                {
                    result.Add(pool[rng.Next(pool.Count)]);
                }
                else
                {
                    result.Add(new TaskItem
                    {
                        Id = $"stub_{subjectId}_{kimPos}_{template.VariantNumber}",
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
                        FipiId = $"stub_{kimPos}"
                    });
                }
            }

            return result;
        }
    }
}