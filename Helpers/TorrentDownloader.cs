using System.Threading.Tasks;
using MonoTorrent.Client;
using WatchedAnimeList.Controls;
using System.IO;
using System.Text.Json;
using WatchedAnimeList.Models;
using Microsoft.Win32.TaskScheduler.Fluent;

namespace WatchedAnimeList.Helpers
{
    public static class TorrentDownloader
    {
        private static ClientEngine engine = new(
            new EngineSettingsBuilder
            {
                CacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache")
            }.ToSettings()
        );

        private static Dictionary<string, TorrentDownloadJob> jobs = new();

        public static async Task StartDownloadAsync(string torrentFilePath, string saveFolder, WatchAnimePage watchPage,
            Action<string> logAction,
            Action onFinished,
            Action onUpdate)
        {

            if (jobs.ContainsKey(saveFolder))
            {
                logAction?.Invoke($"⚠️ Завантаження вже йде для: {saveFolder}");
                return;
            }

            var job = new TorrentDownloadJob(engine)
            {
                LogAction = logAction
            };

            job.OnDownloadFinished += onFinished;
            job.OnEpisodeCountUpdated += onUpdate;

            await job.AddDownloadAsync(torrentFilePath, saveFolder);
            var availableEpisodes = job.GetAvailableEpisodes();

            foreach (var ep in availableEpisodes)
                watchPage.AddEpisodeToggle(ep);

            string configPath = Path.Combine(saveFolder, "downloadConfig.json");
            if (File.Exists(configPath))
            {
                var config = JsonSerializer.Deserialize<DownloadConfig>(File.ReadAllText(configPath));
                if (config?.SelectedEpisodes == null)
                {
                    Debug.Log("download config is null", NotificationType.Error);
                }
                else
                {
                    logAction?.Invoke($"🔽 Продовжую качати: {string.Join(", ", config.SelectedEpisodes)}");
                    await job.StartDownloadAsync(config.SelectedEpisodes);
                }
            }

            else
            {
                watchPage.ShowSelectEpisodes();
                await watchPage.WaitForUserClickAsync();
                watchPage.HideSelectEpisodes();

                var episodesToDownload = watchPage.GetEpisodesToDownload();
                var json = JsonSerializer.Serialize(new DownloadConfig() { SelectedEpisodes = episodesToDownload.ToList() }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(saveFolder, "downloadConfig.json"), json);


                jobs[saveFolder] = job;
                await job.StartDownloadAsync(episodesToDownload);
            }
        }

        public static async Task ContinueDownloadFeedback(string saveFolder, WatchAnimePage watchPage,
            Action<string> logAction, Action onFinished, Action onUpdate)
        {
            if (!jobs.TryGetValue(saveFolder, out var job))
            {
                logAction?.Invoke($"❌ Нема активного завантаження для: {saveFolder}");
                return;
            }

            job.LogAction = logAction;

            // Перепідключаємо евенти
            job.OnDownloadFinished += onFinished;
            job.OnEpisodeCountUpdated += onUpdate;

            logAction?.Invoke($"🔄 Відновлено підключення до завантаження: {job.manager.Torrent.Name}");

            // Показати вже активні епізоди
            var episodes = job.GetAvailableEpisodes();
            foreach (var ep in episodes)
                watchPage.AddEpisodeToggle(ep);

            string configPath = Path.Combine(saveFolder, "downloadConfig.json");
            if (File.Exists(configPath))
            {
                var config = JsonSerializer.Deserialize<DownloadConfig>(File.ReadAllText(configPath));
                if (config == null || config.SelectedEpisodes == null)
                {
                    Debug.Log("download config is null", NotificationType.Error);
                }
                else
                {
                    logAction?.Invoke($"🔽 Продовжую качати: {string.Join(", ", config.SelectedEpisodes)}");
                    await job.StartDownloadAsync(config.SelectedEpisodes);
                }
            }
            else
            {
                watchPage.ShowSelectEpisodes();
                await watchPage.WaitForUserClickAsync();
                watchPage.HideSelectEpisodes();

                var needEpisodesToDownload = watchPage.GetEpisodesToDownload();
                logAction?.Invoke($"🔽 Продовжую качати: {string.Join(", ", needEpisodesToDownload)}");

                // Зміна пріоритетів на вже активному менеджері
                await job.StartDownloadAsync(needEpisodesToDownload);
            }
        }


        public static void RemoveManager(string path)
        {
            if (jobs.ContainsKey(path))
            {
                jobs[path].StopDownloadAsync();
            }
            else
            {
                Debug.Log($"Незнайдено TorrentDownloadJob для {path}");
            }
        }

        public static bool IsDownloading(string path) => jobs.ContainsKey(path);

        public static async Task StopAllAsync()
        {
            foreach (var job in jobs.Values)
                await job.StopDownloadAsync();

            jobs.Clear();
            engine.Dispose();
        }
    }

}
