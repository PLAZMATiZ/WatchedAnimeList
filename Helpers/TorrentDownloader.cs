using System;
using System.IO;
using System.Threading.Tasks;
using MonoTorrent;
using MonoTorrent.Client;

namespace WatchedAnimeList.Helpers
{
    public class TorrentDownloader
    {
        private ClientEngine engine;
        private TorrentManager manager;
        public event Action<int> OnEpisodeCountUpdated;
        public event Action OnDownloadFinished;
        public Action<string> LogAction;
        public TorrentDownloader()
        {
            engine = new ClientEngine(new EngineSettings());
        }

        public async Task StartDownloadAsync(string torrentFilePath, string saveFolder)
        {
            if (!File.Exists(torrentFilePath))
                throw new FileNotFoundException("Торрент не знайдено", torrentFilePath);

            Directory.CreateDirectory(saveFolder);

            manager = await engine.AddAsync(torrentFilePath, saveFolder);

            await manager.StartAsync();

            while (!manager.Complete)
            {
                LogStatus();

                // Підрахунок серій по ходу
                int currentCount = Directory.GetFiles(saveFolder, "*", SearchOption.AllDirectories)
                    .Count(f => f.EndsWith(".mkv") || f.EndsWith(".mp4"));

                OnEpisodeCountUpdated?.Invoke(currentCount);

                await Task.Delay(2000);
            }

            LogStatus();

            // Останнє оновлення
            int finalCount = Directory.GetFiles(saveFolder, "*", SearchOption.AllDirectories)
                .Count(f => f.EndsWith(".mkv") || f.EndsWith(".mp4"));
            OnEpisodeCountUpdated?.Invoke(finalCount);

            OnDownloadFinished?.Invoke();
            LogAction?.Invoke("✅ Завантаження завершено");
            var mainPage = MainWindow.Global.mainPage;
            if(mainPage != null)
                MainWindow.Global.mainPage.DownloadedTitlesLoad();
        }


        private void LogStatus()
        {
            long totalSize = manager.Torrent.Size;
            long downloaded = 0;
            foreach (var file in manager.Files)
                downloaded += file.BytesDownloaded();

            long bytesLeft = totalSize - downloaded;
            double progress = manager.Progress;
            double speed = manager.Monitor.DownloadSpeed / 1024.0;

            LogAction?.Invoke($"⬇️ Швидкість: {speed:F2} KB/s | ✅ Завантажено: {downloaded / (1024.0 * 1024.0):F2} MB | ⏳ Залишилось: {bytesLeft / (1024.0 * 1024.0):F2} MB | 📊 Прогрес: {progress:F2}%");
        }
    }
}