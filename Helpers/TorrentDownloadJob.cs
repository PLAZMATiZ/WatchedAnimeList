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
        public TorrentManager? manager;
        public Action<string> LogAction = (a) => { };
        public Action OnEpisodeCountUpdated = () => { };
        public Action OnDownloadFinished = () => { };
        public bool downloadFinished = false;
        public string? saveFolder;

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

            manager = await engine.AddAsync(torrentFilePath, saveFolder, new TorrentSettingsBuilder
            {
                UploadSlots = 16,
                MaximumConnections = 1000, // макс піpів на цей торрент
                AllowDht = true,
                AllowPeerExchange = true,
                MaximumDownloadRate = 0,
                MaximumUploadRate = 0,
                RequirePeerIdToMatch = false
            }.ToSettings());
        }

        public List<int> GetAvailableEpisodes()
        {
            if (manager is null)
                Debug.Ex("manager is null");

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
            if (manager is null)
                Debug.Ex("manager is null");

            await manager.ChangePickerAsync(new StandardPieceRequester(new PieceRequesterSettings() { }));
            //await manager.ChangePickerAsync(new HybridPieceRequester(manager));


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

            var firstThreeIncomplete = videoFiles.Where(f => !f.BitField.AllTrue).Take(3);
            foreach (var f in firstThreeIncomplete)
                await manager.SetFilePriorityAsync(f, Priority.High);

            await manager.StartAsync();

            var completeLog = new StringBuilder();

            while (videoFiles.Any(f => !f.BitField.AllTrue))
            {
                LogStatus();

                // Вибираємо перші 3 незакінчені файли
                var topThree = videoFiles.Where(f => !f.BitField.AllTrue).Take(3).ToList();

                // Спочатку ставимо Low для всіх відеофайлів
                foreach (var f in videoFiles)
                    await manager.SetFilePriorityAsync(f, Priority.Low);

                // Піднімаємо High тільки для перших трьох
                foreach (var f in topThree)
                    await manager.SetFilePriorityAsync(f, Priority.High);

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

            if (saveFolder is null)
                Debug.Ex("saveFolder is null");

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

        private readonly Dictionary<string, long> lastDownloaded = new();
        private readonly Dictionary<string, double> fileSpeed = new();
        double alpha = 0.2;
        private void LogStatus()
        {
            var log = new StringBuilder();
            log.AppendLine("📂 Статус файлів:");

            double totalSpeed = 0;
            int totalPeers = 0;

            if (manager is null)
                Debug.Ex("manager is null");

            foreach (var file in manager.Files
                .Where(f => f.Path.EndsWith(".mkv") || f.Path.EndsWith(".mp4"))
                .OrderBy(f => ExtractEpisodeNumber(f.Path)))
            {
                int ep = ExtractEpisodeNumber(file.Path);
                long size = file.Length;
                long downloaded = file.BytesDownloaded();
                double progress = size > 0 ? (downloaded / (double)size) * 100.0 : 0;
                double mbDownloaded = downloaded / (1024.0 * 1024.0);
                double mbSize = size / (1024.0 * 1024.0);

                string status;
                double speed = 0;
                int peers = manager.Peers.Available + manager.Peers.Seeds;
                totalPeers += peers;

                if (file.BitField.AllTrue)
                {
                    status = "✔ завершено";
                }
                else if (file.Priority == Priority.DoNotDownload)
                {
                    status = "❌ не качається";
                }
                else
                {
                    status = file.Priority == Priority.High ? "⬇️ нормальний" : "в черзі";

                    if (!lastDownloaded.ContainsKey(file.Path))
                        lastDownloaded[file.Path] = downloaded;

                    long delta = downloaded - lastDownloaded[file.Path];
                    lastDownloaded[file.Path] = downloaded;

                    double newSpeed = delta / 1024.0; // KB/s за останню секунду
                    if (!fileSpeed.ContainsKey(file.Path))
                        fileSpeed[file.Path] = newSpeed;
                    else
                        fileSpeed[file.Path] = alpha * newSpeed + (1 - alpha) * fileSpeed[file.Path];

                    speed = fileSpeed[file.Path];
                    totalSpeed += speed;
                }

                log.AppendLine($"Ep {ep}: {mbDownloaded:F2}/{mbSize:F2} MB | {progress:F1}% | Peers: {peers} | {status} {(speed > 0 ? $"⬇️ {speed:F2} KB/s" : "")}");
            }

            log.Insert(0, $"🌐 Піри всіх файлів: {totalPeers} | Швидкість всіх файлів: {totalSpeed:F2} KB/s\n");
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
                if(manager.Torrent != null)
                    LogAction?.Invoke($"⏹ Зупинено торрент: {manager.Torrent.Name}");
            }
        }

    }
}
