using Microsoft.Win32;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;
using WatchedAnimeList.Helpers;
using WatchedAnimeList.Logic;
using Debug = WatchedAnimeList.Helpers.Debug;

namespace WatchedAnimeList
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        [STAThread]
        public static void Main()
        {
            Debug.Log("=== WAL starting via Main() ===");

            
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is DebugException) return;
                Debug.ShowAndLog($"[UnhandledException] {e.ExceptionObject}\n", NotificationType.Error);
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                var flat = e.Exception.Flatten();
                if (flat.InnerExceptions.All(x => x is DebugException))
                {
                    e.SetObserved();
                    return; // всі наші DebugException → ігноруємо
                }

                Debug.ShowAndLog($"[Task Exception] {flat}", NotificationType.Error);
                e.SetObserved();
            };

            var app = new WatchedAnimeList.App();

            System.Windows.Application.Current.DispatcherUnhandledException += (s, e) =>
            {
                if (e.Exception is DebugException) { e.Handled = true; return; }
                Debug.ShowAndLog($"[UI Exception] {e.Exception}", NotificationType.Error);
                e.Handled = true;
            };

            app.InitializeComponent();
            app.Startup += app.Application_Startup;
            app.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            GlobalToolTip.Init();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // Створюємо MainWindow
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                mainWindow.Show(); // Показуємо вікно

                CreateNotifyIcon();
            }
            catch (Exception ex)
            {
                Debug.Log("App startup crash: " + ex);
            }

#if !DEBUG
                if (!AutorunHelper.IsAutorunEnabled("WatchedAnimeList"))
                {
                    string? exePath = Process.GetCurrentProcess().MainModule?.FileName;

                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    {
                        Debug.Show($"Executable not found: {exePath}");
                        return;
                    }
                    AutorunHelper.EnableAutorun("WatchedAnimeList", exePath);
                }
                _ = UpdateUpdater();
#endif
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _ = AnimeManager.Save();
            Settings.SaveAll();
            NetworkHelper.StopMonitorInternet();
        }

        private NotifyIcon? _notifyIcon;
        private void CreateNotifyIcon()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using Stream? stream = assembly.GetManifestResourceStream("WatchedAnimeList.Assets.Icon.ico");
            if (stream == null)
            {
                Debug.Log(@"Ресурс не знайдено ""WatchedAnimeList.Assets.Icon.ico""");
                return;
            }

            // Створюємо трей-іконку
            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(stream),
                Visible = true,
                Text = "WatchedAnimeList"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Відкрити", null, (s, ev) => ShowMainWindow());
            contextMenu.Items.Add("Вийти", null, (s, ev) => Shutdown());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, ev) => ShowMainWindow();
        }

        private void OnOpenClick(object sender, RoutedEventArgs e)
        {
            if (MainWindow == null)
                MainWindow = new MainWindow();

            if (!MainWindow.IsVisible)
                MainWindow.Show();

            if (MainWindow.WindowState == WindowState.Minimized)
                MainWindow.WindowState = WindowState.Normal;

            MainWindow.Activate();
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            _ = AnimeManager.Save();
            Settings.SaveAll();
            Current.Shutdown();
        }

        private static async Task UpdateUpdater()
        {
            const string repoApi = "https://api.github.com/repos/PLAZMATiZ/WatchedAnimeList/releases/tags/updater";
            const string fileToDownload = "WAL_Updater.exe";

            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileToDownload);

            while (true)
            {
                try
                {

                    string? latestVersion = await GitHubVersionHelper.GetVersionFromReleaseNameAsync(repoApi);
                    string? localVersion = LocalVersionStorage.GetVersion("Updater");
                    Debug.Log($"Локальна версія {localVersion ?? "null"} остання {latestVersion}...");

                    if (!string.IsNullOrEmpty(latestVersion))
                    {
                        if (latestVersion == localVersion)
                        {
                            Debug.Log("Updater вже актуальний.");
                        }
                        else
                        {
                            Debug.Log($"Оновлення Updater");

                            bool success = await GitHubFileDownloader.DownloadFileFromLatestReleaseAsync(
                                repoApi,
                                fileToDownload,
                                localPath
                            );

                            if (success)
                            {
                                Debug.Log("Updater успішно завантажено.");
                                LocalVersionStorage.SetVersion("Updater", latestVersion);
                            }
                            else
                            {
                                Debug.Log("Завантаження Updater не вдалося.");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Не вдалося визначити версію.");
                    }
                    break;
                }
                catch
                {
                    await Task.Delay(5000);
                }
            }
            if (!File.Exists(localPath))
            {
                Debug.Log("Updater не знайдено за шляхом: " + localPath);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = localPath,
                    UseShellExecute = true
                });
                Debug.Log("Запуск Updater");
            }
            catch (Exception ex)
            {
                Debug.Log($"Помилка запуску Updater: {ex.Message}");
            }
        }


        private void ShowMainWindow()
        {
            if (MainWindow == null)
                MainWindow = new MainWindow();

            if (!MainWindow.IsVisible)
                MainWindow.Show();

            if (MainWindow.WindowState == WindowState.Minimized)
                MainWindow.WindowState = WindowState.Normal;

            MainWindow.Activate();
        }
    }

    public static class LocalVersionStorage
    {
        private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        private static readonly string VersionFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.json");

        public static Dictionary<string, string> Load()
        {
            if (!File.Exists(VersionFilePath))
                return new();

            string json = File.ReadAllText(VersionFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }

        public static void Save(Dictionary<string, string> versions)
        {
            var json = JsonSerializer.Serialize(versions, jsonSerializerOptions);

            File.WriteAllText(VersionFilePath, json);
        }

        public static string? GetVersion(string component)
        {
            var versions = Load();
            return versions.TryGetValue(component, out var version) ? version : null;
        }

        public static void SetVersion(string component, string version)
        {
            var versions = Load();
            versions[component] = version;
            Save(versions);
        }
    }
}
