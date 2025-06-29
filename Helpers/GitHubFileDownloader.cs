using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WatchedAnimeList.Helpers
{
    public static class GitHubFileDownloader
    {
        public static async Task<bool> DownloadFileFromLatestReleaseAsync(
            string repoApiUrl,
            string fileNameToDownload,
            string localSavePath,
            string userAgent = "WAL-Updater")
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

                string json = await client.GetStringAsync(repoApiUrl);
                using var doc = JsonDocument.Parse(json);

                var assets = doc.RootElement.GetProperty("assets");
                foreach (var asset in assets.EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString();
                    string downloadUrl = asset.GetProperty("browser_download_url").GetString();

                    if (name.Equals(fileNameToDownload, StringComparison.OrdinalIgnoreCase))
                    {
                        var data = await client.GetByteArrayAsync(downloadUrl);
                        await File.WriteAllBytesAsync(localSavePath, data);
                        return true;
                    }
                }

                Console.WriteLine($"Файл '{fileNameToDownload}' не знайдено в останньому релізі.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при завантаженні: {ex.Message}");
                return false;
            }
        }
    }

}
