using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WatchedAnimeList.Helpers
{
    public static class GitHubVersionHelper
    {
        /// <summary>
        /// Отримує версію з поля `tag_name` або `name` останнього релізу на GitHub.
        /// </summary>
        public static async Task<string?> GetVersionFromReleaseNameAsync(string repoApiUrl)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("WAL-Updater");

                string json = await client.GetStringAsync(repoApiUrl);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("name", out var nameProp))
                    return null;

                string? name = nameProp.GetString();
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                // Парсимо версію: 1.0, 1.0.0, 1.0.0.0
                var match = System.Text.RegularExpressions.Regex.Match(name, @"(\d+(?:\.\d+){1,3})");
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Помилка отримання версії з name: " + ex.Message);
                return null;
            }
        }

    }

}
