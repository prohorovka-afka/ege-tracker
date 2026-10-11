using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    /// <summary>
    /// Перевод первичных баллов в 100-балльные. Загружает tasks/score_scale.json,
    /// при отсутствии — использует встроенную шкалу.
    /// </summary>
    public class ScoreService
    {
        private ScoreScale _scale = new ScoreScale();

        private static string ScalePath =>
            Path.Combine(App.BaseDir, "tasks", "score_scale.json");

        public async Task LoadAsync()
        {
            var loaded = await JsonHelper.ReadAsync<ScoreScale>(ScalePath).ConfigureAwait(false);
            if (loaded == null || (loaded.MathProfile == null && loaded.Russian == null && loaded.Informatics == null))
            {
                _scale = BuildDefaultScale();
                // Пробуем записать дефолтную шкалу рядом — на случай первого запуска.
                await JsonHelper.WriteAsync(ScalePath, _scale).ConfigureAwait(false);
            }
            else
            {
                _scale = loaded;
            }
        }

        /// <summary>
        /// Перевести первичный балл в 100-балльный. Возвращает null, если шкала для предмета отсутствует.
        /// </summary>
        public int? ConvertToSecondary(string subjectId, int primary)
        {
            var table = GetTable(subjectId);
            if (table == null || table.Count == 0) return null;

            // Ищем точное совпадение, иначе — ближайшее меньшее/равное.
            var exact = table.FirstOrDefault(e => e.Primary == primary);
            if (exact != null) return exact.Secondary;

            var nearest = table.Where(e => e.Primary <= primary)
                               .OrderByDescending(e => e.Primary)
                               .FirstOrDefault();
            return nearest?.Secondary ?? 0;
        }

        private List<ScoreScaleEntry>? GetTable(string subjectId)
        {
            return subjectId switch
            {
                "math_profile" => _scale.MathProfile,
                "russian" => _scale.Russian,
                "informatics" => _scale.Informatics,
                _ => null
            };
        }

        private static ScoreScale BuildDefaultScale()
        {
            return new ScoreScale
            {
                MathProfile = BuildLinear(31, 0, 100),
                Russian = BuildLinear(50, 0, 100),
                Informatics = BuildLinear(29, 0, 100)
            };
        }

        /// <summary>
        /// Простейшая линейная шкала: 0 → 0, max → 100 (для встроенного дефолта).
        /// </summary>
        private static List<ScoreScaleEntry> BuildLinear(int maxPrimary, int minSecondary, int maxSecondary)
        {
            var list = new List<ScoreScaleEntry>();
            for (int i = 0; i <= maxPrimary; i++)
            {
                double t = maxPrimary == 0 ? 1.0 : (double)i / maxPrimary;
                int sec = (int)System.Math.Round(minSecondary + t * (maxSecondary - minSecondary));
                list.Add(new ScoreScaleEntry { Primary = i, Secondary = sec });
            }
            return list;
        }
    }
}