using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using WatchedAnimeList.Helpers;
using System.Diagnostics;
using Debug = WatchedAnimeList.Helpers.Debug;

namespace WatchedAnimeList
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private NotifyIcon _notifyIcon;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Створюємо MainWindow
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show(); // Показуємо вікно

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






            string exePath = Process.GetCurrentProcess().MainModule.FileName;
                        if (!File.Exists(exePath))
                Debug.Show($"Executable not found: {exePath}");
            AutorunHelper.EnableAutorun("WatchedAnimeList", exePath);
            

            try
            {
                AutorunHelper.EnableAutorun("WatchedAnimeList", exePath);
            }
            catch (Exception ex)
            {
                Debug.Log("Failed to set autorun: " + ex.Message);
            }

            UpadteUpdater();
        }

        private async void UpadteUpdater()
        {
            const string repoApi = "https://api.github.com/repos/PLAZMATiZ/WatchedAnimeList/releases/tags/updater";
            const string fileToDownload = "WAL_Updater.exe";
            const string localPath = "WAL_Updater.exe";


            string? latestVersion = await GitHubVersionHelper.GetVersionFromReleaseNameAsync(repoApi);

            if (!string.IsNullOrEmpty(latestVersion))
            {
                bool success = await GitHubFileDownloader.DownloadFileFromLatestReleaseAsync(
                    repoApi,
                    fileToDownload,
                    localPath
                );

                if (success)
                {
                    Debug.Log("Updater успішно завантажено.");
                }
            }
            else
            {
                Debug.Log("Не вдалося визначити версію.");
            }

            string walTrayPath = Path.Combine(Directory.GetCurrentDirectory(), "WAL_Updater.exe");
            if(walTrayPath != null)
                Debug.Log("Updater запущено.");

            Process.Start(new ProcessStartInfo()
            {
                FileName = walTrayPath,
                UseShellExecute = true
            });
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
}
