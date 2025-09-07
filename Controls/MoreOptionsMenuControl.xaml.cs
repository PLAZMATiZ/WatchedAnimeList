using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WatchedAnimeList.Helpers;
using Debug = WatchedAnimeList.Helpers.Debug;

namespace WatchedAnimeList.Controls
{
    public partial class MoreOptionsMenuControl : UserControl
    {
        public MoreOptionsMenuControl()
        {
            InitializeComponent();
            DownloadedTitlesUpdate();
        }
        
        public void DownloadedTitlesUpdate()
        {
            var titles = new List<string>();

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");
            if (!Directory.Exists(path))
            {
                DownloadedAnimeTitlesText.Visibility = Visibility.Visible;
                return;
            }
            titles = Directory.GetDirectories(path).ToList();

            DownloadedAnimeTitlesList.ItemsSource = titles;
        }
        public void DownloadHistory_Button_Click(object sender, EventArgs e)
        {
            if (AnimeDownloads.Visibility == Visibility.Collapsed)
            {
                AnimeDownloads.Visibility = Visibility.Visible;
            }
            else
            {
                AnimeDownloads.Visibility = Visibility.Collapsed;
            }
        }
        public void DeleteTitle_Click(object sender, EventArgs e)
        {
            string? folderPath = (sender as Button)?.Tag as string;
            if (folderPath is null)
                Debug.Ex("folderPath is null");

            if (!string.IsNullOrEmpty(folderPath))
            {
                if (Directory.Exists(folderPath))
                {
                    Debug.Log($"Видалення скачаного аніме: {Path.GetFileName(folderPath)}");
                    try
                    {
                        TorrentDownloader.RemoveManager(folderPath);
                        Directory.Delete(folderPath, true);
                        DownloadedTitlesUpdate();
                        Debug.Log($"Успішне видалення");
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Помилка при видаленні: {ex}", NotificationType.Error);
                    }
                }
                else
                    Debug.Log($"Директорія для видалення не існує", NotificationType.Error);
            }
            else
                Debug.Log("Шлях для видалення пустий", NotificationType.Error);
        }
        
        public void Settings_Button_Click(object sender, EventArgs e)
        {
            var page = new SettingsConfig_Page();

            PagesHelper.GoToPage(page);
        }
        public void UpdateApp_Button_Click(object sender, EventArgs e)
        {
            const string fileToDownload = "WAL_Updater.exe";

            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileToDownload);

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
        public void Exit_Clik(object sender, EventArgs e)
        {
            MainPage.Global.RightPopupContent.Content = null;
        }
        public void OpenTitle_Click(object sender, EventArgs e)
        {
            string? folderPath = (sender as Button)?.Tag as string;
            if (folderPath is null)
                Debug.Ex("folderPath is null");

            if (!string.IsNullOrEmpty(folderPath))
            {
                string folderName = Path.GetFileName(folderPath);
                string torrentPath = Path.Combine(folderPath, folderName + ".torrent");

                if (File.Exists(torrentPath))
                {
                    WatchAnimePage page = new(torrentPath, false);

                    MainWindow.Global.MainContent.Content = page;
                }
                else
                {
                    Debug.ShowAndLog("Торрент файл не знайдено.");
                }
            }
        }
    }
}
