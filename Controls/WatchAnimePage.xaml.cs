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
using WatchedAnimeList.Models;
using System.Globalization;
using System.Windows.Data;
using System.Text.Json;
using System.Windows.Controls.Primitives;
using System.Text.RegularExpressions;


namespace WatchedAnimeList.Controls
{
    public partial class WatchAnimePage : System.Windows.Controls.UserControl
    {
        public WatchAnimePage(string torrentFilePath, bool copyTorrent = true)
        {
            InitializeComponent();

            _ = HandleTorrentDrop(torrentFilePath, copyTorrent);
        }
        private string? animeFolderPath;
        private string animeName = "";

        private void SetWachedEpisodes()
        {
            if(AnimeManager.ContainsAnime(animeName))
            {
                WachedAnimeData? animeData = null;
                if (AnimeManager.TryGetWachedAnimeData(animeName, out animeData))
                {
                    if (animeData != null && animeData.WatchedEpisodesSet != null)
                    {
                        var watchedEpisodes = animeData.WatchedEpisodesSet;

                        int count = watchedEpisodes.Count;
                        string lastEp = watchedEpisodes.Any() ? watchedEpisodes.Last().ToString() : "N/A";
                        EpisodesCountText.Text = $"Переглянуто серій: {count} \n Остання серія {lastEp}";
                        Debug.Log("Оновлюю кількість переглянутих епізодів");
                    }
                    else
                    {
                        Debug.Log("Переглянутих епізодів не знайдено", NotificationType.Info);
                        EpisodesCountText.Text = $"Переглянуто серій: 0";
                    }
                }
            }
        }
        private async Task HandleTorrentDrop(string torrentFilePath, bool copyTorrent)
        {
            animeName = Path.GetFileNameWithoutExtension(torrentFilePath);
            TitleNameFormater(animeName, out string name, out string episodes);
            animeName = Regex.Replace(name, @" - .*?\[.*?\]$", "").Trim();
            SetWachedEpisodes();

            TitleTextBox.Text = name;

            string downloadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", animeName);
            animeFolderPath = downloadsPath;
            Directory.CreateDirectory(downloadsPath);

            string? torrentPath = "";
            if(copyTorrent)
            {
                torrentPath = CopyTorrent(torrentFilePath, downloadsPath);

                if(torrentPath is null)
                    Debug.Ex("torrentPath is null");
            }
            else
            {
                torrentPath = torrentFilePath;
            }
            if (TorrentDownloader.IsDownloading(downloadsPath))
            {
                await TorrentDownloader.DownloadFeedback(downloadsPath, this, logAction: (msg) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogTextBox.Text = msg;
                    });
                },
                onFinished: () =>
                {
                    var episodes = Directory.GetFiles(animeFolderPath, "*", SearchOption.AllDirectories)
                                 .Where(f => f.EndsWith(".mkv") || f.EndsWith(".mp4"))
                                 .OrderBy(f => f)
                                 .ToList();

                    AnimeEpisodesList.ItemsSource = episodes;
                },
                onUpdate: () =>
                {
                    var episodes = Directory.GetFiles(animeFolderPath, "*", SearchOption.AllDirectories)
                                 .Where(f => f.EndsWith(".mkv") || f.EndsWith(".mp4"))
                                 .OrderBy(f => f)
                                 .ToList();

                    AnimeEpisodesList.ItemsSource = episodes;
                }
            );
            }
            else
            {
                await TorrentDownloader.StartDownloadAsync(torrentPath, downloadsPath, this, logAction: (msg) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogTextBox.Text = msg;
                    });
                },
                onFinished: () =>
                {
                    var episodes = Directory.GetFiles(animeFolderPath, "*", SearchOption.AllDirectories)
                                 .Where(f => f.EndsWith(".mkv") || f.EndsWith(".mp4"))
                                 .OrderBy(f => f)
                                 .ToList();

                    AnimeEpisodesList.ItemsSource = episodes;
                },
                onUpdate: () =>
                {
                    var episodes = Directory.GetFiles(animeFolderPath, "*", SearchOption.AllDirectories)
                                 .Where(f => f.EndsWith(".mkv") || f.EndsWith(".mp4"))
                                 .OrderBy(f => f)
                                 .ToList();

                    AnimeEpisodesList.ItemsSource = episodes;
                }
            );
            }

        }

        private string? CopyTorrent(string torrentFile, string downloadsPath)
        {
            var path = Path.Combine(downloadsPath, $"{animeName}.torrent");
            try
            {
                File.Copy(torrentFile, path, overwrite: true);
                return path;
            }
            catch { }
            return null;
        }
        private static TaskCompletionSource<bool>? tcs;
        private readonly List<ToggleButton> downloadEpisodesToggles = new();
        public List<int> GetEpisodesToDownload()
        {
            return downloadEpisodesToggles
                .Where(t => t.IsChecked == true)
                .Select(t => (int)t.Tag)
                .ToList();
        }
        public void AddEpisodeToggle(int episodeNumber)
        {

            var toggle = new ToggleButton
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Style = (Style)Application.Current.FindResource("CustomToggleButton"),
                Tag = episodeNumber
            };
            downloadEpisodesToggles.Add(toggle);

            var border = new Border
            {
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#34363a")!,
                BorderThickness = new Thickness(2),
                Background = (Brush)new BrushConverter().ConvertFrom("#1e1e1e")!,
                Margin = new Thickness(6, 12, 6, 6),
                CornerRadius = new CornerRadius(10),

                Child = new Border
                {
                    Child = new Grid
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"Епізод {episodeNumber}",
                                MinHeight = 30,
                                Foreground = Brushes.White,
                                FontSize = 20,
                                FontFamily = new FontFamily("Segoe Print"),
                                Margin = new Thickness(12, 0, 0, 0),
                                VerticalAlignment = VerticalAlignment.Center
                            },
                            toggle
                        }
                    }
                }
            };

            EpisodeToDownload_StackPanel.Children.Add(border);
        }
        public Task WaitForUserClickAsync()
        {
            tcs = new TaskCompletionSource<bool>();
            return tcs.Task;
        }
        private void DownloadEpisodes_Button_Click(object sender, EventArgs e)
        {
            tcs?.TrySetResult(true);
        }
        public void ShowSelectEpisodes()
        {
            SelectEpisodesToDownloadPanel.Visibility = Visibility.Visible;
        }
        public void HideSelectEpisodes()
        {
            SelectEpisodesToDownloadPanel.Visibility = Visibility.Collapsed;
        }

        private void PlayEpisode_Click(object sender, RoutedEventArgs e)
        {
            string? path = (sender as Button)?.Tag as string;
            if (path is null)
                Debug.Ex("path is null");

            if (!string.IsNullOrEmpty(path))
            {
                StartMpvPlayer(path);
            }
        }



        #region  MPV
        private NamedPipeClientStream? pipeClient;
        private StreamWriter? writer;
        private StreamReader? reader;
        private Process? mpvProcess;
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
            mpvProcess.Start();
            _ = q();
        }

        private async Task q()
        {
            await ConnectToPipeAsync();  // ⏳ чекаємо підключення до пайпу
            await ListenToMpvEvents();   // 🔄 слухаємо події
        }

        private async Task ConnectToPipeAsync()
        {
            pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeClient.ConnectAsync(3000);

            writer = new StreamWriter(pipeClient) { AutoFlush = true };
            reader = new StreamReader(pipeClient);
        }

        private static void TitleNameFormater(string text, out string name, out string episodes)
        {
            string pattern = @"\[(\d+-\d+)\]";
            Match match = Regex.Match(text, pattern);

            if (match.Success)
            {
                episodes = match.Groups[1].Value;

                name = Regex.Replace(text, pattern, "").Trim();
            }
            else
            {
                name = text.Trim();
                episodes = string.Empty;
            }
        }

        private int? GetEpisode(string name)
        {
            string pattern = @"\[(\d+)\]";
            Match match = Regex.Match(name, pattern);

            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, out int episode))
                {
                    // Видаляємо [число] з назви
                    name = Regex.Replace(name, pattern, "").Trim();
                    return episode;
                }
            }
            return null;
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
                    if (reader is null)
                        Debug.Ex("reader is null");

                    string? line = await reader.ReadLineAsync();
                    if (line != null && line.Contains("\"event\":"))
                    {
                        if (line.Contains("\"file-loaded\""))
                        {
                            string path = await GetMpvProperty("path");
                            if (!string.IsNullOrEmpty(path))
                            {
                                if (!watchedFiles.Contains(path))
                                {
                                    watchedFiles.Add(path);

                                    var episodeName = Path.GetFileName(path);

                                    var episode = GetEpisode(episodeName);
                                    if(episode == null)
                                    {
                                        Debug.Log($"Не вдалося визначити серію", NotificationType.Error);
                                        return;
                                    }
                                    Debug.Log($"Додаю епізод до переглянутого (еп.{(int)episode})", NotificationType.Info);
                                    AnimeManager.AddEpisode(animeName, (int)episode);
                                    SetWachedEpisodes();
                                }
                                else
                                {
                                    Debug.Log($"↩️ Вже бачив: {Path.GetFileName(path)}", NotificationType.Info);
                                }
                            }
                        }
                    }
                }
                catch { break; }
            }
        }

        private async Task<string> GetMpvProperty(string property)
        {
            if (writer is null)
                Debug.Ex("reader is null");
            if (reader is null)
                Debug.Ex("reader is null");

            var request = $"{{ \"command\": [\"get_property\", \"{property}\"] }}";
            await writer.WriteLineAsync(request);

            while (true)
            {
                var response = await reader.ReadLineAsync();
                if (response != null && response.Contains("\"data\""))
                {
                    using var doc = JsonDocument.Parse(response);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var mpvProperty = data.GetString();
                        if (mpvProperty is null)
                            Debug.Ex("mpvProperty is null");

                        return mpvProperty;
                    }
                }
            }
        }

        private void MainPage_Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Global.MainPage();
        }
        #endregion

        public void Dispose()
        {

        }
    }

    public class FileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? path = value as string;
            if (string.IsNullOrEmpty(path)) return "";
            return Path.GetFileName(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}