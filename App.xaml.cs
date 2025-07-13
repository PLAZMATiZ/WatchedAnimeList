using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using WatchedAnimeList.Helpers;
using System.Diagnostics;
using Debug = WatchedAnimeList.Helpers.Debug;
using System.Text.Json;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

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
            try
            {
                Debug.Log("=== WAL starting via Main() ===");

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    Debug.Log($"[UnhandledException] {e.ExceptionObject}\n", NotificationType.Error);
                };

                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    Debug.Log($"[Task Exception] {e.Exception}", NotificationType.Error);
                    e.SetObserved();
                };

                var app = new WatchedAnimeList.App();

                System.Windows.Application.Current.DispatcherUnhandledException += (s, e) =>
                {
                    Debug.Log($"[UI Exception] {e.Exception}", NotificationType.Error);
                    e.Handled = true;
                };

                app.InitializeComponent();
                app.Startup += app.Application_Startup;
                app.Run();
            }
            catch (Exception ex)
            {
                Debug.Log($"[MAIN try-catch] {ex}", NotificationType.Error);
            }
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // Створюємо MainWindow
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                mainWindow.Show(); // Показуємо вікно

#if !DEBUG
                if (!AutorunHelper.IsAutorunEnabled("WatchedAnimeList"))
                {
                    string exePath = Process.GetCurrentProcess().MainModule.FileName;
                    if (!File.Exists(exePath))
                        Debug.Show($"Executable not found: {exePath}");
                    AutorunHelper.EnableAutorun("WatchedAnimeList", exePath);
                }
                UpdateUpdater();
#endif
            }
            catch (Exception ex)
            {
                Debug.Log("App startup crash: " + ex);
            }
            CreateNotifyIcon();
        }

        private NotifyIcon _notifyIcon;
        private void CreateNotifyIcon()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("WatchedAnimeList.Assets.Icon.ico");
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
            Current.Shutdown();
        }

        private async void UpdateUpdater()
        {
            const string repoApi = "https://api.github.com/repos/PLAZMATiZ/WatchedAnimeList/releases/tags/updater";
            const string fileToDownload = "WAL_Updater.exe";

            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileToDownload);

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
            var json = JsonSerializer.Serialize(versions, new JsonSerializerOptions
            {
                WriteIndented = true
            });

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
