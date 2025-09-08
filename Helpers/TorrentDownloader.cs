using Microsoft.Win32.TaskScheduler.Fluent;
using MonoTorrent;
using MonoTorrent.Client;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using WatchedAnimeList.Controls;
using WatchedAnimeList.Models;

namespace WatchedAnimeList.Helpers
{
    public static class TorrentDownloader
    {
        private static ClientEngine engine = new(
            new EngineSettingsBuilder
            {
                CacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache"),
                MaximumConnections = 5000,
                AllowPortForwarding = true,
                AutoSaveLoadDhtCache = true,
                MaximumDownloadRate = 0,
                MaximumUploadRate = 0,
            }.ToSettings()
        );

        private static Dictionary<string, TorrentDownloadJob> jobs = new();

        public static async Task StartDownloadAsync(
            MagnetLink? magnetLink,
            string saveFolder,
            WatchAnimePage watchPage,
            Action<string> logAction,
            Action onFinished,
            Action onUpdate)
        {
            if (jobs.ContainsKey(saveFolder))
            {
                logAction?.Invoke($"Download is already in progress for: {saveFolder}");
                return;
            }

            var job = new TorrentDownloadJob(engine)
            {
                LogAction = logAction
            };
            jobs[saveFolder] = job;

            job.OnDownloadFinished += onFinished;
            job.OnEpisodeCountUpdated += onUpdate;

            string configPath = Path.Combine(saveFolder, "downloadConfig.json");

            // If magnet link is not provided, try to get it from config
            if (magnetLink == null && File.Exists(configPath))
            {
                var configJson = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<DownloadConfig>(configJson);
                if (config != null && config.MagnetLinkHashes != null)
                {
                    magnetLink = MagnetLink.Parse(config.MagnetLinkHashes);
                }
            }

            // If no magnet link is available, stop
            if (magnetLink == null)
            {
                logAction?.Invoke("Magnet link not provided and not found in config.");
                return;
            }

            await job.AddDownloadAsync(magnetLink, saveFolder);

            var availableEpisodes = job.GetAvailableEpisodes();
            foreach (var ep in availableEpisodes)
                watchPage.AddEpisodeToggle(ep);

            List<int> episodesToDownload;

            if (File.Exists(configPath))
            {
                var config = JsonSerializer.Deserialize<DownloadConfig>(File.ReadAllText(configPath));
                episodesToDownload = config?.SelectedEpisodes?.ToList() ?? new List<int>();
                if (!episodesToDownload.Any())
                    Debug.Log("Download config is empty", NotificationType.Error);
                else
                    logAction?.Invoke($"Continuing download: {string.Join(", ", episodesToDownload)}");
            }
            else
            {
                // Show UI to select episodes if config does not exist
                watchPage.ShowSelectEpisodes();
                await watchPage.WaitForUserClickAsync();
                watchPage.HideSelectEpisodes();

                episodesToDownload = watchPage.GetEpisodesToDownload().ToList();


                string magnetUri = $"magnet:?xt=urn:btih:{magnetLink.InfoHashes.V1OrV2.ToHex()}&dn={Uri.EscapeDataString(magnetLink.Name!)}";
                // Save selected episodes and magnet link to config
                var json = JsonSerializer.Serialize(new DownloadConfig()
                {
                    SelectedEpisodes = episodesToDownload,
                    MagnetLinkHashes = magnetUri
                }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }

            if (episodesToDownload.Any())
                await job.StartDownloadAsync(episodesToDownload);
        }


        public static async Task DownloadFeedback(string saveFolder, WatchAnimePage watchPage,
            Action<string> logAction, Action onFinished, Action onUpdate)
        {
            if (!jobs.TryGetValue(saveFolder, out var job))
            {
                logAction?.Invoke($"❌ Нема активного завантаження для: {saveFolder}");
                return;
            }
            if (job.manager is null || job.manager.Torrent is null)
                Debug.Ex("job.manager.Torrent is null");

            Debug.Log($"Відновлення підключення до завантаження: {job.manager.Torrent.Name}", NotificationType.Info);
            logAction?.Invoke($"🔄 Відновлення підключення до завантаження: {job.manager.Torrent.Name}");

            // Показати вже активні епізоди
            var episodes = job.GetAvailableEpisodes();
            foreach (var ep in episodes)
                watchPage.AddEpisodeToggle(ep);

            job.LogAction += logAction;
            job.OnDownloadFinished += onFinished;
            job.OnEpisodeCountUpdated += onUpdate;

            job.FeedBackConnect();
            await Task.Delay(1);
        }
        public static void RemoveManager(string path)
        {
            if (jobs.ContainsKey(path))
            {
                _ = jobs[path].StopDownloadAsync();
            }
            else
            {
                Debug.Log($"Незнайдено TorrentDownloadJob для {path}", NotificationType.Error);
            }
        }
        public static bool IsDownloading(string path)
        {
            return jobs.ContainsKey(path);
        }
        public static async Task StopAllAsync()
        {
            foreach (var job in jobs.Values)
                await job.StopDownloadAsync();

            jobs.Clear();
            engine.Dispose();
        }
    }

}
