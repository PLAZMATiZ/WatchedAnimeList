using System.IO;
using Path = System.IO.Path;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DeepL;
using JikanDotNet;
using static System.Net.Mime.MediaTypeNames;
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using FuzzySharp;
using System.Net.Http;

using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using System.Xml.Linq;
using System.ComponentModel;

using WatchedAnimeList.Controls;
using System.Windows.Threading;
using System.Collections.ObjectModel;

using WatchedAnimeList.Helpers;
using WatchedAnimeList.Models;
using WatchedAnimeList.ViewModels;
using System.Windows.Media.Effects;
using MonoTorrent;
using System.Diagnostics;
using Process = System.Diagnostics.Process;
using Debug = WatchedAnimeList.Helpers.Debug;

namespace WatchedAnimeList.Controls
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : System.Windows.Controls.UserControl
    {
        public static MainPage Global;
        public MainPage()
        {
            InitializeComponent();
            Global = this;
            try
            {
                this.DataContext = new AnimeViewModel();

                SetupSearchDelay();
                Huinya();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
            DownloadedTitlesLoad();
        }


        #region xz

        public void DownloadedTitlesLoad()
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
        private void Huinya()
        {
            AnimeCardList.PreviewMouseWheel += (sender, e) =>
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(AnimeCardList);
                if (scrollViewer != null)
                {
                    // Дробний крок, наприклад 0.2 рядка на 1 одиницю Delta
                    double delta = -e.Delta * 0.4; // 120 * 0.05 = 6 рядків за один крок, можна зменшити ще

                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + delta);
                    e.Handled = true;
                }
            };

        }
        public static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t)
                    return t;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
        #endregion

        #region Add Anime

        private string AnimeName;
        private string AnimeNameEN;

        #region Buffer Button
        private void AddAnimeButton_Click(object sender, EventArgs e)
        {
            string text = System.Windows.Clipboard.GetText();
            AddAnimeToWatched(text);
        }

        public void AddAnimeToWatched(string name)
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                if (SiteParser.Global.UrlValidate(name))
                {
                    var animeInfo = SiteParse(name);
                    return;
                }
                else if (TextVerify(name))
                {
                    _ = AnimeNameFormating(name);
                }
                else
                {
                    Debug.Show("Не вдалося розпізнати назву/посилання");
                }
            }
            else
            {
                Debug.Show("Не вдалося розпізнати назву/посилання");
            }
        }

        public async void AddEpisodeToWached(string titleName, int episode)
        {
            var anime = await GetAnimeTitle(titleName);
            titleName = anime.Titles.FirstOrDefault(t => t.Type == "English")?.Title
                     ?? anime.Titles.FirstOrDefault()?.Title
                     ?? "Unnamed";

            if (WachedAnimeSaveLoad.Global.wachedAnimeDict.ContainsKey(titleName))
            {
                WachedAnimeSaveLoad.Global.AddEpisode(titleName, episode);
            }
            else
            {
                AddAnimeToWatched(titleName);
            }
        }

        private async Task SiteParse(string url)
        {
            string title = "";
            string date = "";
            await SiteParser.Global.SiteParse(url, (_title, _date) =>
            {
                title = _title;
                date = _date;
            });

            string[] parts = title.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                AnimeName = parts[0].Trim();
                AnimeNameEN = parts[1].Trim();
            }
            else
            {
                var (eng, other) = SplitByEnglish(title);
                if (eng != "" && other != "")
                {
                    AnimeName = other.Trim();
                    AnimeNameEN = eng.Trim();
                }
            }

            if (AnimeNameEN == "" || AnimeName == "" || AnimeNameEN == null || AnimeName == null)
            {
                //
                //
                //
                //

                ///      додай приклад норм силки

                //
                Debug.Show($"Помилка при отриманні данних з {url}, перевірте коректність силки"); 
                //
                //
                //
                //
                //
                //

                return;
            }
            CreateAnimeCard(AnimeNameEN, AnimeName);
        }
        private static bool TextVerify(string text)
        {
            int letters = 0;

            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    letters++;
                }
            }

            if (letters < 3)
                return false;
            return true;
        }

        private async Task<(string, string)> AnimeNameFormating(string text)
        {
            string name = "";
            string eng_Name = "";

            string[] parts = text.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                name = parts[0].Trim();
                eng_Name = parts[1].Trim();
            }
            else
            {
                var (eng, other) = SplitByEnglish(text);
                if (eng != "" && other != "")
                {
                    name = other.Trim();
                    eng_Name = eng.Trim();
                }
                else
                {
                    name = text;

                    var client = new Translator("49d710dd-2897-4129-b171-2ea0548043c8:fx");
                    var translatedText = await client.TranslateTextAsync(
                    text,
                    LanguageCode.Russian,
                    LanguageCode.EnglishAmerican);
                    eng_Name = translatedText.ToString();
                }
            }

            AnimeName = name;
            AnimeNameEN = eng_Name;

            if (WachedAnimeSaveLoad.Global.wachedAnimeDict.ContainsKey(eng_Name))
            {
                Debug.ShowAndLog($"Аніме вже існує: {eng_Name}");
                return ("", "");
            }
            CreateAnimeCard(eng_Name, name);
            return (AnimeName, AnimeNameEN);
        }

        public async void CreateAnimeCard(string eng_Name, string name)
        {
            Anime title = await GetAnimeTitle(eng_Name);

            var animeData = await CreateWachedAnimeData(title, name);
            if (animeData == null)
                return;

            WachedAnimeSaveLoad.Global.AddAnime(animeData);

            AddAnimeCardsAsync([animeData]);
        }

        static bool IsEnglish(string word)
        {
            return Regex.IsMatch(word, "^[a-zA-Z]+$");
        }

        static (string eng, string other) SplitByEnglish(string input)
        {
            string[] words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            List<string> beforeEnglish = new();
            List<string> englishWords = new();

            bool foundEnglish = false;

            foreach (var word in words)
            {
                if (!foundEnglish && IsEnglish(word))
                {
                    foundEnglish = true;
                }

                if (foundEnglish)
                    englishWords.Add(word);
                else
                    beforeEnglish.Add(word);
            }

            return (string.Join(' ', englishWords), string.Join(' ', beforeEnglish));
        }
        #endregion

        #region Load Anime Icon

        public async Task<Anime> GetAnimeTitle(string animeNameEN)
        {
            var jikan = new Jikan();

            try
            {
                var searchResult = await jikan.SearchAnimeAsync(animeNameEN);
                var filtered = searchResult.Data.Where(a => a.Type != "Music").ToList();

                if (filtered?.Count > 0)
                {
                    var first = filtered.First();
                    return (filtered.First());
                }
                else
                {
                    Console.WriteLine("Аніме не знайдено.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Помилка: " + ex.Message);
            }
            return null;
        }

        private TaskCompletionSource<bool> userConfirmationTask;
        private async Task<WachedAnimeData> CreateWachedAnimeData(Anime anime, string animeName)
        {
            var wachedAnimeData = new WachedAnimeData();

            var title = anime.Titles.FirstOrDefault(t => t.Type == "English")?.Title
                     ?? anime.Titles.FirstOrDefault()?.Title
                     ?? "Unnamed";

            string imageUrl = anime.Images.JPG.ImageUrl;

            wachedAnimeData.AnimeNameEN = title;
            wachedAnimeData.AnimeName = animeName;
            wachedAnimeData.WatchedDate = DateTime.Now.ToString();

            if (WachedAnimeSaveLoad.Global.wachedAnimeDict.ContainsKey(title))
            {
                Debug.ShowAndLog($"Аніме вже існує: {title}");
                return null;
            }

            try
            {
                using var client = new HttpClient();
                var imgData = await client.GetByteArrayAsync(imageUrl);
                wachedAnimeData.AnimeImage = LoadImageFromBytes(imgData);
            }
            catch
            {
                wachedAnimeData.AnimeImage = new BitmapImage(new Uri("pack://application:,,,/Resources/defaultAnimeIcon.jpg"));
            }

            //
            // Встановлюю всі данні про карту
            //

            AnimeCardInfoImage.Source = wachedAnimeData.AnimeImage;
            AnimeCardInfoName.Text = wachedAnimeData.AnimeNameEN;

            //
            //
            //

            AnimeCardInfoPanel.Visibility = Visibility.Visible; // Показую вікно

            //
            // Очікуємо взаємодію з користувачем
            //

            userConfirmationTask = new TaskCompletionSource<bool>();

            bool confirmed = await userConfirmationTask.Task;

            AnimeCardInfoPanel.Visibility = Visibility.Collapsed;

            if (!confirmed)
                return null;

            return wachedAnimeData;
        }
        private BitmapImage LoadImageFromBytes(byte[] imageData)
        {
            using var ms = new MemoryStream(imageData);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }
        public async Task AddAnimeCardsAsync(WachedAnimeData[] animeArray)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var item in animeArray)
                {
                    var viewModel = new AnimeItemViewModel(item, OnAnimeCardClicked);
                    AnimeViewModel.Global.AnimeList.Add(viewModel);
                }

                WachedAnimeSaveLoad.Global.Save();
            });
        }
        public void OnAnimeCardClicked(string animeNameEN)
        {
            // Наприклад, відкриття деталей:
            Debug.Show($"Натиснуто: {animeNameEN}");
        }


        #endregion

        #endregion

        #region Search

        private System.Windows.Threading.DispatcherTimer searchDelayTimer;

        private void SetupSearchDelay()
        {
            searchDelayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(600)
            };

            searchDelayTimer.Tick += (s, e) =>
            {
                searchDelayTimer.Stop();

                string query = SearchBox.Text.ToLowerInvariant().Trim();

                if (string.IsNullOrWhiteSpace(query) || query == "пошук аніме...")
                {
                    SuggestionsBox.Visibility = Visibility.Collapsed;
                    return;
                }

                SearchAnime(query);
                SuggestionsBox.Visibility = Visibility.Visible;
            };

            SearchBox.TextChanged += (s, e) =>
            {
                searchDelayTimer.Stop();
                searchDelayTimer.Start();
            };
        }

        private void SearchAnime(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return;

            var sourceDict = WachedAnimeSaveLoad.Global.wachedAnimeDict;

            var results = sourceDict
                .Select(x => new
                {
                    Data = x.Value,
                    Score = FuzzySharp.Fuzz.Ratio(x.Value.AnimeName.ToLowerInvariant(), searchText)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            ReorderCards(results.Select(x => x.Data));
        }
        public void ReorderCards(IEnumerable<WachedAnimeData> reorderedList)
        {
            AnimeViewModel.Global.AnimeList.Clear();
            foreach (var data in reorderedList)
            {
                var viewModel = new AnimeItemViewModel(data, OnAnimeCardClicked);
                AnimeViewModel.Global.AnimeList.Add(viewModel);
            }
            
            WachedAnimeSaveLoad.Global.Save();
        }

        #endregion

        #region UI Items

        private void SuggestionsBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SuggestionsBox.SelectedItem is string selected)
            {
                SearchBox.Text = selected;
                SuggestionsBox.Visibility = Visibility.Collapsed;
            }
        }
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Пошук аніме...")
            {
                SearchBox.Text = "";
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Пошук...";
            }
        }

        // Підтвердженя аніме
        public void AcceptButton_Click(object sender, EventArgs e)
        {
            AnimeCardInfoPanel.Visibility = Visibility.Collapsed;
            userConfirmationTask?.TrySetResult(true);
        }

        // Відхилення аніме
        public void RejectButton_Click(object sender, EventArgs e)
        {
            AnimeCardInfoPanel.Visibility = Visibility.Collapsed;
            userConfirmationTask?.TrySetResult(false);
        }

        public void MenuMoreButton_Click(object sender, EventArgs e)
        {
            if (MoreOptionsMenuPanel.Visibility == Visibility.Collapsed)
            {
                var blur = new BlurEffect
                {
                    Radius = 5 // сила розмиття
                };
                ContentGrid.Effect = blur;
                MoreOptionsMenuPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ContentGrid.Effect = null;
                MoreOptionsMenuPanel.Visibility = Visibility.Collapsed;
            }
        }

        public void MoreOptionsMenuExit_Clik(object sender, EventArgs e)
        {
            MoreOptionsMenuPanel.Visibility = Visibility.Collapsed;
            ContentGrid.Effect = null;
        }

        public void OpenTitle_Click(object sender, EventArgs e)
        {
            string folderPath = (sender as Button)?.Tag as string;
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
        public void DeleteTitle_Click(object sender, EventArgs e)
        {
            string folderPath = (sender as Button)?.Tag as string;
            if (!string.IsNullOrEmpty(folderPath))
            {
                if (Directory.Exists(folderPath))
                {
                    Debug.Log($"Видалення скачаного аніме: {Path.GetFileName(folderPath)}");
                    try
                    {
                        Directory.Delete(folderPath, true);
                        DownloadedTitlesLoad();
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
        #endregion
    }
}