using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace EgeTracker.Services
{
    /// <summary>
    /// Централизованные настройки сериализации JSON.
    /// </summary>
    public static class JsonHelper
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static async Task<T?> ReadAsync<T>(string path)
        {
            if (!File.Exists(path)) return default;
            try
            {
                using var stream = File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync<T>(stream, Options).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                return default;
            }
        }

        public static async Task<bool> WriteAsync<T>(string path, T value)
        {
            try
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Атомарная запись: сначала во временный файл, потом перемещение.
                string tmp = path + ".tmp";
                using (var stream = File.Create(tmp))
                {
                    await JsonSerializer.SerializeAsync(stream, value, Options).ConfigureAwait(false);
                }
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
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
