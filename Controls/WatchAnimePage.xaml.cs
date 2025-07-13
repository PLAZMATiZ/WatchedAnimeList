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


namespace WatchedAnimeList.Controls
{
    public partial class WatchAnimePage : System.Windows.Controls.UserControl
    {
        public WatchAnimePage(string torrentFile)
        {
            InitializeComponent();
            HandleTorrentDrop(torrentFile);

            _ = HandleTorrentDrop(torrentFile);
        }
        private string animeFolderPath;
        private string animeName = "";
        private int episodeCount = 0;

        private async Task HandleTorrentDrop(string torrentFile)
        {
            animeName = Path.GetFileNameWithoutExtension(torrentFile);
            TitleTextBox.Text = animeName;

            string downloadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", animeName);
            animeFolderPath = downloadsPath;
            Directory.CreateDirectory(downloadsPath);

            try
            {
                File.Copy(torrentFile, Path.Combine(downloadsPath, $"{animeName}.torrent"), overwrite: true);
            }
            catch { }

            if (TorrentDownloader.IsDownloading(downloadsPath))
            {
                await TorrentDownloader.ContinueDownloadFeedback(downloadsPath, this, logAction: (msg) =>
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
                await TorrentDownloader.StartDownloadAsync(torrentFile, downloadsPath, this, logAction: (msg) =>
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

        private static TaskCompletionSource<bool> tcs;
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
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#34363a"),
                BorderThickness = new Thickness(2),
                Background = (Brush)new BrushConverter().ConvertFrom("#1e1e1e"),
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

        public void Dispose()
        {

        }
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