using System;
using System.Diagnostics;

namespace EgeTracker.Services
{
    /// <summary>
    /// Запуск внешних инструментов для ЕГЭ по информатике.
    /// </summary>
    public static class ExternalToolsService
    {
        public static bool LaunchVsCode()   => TryStart("code", null);
        public static bool LaunchExcel()    => TryStart("excel", null);
        public static bool LaunchNotepad()  => TryStart("notepad.exe", null);
        public static bool LaunchCalculator() => TryStart("calc.exe", null);
        public static bool LaunchPaint()    => TryStart("mspaint.exe", null);

        private static bool TryStart(string fileName, string? args)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args ?? string.Empty,
                    UseShellExecute = true,
                });
                return true;
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                return false;
            }
        }
    }
}