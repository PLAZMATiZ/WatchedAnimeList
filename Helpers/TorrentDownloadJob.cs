using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.PiecePicking;

namespace WatchedAnimeList.Helpers
{
    public class TorrentDownloadJob
    {
        private readonly ClientEngine engine;
        public TorrentManager manager;
        public Action<string> LogAction;
        public event Action OnEpisodeCountUpdated;
        public event Action OnDownloadFinished;
        public bool downloadFinished = false;
        public string saveFolder;

        public TorrentDownloadJob(ClientEngine engine)
        {
            this.engine = engine;
        }

        public async Task AddDownloadAsync(string torrentFilePath, string _saveFolder)
        {
            saveFolder = _saveFolder;
            if (!File.Exists(torrentFilePath))
                throw new FileNotFoundException("Торрент не знайдено", torrentFilePath);

            Directory.CreateDirectory(saveFolder);

            manager = await engine.AddAsync(torrentFilePath, saveFolder);
        }

        public List<int> GetAvailableEpisodes()
        {
            return manager.Files
                .Where(f => f.Path.EndsWith(".mkv") || f.Path.EndsWith(".mp4"))
                .Select(f => ExtractEpisodeNumber(f.Path))
                .Where(n => n != int.MaxValue)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }

        public async Task StartDownloadAsync(List<int> selectedEpisodes)
        {
            await manager.ChangePickerAsync(new StreamingPieceRequester());

            var videoFiles = manager.Files
            .Where(f => f.Path.EndsWith(".mkv") || f.Path.EndsWith(".mp4"))
            .Select(f => new { File = f, Episode = ExtractEpisodeNumber(f.Path) })
            .Where(x => selectedEpisodes.Contains(x.Episode))
            .OrderBy(x => x.Episode)
            .Select(x => x.File)
            .ToList();


            if (!videoFiles.Any())
            {
                LogAction?.Invoke("❌ Не знайдено відеофайлів для завантаження.");
                return;
            }

            var startLog = new StringBuilder();
            startLog.AppendLine("🎯 Починаю качати файли:");
            foreach (var file in videoFiles)
            {
                OnEpisodeCountUpdated?.Invoke();
                startLog.AppendLine($"\u2014 {file.Path}");
            }
            LogAction?.Invoke(startLog.ToString());

            foreach (var f in manager.Files)
                await manager.SetFilePriorityAsync(f, Priority.DoNotDownload);

            foreach (var f in videoFiles)
                await manager.SetFilePriorityAsync(f, Priority.Low);

            var firstIncomplete = videoFiles.FirstOrDefault(f => !f.BitField.AllTrue);
            if (firstIncomplete != null)
                await manager.SetFilePriorityAsync(firstIncomplete, Priority.High);

            await manager.StartAsync();

            var completeLog = new StringBuilder();

            while (videoFiles.Any(f => !f.BitField.AllTrue))
            {
                LogStatus();

                var next = videoFiles.FirstOrDefault(f => !f.BitField.AllTrue);
                if (next != null && next.Priority != Priority.High)
                {
                    foreach (var f in videoFiles)
                        await manager.SetFilePriorityAsync(f, Priority.Low);

                    await manager.SetFilePriorityAsync(next, Priority.High);
                    LogAction?.Invoke($"🔥 Тепер з пріоритетом: {next.Path}");
                }

                foreach (var file in videoFiles)
                {
                    OnEpisodeCountUpdated?.Invoke();
                    if (file.BitField.AllTrue && !completeLog.ToString().Contains(file.Path))
                        completeLog.AppendLine($"✅ Завантажено: {file.Path}");
                }

                await Task.Delay(1000);
            }

            LogAction?.Invoke(completeLog.ToString());
            LogAction?.Invoke("🏁 Усі вибрані відеофайли завантажено.");
            OnDownloadFinished?.Invoke();
            downloadFinished = true;

            MainWindow.Global.mainPage?.DownloadedTitlesUpdate();
            TorrentDownloader.RemoveManager(saveFolder);
        }

        public void FeedBackConnect()
        {
            if (downloadFinished)
            {
                LogAction?.Invoke("🏁 Усі вибрані відеофайли завантажено.");
                OnEpisodeCountUpdated?.Invoke();
            }
        }
        private void LogStatus()
        {
            var log = new StringBuilder();

            double totalSpeed = manager.Monitor.DownloadSpeed / 1024.0;
            log.AppendLine($"⬇️ Загальна швидкість: {totalSpeed:F2} KB/s");
            log.AppendLine("📂 Статус файлів:");

            foreach (var file in manager.Files.Where(f => f.Path.EndsWith(".mkv") || f.Path.EndsWith(".mp4")))
            {
                long size = file.Length;
                long downloaded = file.BytesDownloaded();
                double progress = size > 0 ? (downloaded / (double)size) * 100.0 : 0;
                double mbDownloaded = downloaded / (1024.0 * 1024.0);
                double mbSize = size / (1024.0 * 1024.0);
                string priority = file.Priority.ToString();

                log.AppendLine($"\u2014 {Path.GetFileName(file.Path)}: {mbDownloaded:F2} / {mbSize:F2} MB | {progress:F1}% | prioryty: {priority}");
            }

            LogAction?.Invoke(log.ToString());
        }

        private int ExtractEpisodeNumber(string fileName)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(fileName, @"\[(\d{1,3})\]");
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int ep))
                    return ep;
            }

            return int.MaxValue;
        }


        public async Task StopDownloadAsync()
        {
            if (manager != null &&
                (manager.State == TorrentState.Downloading || manager.State == TorrentState.Seeding))
            {
                await manager.StopAsync();
                LogAction?.Invoke($"⏹ Зупинено торрент: {manager.Torrent.Name}");
            }
        }

    }
}
