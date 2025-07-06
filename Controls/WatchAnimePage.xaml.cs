using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO.Pipes;
using System.Threading.Tasks;
using WatchedAnimeList.Helpers;
using Debug = WatchedAnimeList.Helpers.Debug;
using System.Globalization;
using System.Windows.Data;
using System.Text;


namespace WatchedAnimeList.Controls
{
    public partial class WatchAnimePage : System.Windows.Controls.UserControl
    {
        public WatchAnimePage(string torrentFile)
        {
            InitializeComponent();
            HandleTorrentDrop(torrentFile);

            _ = InitAsync(torrentFile);
        }
        private async Task InitAsync(string torrentFile)
        {
            await HandleTorrentDrop(torrentFile);
        }
        private string animeFolderPath;
        private string animeName = "";
        private int episodeCount = 0;


        private async Task HandleTorrentDrop(string torrentFile)
        {
            animeName = Path.GetFileNameWithoutExtension(torrentFile);
            TitleTextBox.Text = animeName;

            string savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", animeName);
            animeFolderPath = savePath;
            Directory.CreateDirectory(savePath);

            try
            {
                File.Copy(torrentFile, Path.Combine(savePath, $"{animeName}.torrent"), overwrite: true);
            }
            catch { }

            var downloader = new TorrentDownloader();
            downloader.LogAction = (msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogTextBox.Text = msg;
                });
            };

            downloader.OnDownloadFinished += () =>
            {
                var episodes = Directory.GetFiles(animeFolderPath, "*", SearchOption.AllDirectories)
                             .Where(f => f.EndsWith(".mkv") || f.EndsWith(".mp4"))
                            .OrderBy(f => f)
                            .ToList();

                AnimeEpisodesList.ItemsSource = episodes;
            };

            downloader.OnEpisodeCountUpdated += () =>
            {
                var episodes = Directory.GetFiles(animeFolderPath, "*", SearchOption.AllDirectories)
                             .Where(f => f.EndsWith(".mkv") || f.EndsWith(".mp4"))
                            .OrderBy(f => f)
                            .ToList();

                AnimeEpisodesList.ItemsSource = episodes;
            };

            await downloader.StartDownloadAsync(torrentFile, savePath);
        }
        private void PlayEpisode_Click(object sender, RoutedEventArgs e)
        {
            string path = (sender as Button)?.Tag as string;
            if (!string.IsNullOrEmpty(path))
            {
                StartMpvPlayer(path);
            }
        }



        #region  MPV
        private NamedPipeClientStream pipeClient;
        private StreamWriter writer;
        private StreamReader reader;
        private Process mpvProcess;
        private const string PipeName = "mpvsocket";
        private void StartMpvPlayer(string videoPath)
        {
            var mpvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mpv.exe");
            var pipePath = $@"\\.\pipe\{PipeName}";

            mpvProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = mpvPath,
                    Arguments = $"--input-ipc-server={pipePath} \"{videoPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
            mpvProcess.Exited += MpvProcess_Exited;
            mpvProcess.Start();
            _ = ConnectToPipeAsync(); // пайп підключення окремо
            q();
        }

        private async Task q()
        {
            await Task.Delay(1000);
            ListenToMpvEvents();
        }

        private async Task ConnectToPipeAsync()
        {
            pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeClient.ConnectAsync(3000);

            writer = new StreamWriter(pipeClient) { AutoFlush = true };
            reader = new StreamReader(pipeClient);
        }

        private void MpvProcess_Exited(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AddToWatched(); // виклич свій метод тут
            });
        }

        private void AddToWatched()
        {
            // Твоя логіка — наприклад, додати назву в список
            string title = TitleTextBox.Text.Trim();
            Debug.Show($"✅ Додано до переглянутого: {title}");
            // + запис у файл/базу, якщо треба
        }

        private async Task SendMpvCommand(string jsonCmd)
        {
            if (writer != null)
                await writer.WriteLineAsync(jsonCmd);
        }


        private async Task SendCommandAsync(string jsonCmd)
        {
            if (writer != null)
                await writer.WriteLineAsync(jsonCmd);
        }

        private HashSet<string> watchedFiles = new();

        private async Task ListenToMpvEvents()
        {
            while (mpvProcess != null && !mpvProcess.HasExited)
            {
                try
                {
                    string line = await reader.ReadLineAsync();
                    if (line != null && line.Contains("\"event\":"))
                    {
                        if (line.Contains("\"file-loaded\""))
                        {
                            string path = GetFilePathFromJson(line);
                            if (!string.IsNullOrEmpty(path))
                            {
                                if (!watchedFiles.Contains(path))
                                {
                                    watchedFiles.Add(path);
                                    Debug.Show($"👀 Переглянута серія: {Path.GetFileName(path)}");
                                    //
                                    //
                                    //

                                    //
                                    //
                                    // якшо переглянув

                                    //
                                    //
                                    //
                                    //
                                }
                                else
                                {
                                    Debug.Show($"↩️ Вже бачив: {Path.GetFileName(path)}");
                                }
                            }
                        }
                    }
                }
                catch { break; }
            }
        }

        private string GetFilePathFromJson(string json)
        {
            try
            {
                var obj = System.Text.Json.JsonDocument.Parse(json);
                if (obj.RootElement.TryGetProperty("event", out var evt) && evt.GetString() == "file-loaded")
                {
                    if (obj.RootElement.TryGetProperty("prefix", out var prefix) &&
                        obj.RootElement.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("path", out var path))
                    {
                        return path.GetString();
                    }
                }
            }
            catch { }

            return null;
        }


        private void MainPage_Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Global.MainPage();
        }
        #endregion
    }



    public class FileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value as string;
            if (string.IsNullOrEmpty(path)) return "";
            return Path.GetFileName(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}