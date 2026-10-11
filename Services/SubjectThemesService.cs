using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EgeTracker.Models;

namespace EgeTracker.Services
{
    /// <summary>
    /// Темы ЕГЭ 2027 по спецификации ФИПИ.
    /// Сохраняет в tasks/themes.json, при отсутствии создаёт дефолт.
    /// </summary>
    public class SubjectThemesService
    {
        private static string ThemesPath =>
            Path.Combine(App.BaseDir, "tasks", "themes.json");

        public Dictionary<string, SubjectThemeSpec> Themes { get; private set; } = new();

        public async Task LoadAsync()
        {
            try
            {
                if (File.Exists(ThemesPath))
                {
                    var list = await JsonHelper
                        .ReadAsync<List<SubjectThemeSpec>>(ThemesPath)
                        .ConfigureAwait(false);

                    if (list != null && list.Count > 0)
                    {
                        Themes = list.ToDictionary(t => t.SubjectId, t => t);
                        return;
                    }
                }

                Themes = BuildDefaults();
                await SaveAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                Themes = BuildDefaults();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var dir = Path.GetDirectoryName(ThemesPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                await JsonHelper
                    .WriteAsync(ThemesPath, Themes.Values.ToList())
                    .ConfigureAwait(false);
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        public List<ThemeSpec> GetThemes(string subjectId) =>
            Themes.TryGetValue(subjectId, out var spec) ? spec.Themes : new List<ThemeSpec>();

        private static Dictionary<string, SubjectThemeSpec> BuildDefaults() => new()
        {
            ["math_profile"] = new SubjectThemeSpec
            {
                SubjectId = "math_profile",
                Themes = new List<ThemeSpec>
                {
                    new() { Id = "m1",  Name = "Вычисления и преобразования",                          KimPosition = 1 },
                    new() { Id = "m2",  Name = "Уравнения и неравенства",                              KimPosition = 2 },
                    new() { Id = "m3",  Name = "Планиметрия",                                          KimPosition = 3 },
                    new() { Id = "m4",  Name = "Вероятность и статистика",                             KimPosition = 4 },
                    new() { Id = "m5",  Name = "Производная и её применение",                          KimPosition = 5 },
                    new() { Id = "m6",  Name = "Теория вероятностей (дисперсия, мат. ожидание)",       KimPosition = 6 },
                    new() { Id = "m7",  Name = "Стереометрия",                                         KimPosition = 7 },
                    new() { Id = "m8",  Name = "Графики функций",                                      KimPosition = 8 },
                    new() { Id = "m9",  Name = "Преобразования выражений",                             KimPosition = 9 },
                    new() { Id = "m10", Name = "Прикладные задачи",                                    KimPosition = 10 },
                    new() { Id = "m11", Name = "Текстовые задачи",                                     KimPosition = 11 },
                    new() { Id = "m12", Name = "Исследование функций",                                 KimPosition = 12 },
                    new() { Id = "m13", Name = "Текстовые задачи (личные финансы)",                    KimPosition = 13 },
                    new() { Id = "m14", Name = "Уравнения и неравенства с параметром",                 KimPosition = 14 },
                    new() { Id = "m15", Name = "Планиметрия (сложное)",                                KimPosition = 15 },
                    new() { Id = "m16", Name = "Экономические задачи",                                 KimPosition = 16 },
                    new() { Id = "m17", Name = "Моделирование реальных ситуаций",                      KimPosition = 17 },
                    new() { Id = "m18", Name = "Задачи с параметром (сложное)",                        KimPosition = 18 },
                    new() { Id = "m19", Name = "Числа и их свойства",                                  KimPosition = 19 },
                    new() { Id = "m20", Name = "Олимпиадные задачи",                                   KimPosition = 20 }
                }
            },
            ["russian"] = new SubjectThemeSpec
            {
                SubjectId = "russian",
                Themes = new List<ThemeSpec>
                {
                    new() { Id = "r1",  Name = "Анализ текста",                                        KimPosition = 1 },
                    new() { Id = "r2",  Name = "Средства связи предложений",                           KimPosition = 2 },
                    new() { Id = "r3",  Name = "Лексическое значение слова",                           KimPosition = 3 },
                    new() { Id = "r4",  Name = "Орфоэпические нормы",                                  KimPosition = 4 },
                    new() { Id = "r5",  Name = "Лексические нормы",                                    KimPosition = 5 },
                    new() { Id = "r6",  Name = "Морфологические нормы",                                KimPosition = 6 },
                    new() { Id = "r7",  Name = "Синтаксические нормы",                                 KimPosition = 7 },
                    new() { Id = "r8",  Name = "Орфография: корни",                                    KimPosition = 8 },
                    new() { Id = "r9",  Name = "Орфография: приставки",                                KimPosition = 9 },
                    new() { Id = "r10", Name = "Орфография: суффиксы",                                 KimPosition = 10 },
                    new() { Id = "r11", Name = "Орфография: Н/НН",                                     KimPosition = 11 },
                    new() { Id = "r12", Name = "Правописание НЕ/НИ",                                   KimPosition = 12 },
                    new() { Id = "r13", Name = "Слитно/раздельно/дефис",                               KimPosition = 13 },
                    new() { Id = "r14", Name = "Пунктуация: простые предложения",                      KimPosition = 14 },
                    new() { Id = "r15", Name = "Пунктуация: тире/двоеточие",                           KimPosition = 15 },
                    new() { Id = "r16", Name = "Пунктуация: однородные члены",                         KimPosition = 16 },
                    new() { Id = "r17", Name = "Пунктуация: обособленные члены",                       KimPosition = 17 },
                    new() { Id = "r18", Name = "Пунктуация: вводные слова",                            KimPosition = 18 },
                    new() { Id = "r19", Name = "Пунктуация: сложные предложения",                      KimPosition = 19 },
                    new() { Id = "r20", Name = "Пунктуация: СПП",                                      KimPosition = 20 },
                    new() { Id = "r21", Name = "Пунктуация: тире",                                     KimPosition = 21 },
                    new() { Id = "r22", Name = "Текст: связь предложений",                             KimPosition = 22 },
                    new() { Id = "r23", Name = "Типы речи",                                            KimPosition = 23 },
                    new() { Id = "r24", Name = "Лексика: фразеологизмы",                               KimPosition = 24 },
                    new() { Id = "r25", Name = "Средства выразительности",                             KimPosition = 25 },
                    new() { Id = "r26", Name = "Анализ текста (развёрнутый)",                          KimPosition = 26 },
                    new() { Id = "r27", Name = "Сочинение",                                            KimPosition = 27 }
                }
            },
            ["informatics"] = new SubjectThemeSpec
            {
                SubjectId = "informatics",
                Themes = new List<ThemeSpec>
                {
                    new() { Id = "i1",  Name = "Системы счисления",                                    KimPosition = 1 },
                    new() { Id = "i2",  Name = "Таблицы истинности",                                   KimPosition = 2 },
                    new() { Id = "i3",  Name = "Информационные модели",                                KimPosition = 3 },
                    new() { Id = "i4",  Name = "Кодирование и декодирование",                          KimPosition = 4 },
                    new() { Id = "i5",  Name = "Алгоритмы: анализ и исполнение",                       KimPosition = 5 },
                    new() { Id = "i6",  Name = "Циклы и условия",                                      KimPosition = 6 },
                    new() { Id = "i7",  Name = "Электронные таблицы",                                  KimPosition = 7 },
                    new() { Id = "i8",  Name = "Комбинаторика",                                        KimPosition = 8 },
                    new() { Id = "i9",  Name = "Графы и пути",                                         KimPosition = 9 },
                    new() { Id = "i10", Name = "IP-адресация и подсети",                               KimPosition = 10 },
                    new() { Id = "i11", Name = "Хеширование и поиск",                                  KimPosition = 11 },
                    new() { Id = "i12", Name = "Рекурсия и динамика",                                  KimPosition = 12 },
                    new() { Id = "i13", Name = "Анализ алгоритмов",                                    KimPosition = 13 },
                    new() { Id = "i14", Name = "Системы счисления (позиционные)",                      KimPosition = 14 },
                    new() { Id = "i15", Name = "Алгоритмы с несколькими вариантами",                   KimPosition = 15 },
                    new() { Id = "i16", Name = "Рекурсивные алгоритмы",                                KimPosition = 16 },
                    new() { Id = "i17", Name = "Обработка числовых последовательностей",               KimPosition = 17 },
                    new() { Id = "i18", Name = "Динамическое программирование",                        KimPosition = 18 },
                    new() { Id = "i19", Name = "Теория игр",                                           KimPosition = 19 },
                    new() { Id = "i20", Name = "Теория игр (2)",                                       KimPosition = 20 },
                    new() { Id = "i21", Name = "Теория игр (3)",                                       KimPosition = 21 },
                    new() { Id = "i22", Name = "Многопоточные алгоритмы",                              KimPosition = 22 },
                    new() { Id = "i23", Name = "Оптимизация алгоритмов",                               KimPosition = 23 },
                    new() { Id = "i24", Name = "Обработка символьных строк",                           KimPosition = 24 },
                    new() { Id = "i25", Name = "Делители и простые числа",                             KimPosition = 25 },
                    new() { Id = "i26", Name = "Сортировка и поиск в массивах",                        KimPosition = 26 },
                    new() { Id = "i27", Name = "Программирование (сложное)",                           KimPosition = 27 }
                }
            }
        };
    }
}