using System.IO;
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

using WatchedAnimeList.Helpers;
using WatchedAnimeList.Logic;
using WatchedAnimeList.Models;
using WatchedAnimeList.Controls;
using System.Windows.Threading;

namespace WatchedAnimeList
{
    public partial class MainWindow : Window
    {
        public static MainWindow Global;
        public MainWindow()
        {
            InitializeComponent();
            Global = this;

            new WachedAnimeSaveLoad().Initialize();
            new SiteParser().Initialize();
            new Settings(this);

            SetupSearchDelay();
        }

        #region Add Anime

        private string AnimeName;
        private string AnimeNameEN;

        #region Buffer Button
        private void AddAnimeButton_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText();

                if (SiteParser.Global.UrlValidate(text))
                {
                    var animeInfo = SiteParse(text);
                    return;
                }
                else if (TextVerify(text))
                {
                    AnimeNameFormating(text);
                }
                else
                {
                    MessageBox.Show("Даун шо за хуйня а не текст");
                }
            }
            else
            {
                MessageBox.Show("Даун скопіюй нормально");
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
                MessageBox.Show("Силка хуйня");
                return;
            }
            CreateAnimeCard(AnimeNameEN, AnimeName);
        }
        private static bool TextVerify(string text)
        {
            int letters = 0;
            //int slash = 0;

            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    letters++;
                }
                //else if (c == '/')
                //{
                //slash++;
                //}
            }

            if (letters < 3)
                return false;
            //if (slash > 1)
            //return false;
            return true;
        }

        private async void AnimeNameFormating(string text)
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
                return;
            }
            CreateAnimeCard(eng_Name, name);
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

        private Dictionary<string, AnimeCard> cardCache = new();
        public void AddAnimeCardsAsync(WachedAnimeData[] animeArray)
        {
            foreach (var item in animeArray)
            {
                var card = new AnimeCard(item.AnimeName, item.AnimeImage);
                card.CardClicked += (s, animeName) =>
                {
                    AnimeCardInfoPanel.Visibility = Visibility.Visible;
                    cardCache[item.AnimeName] = card;
                };
                AnimeCardWrapPanel.Children.Add(card);
            }
            WachedAnimeSaveLoad.Global.Save();
        }

        public void UpdateList()
        {
            var keys = WachedAnimeSaveLoad.Global.wachedAnimeDict.Keys;
            foreach (var item in keys)
            {
                var card = new AnimeCard(item, WachedAnimeSaveLoad.Global.wachedAnimeDict[item].AnimeImage);
                card.CardClicked += (s, animeName) =>
                {
                    MessageBox.Show($"Ти клікнув по: {animeName}");
                };
                AnimeCardWrapPanel.Children.Add(card);
            }
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
            AnimeCardWrapPanel.Children.Clear();

            foreach (var data in reorderedList)
            {
                if (cardCache.TryGetValue(data.AnimeName, out var card))
                {
                    AnimeCardWrapPanel.Children.Add(card);
                }
                else
                {
                    // fallback якщо картки немає — створити
                    var newCard = new AnimeCard(data.AnimeName, data.AnimeImage);
                    newCard.CardClicked += (s, animeName) =>
                    {
                        AnimeCardInfoPanel.Visibility = Visibility.Visible;
                    };
                    cardCache[data.AnimeName] = newCard;
                    AnimeCardWrapPanel.Children.Add(newCard);
                }
            }
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

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Закрити
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Мінімізувати
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // Тогл розгорнутого/віконного режиму
        private void WindowedButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
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
        #endregion
    }
}