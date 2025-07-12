using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.PiecePicking;

namespace WatchedAnimeList.Helpers
{
    public class TorrentDownloader
    {
        private ClientEngine engine;
        private TorrentManager manager;
        public event Action OnEpisodeCountUpdated;
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

            await manager.ChangePickerAsync(new StreamingPieceRequester());

            var videoFiles = manager.Files
                .Where(f => f.Path.EndsWith(".mkv") || f.Path.EndsWith(".mp4"))
                .OrderBy(f => ExtractEpisodeNumber(f.Path))
                .ToList();

            if (!videoFiles.Any())
            {
                LogAction?.Invoke("❌ Не знайдено відеофайлів для завантаження.");
                return;
            }

            // Групований лог старту
            var startLog = new System.Text.StringBuilder();
            startLog.AppendLine("🎯 Починаю качати файли:");
            foreach (var file in videoFiles)
            {
                OnEpisodeCountUpdated?.Invoke();
                startLog.AppendLine($"— {file.Path}");
            }
            LogAction?.Invoke(startLog.ToString());

            // Встановлюємо DoNotDownload всім
            foreach (var f in manager.Files)
                await manager.SetFilePriorityAsync(f, Priority.DoNotDownload);

            // Встановлюємо Low всім відеофайлам
            foreach (var f in videoFiles)
                await manager.SetFilePriorityAsync(f, Priority.Low);

            // Перший нескачаний — High
            var firstIncomplete = videoFiles.FirstOrDefault(f => !f.BitField.AllTrue);
            if (firstIncomplete != null)
                await manager.SetFilePriorityAsync(firstIncomplete, Priority.High);

            await manager.StartAsync();

            var completeLog = new System.Text.StringBuilder();

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
            LogAction?.Invoke("🏁 Усі відеофайли завантажено.");
            OnDownloadFinished?.Invoke();

            MainWindow.Global.mainPage?.DownloadedTitlesLoad();
        }

        private int ExtractEpisodeNumber(string fileName)
        {
            // шукаємо [число] у імені
            var start = fileName.LastIndexOf('[');
            var end = fileName.LastIndexOf(']');

            if (start >= 0 && end > start)
            {
                var numStr = fileName.Substring(start + 1, end - start - 1);
                if (int.TryParse(numStr, out int ep))
                    return ep;
            }

            return int.MaxValue; // якщо не знайшли номер — кладемо в кінець
        }


        private void LogStatus()
        {
            var log = new System.Text.StringBuilder();

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

                log.AppendLine($"— {Path.GetFileName(file.Path)}: {mbDownloaded:F2} / {mbSize:F2} MB | {progress:F1}% | prioryty: {priority}");
            }

            LogAction?.Invoke(log.ToString());
        }

    }
}
