using System;
using System.Collections.Generic;
using System.Linq;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    /// <summary>
    /// Проверка второй части ЕГЭ:
    /// - Математика/физика: показ эталонного ответа + ручное выставление баллов.
    /// - Русский: подсчёт ключевых слов (черновой вариант проверки сочинения).
    /// - Информатика: несколько правильных вариантов ответа.
    /// </summary>
    public class SecondPartChecker
    {
        /// <summary>
        /// Сравнение ответа пользователя с эталоном. Возвращает true, если ответ совпал.
        /// Для extended_answer — сравнение по ключевым словам.
        /// </summary>
        public bool CheckExtended(TaskItem task, string userAnswer, out double matchPercent)
        {
            matchPercent = 0;
            if (string.IsNullOrWhiteSpace(userAnswer)) return false;

            // Для заданий с answer_criteria — считаем по ключевым словам
            if (task.AnswerCriteria != null && task.AnswerCriteria.Count > 0)
            {
                var normalized = Normalize(userAnswer);
                int matched = task.AnswerCriteria
                    .Count(k => normalized.Contains(Normalize(k)));

                matchPercent = 100.0 * matched / task.AnswerCriteria.Count;
                return matched >= Math.Max(1, task.AnswerCriteria.Count / 2);
            }

            // Иначе — прямое сравнение
            bool ok = Normalize(userAnswer) == Normalize(task.CorrectAnswer);
            matchPercent = ok ? 100 : 0;
            return ok;
        }

        /// <summary>
        /// Проверка информатики — поддержка нескольких правильных вариантов.
        /// Формат: варианты через "|" в correct_answer.
        /// Пример: "5|6|7" — верны 5, 6 или 7.
        /// </summary>
        public bool CheckMultipleVariants(string correctAnswer, string userAnswer)
        {
            if (string.IsNullOrWhiteSpace(correctAnswer) || string.IsNullOrWhiteSpace(userAnswer))
                return false;

            var variants = correctAnswer
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(Normalize)
                .ToList();

            var user = Normalize(userAnswer);
            return variants.Contains(user);
        }

        /// <summary>
        /// Оценка сочинения русского языка по критериям (черновой вариант).
        /// Возвращает 0..maxPoints по каждому критерию.
        /// </summary>
        public Dictionary<string, int> GradeEssay(string userAnswer, List<CriteriaItem> criteria)
        {
            var result = new Dictionary<string, int>();
            if (string.IsNullOrWhiteSpace(userAnswer) || criteria == null)
                return result;

            var normalized = Normalize(userAnswer);
            int length = userAnswer.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            foreach (var c in criteria)
            {
                int points = 0;

                // Базовая проверка по ключевым словам
                if (c.Keywords.Count > 0)
                {
                    int matched = c.Keywords.Count(k => normalized.Contains(Normalize(k)));
                    double ratio = (double)matched / c.Keywords.Count;
                    points = (int)Math.Round(c.MaxPoints * ratio);
                }
                else
                {
                    // Без ключевых слов — бонус за объём
                    points = length >= 150 ? c.MaxPoints : length >= 70 ? c.MaxPoints / 2 : 0;
                }

                result[c.Name] = Math.Min(points, c.MaxPoints);
            }

            return result;
        }

        private static string Normalize(string s) =>
            (s ?? string.Empty)
                .Trim()
                .Replace(" ", "")
                .Replace(",", ".")
                .Replace("\u00A0", "")
                .Replace("ё", "е")
                .ToLowerInvariant();
    }
}