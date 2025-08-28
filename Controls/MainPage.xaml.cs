using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data;

using WatchedAnimeList.Controls;
using System.Windows.Threading;
using System.Collections.ObjectModel;

using WatchedAnimeList.Helpers;
using WatchedAnimeList.Models;
using WatchedAnimeList.ViewModels;
using Debug = WatchedAnimeList.Helpers.Debug;
using DeepL;
using static System.Net.Mime.MediaTypeNames;

namespace WatchedAnimeList.Controls
{
    public partial class MainPage : System.Windows.Controls.UserControl
    {
        public static MainPage Global = null!;

        private System.Windows.Threading.DispatcherTimer searchDelayTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };

        public MainPage()
        {
            InitializeComponent();
            Global = this;
            try
            {
                this.DataContext = new AnimeViewModel();

                SetupSearchDelay();
                ScrollSetup();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
            LocalizationHelper.OnLanguageChanged += LocalizationChangedHandler;
            LocalizationChangedHandler(null, EventArgs.Empty);
        }
        private void LocalizationChangedHandler(object? sender, EventArgs e)
        {
            SearchBox.Text = LocalizationHelper.GetTranslation("Search anime...");
        }

        #region Scroll
        private void ScrollSetup()
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

        #region Search

        private void SetupSearchDelay()
        {
            searchDelayTimer.Tick += (s, e) =>
            {
                searchDelayTimer.Stop();

                string query = SearchBox.Text.ToLowerInvariant().Trim();

                if (string.IsNullOrWhiteSpace(query) || query == LocalizationHelper.GetTranslation("Search anime..."))
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

        public static void SearchAnime(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return;

            var sourceDict = AnimeManager.GetWatchedAnimeDict();

            var results = AnimeSearch.Search(
                sourceDict.Values,
                anime => anime.OriginalName ?? "",
                searchText
            );

            ReorderCards(results);
        }

        private static int LetterSkipScore(string source, string search)
        {
            var srcWords = source.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var searchWords = search.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int totalScore = 0;
            int lastMatchedIndex = -1;

            foreach (var sw in searchWords)
            {
                bool found = false;

                for (int offset = 1; offset <= 2 && !found; offset++)
                {
                    int w = lastMatchedIndex + offset;
                    if (w >= srcWords.Length) break;

                    var word = srcWords[w];
                    int bestScore = 0;

                    // пробуємо зі скіпом 0 або 1 букви
                    for (int skip = 0; skip <= 1; skip++)
                    {
                        int streak = 0;
                        int score = 0;
                        int mismatches = 0;

                        for (int i = 0; i < sw.Length; i++)
                        {
                            int j = i + skip;
                            if (j >= word.Length) break;

                            if (sw[i] == word[j])
                            {
                                streak++;
                                score += streak + Math.Max(1, 5 - i);
                                mismatches = 0;
                            }
                            else
                            {
                                mismatches++;
                                streak = 0;
                                if (mismatches >= 2) break;
                            }
                        }

                        if (score > bestScore)
                            bestScore = score;
                    }

                    if (bestScore > 0)
                    {
                        // зменшуємо бонус, якщо було перескакування слова
                        int jumpPenalty = (w - (lastMatchedIndex + 1)) * 2;
                        totalScore += Math.Max(bestScore - jumpPenalty, 1);
                        lastMatchedIndex = w;
                        found = true;
                        break;
                    }
                }
            }

            return totalScore;
        }


        public static void ReorderCards(IEnumerable<WachedAnimeData> reorderedList)
        {
            // 1. Створюємо словник для швидкого доступу до існуючих ViewModel
            var existingItems = AnimeViewModel.Global.AnimeList
                .Where(vm => !string.IsNullOrEmpty(vm.OriginalName))
                .ToDictionary(vm => vm.OriginalName!, vm => vm);


            // 2. Створюємо новий список у потрібному порядку
            var newOrder = new ObservableCollection<AnimeItemViewModel>();

            foreach (var data in reorderedList)
            {
                if (data.OriginalName is null)
                    Debug.Ex("data.OriginalName is null");

                if (existingItems.TryGetValue(data.OriginalName, out var existingViewModel))
                {
                    newOrder.Add(existingViewModel);
                }
                else
                {
                    // Якщо немає існуючого — створюємо новий
                    var newViewModel = new AnimeItemViewModel(data, AnimeManager.OnAnimeCardClicked);
                    newOrder.Add(newViewModel);
                }
            }

            // 3. Оновлюємо колекцію в ViewModel
            AnimeViewModel.Global.AnimeList.Clear();
            foreach (var item in newOrder)
                AnimeViewModel.Global.AnimeList.Add(item);

            // 4. Зберігаємо
            _ = AnimeManager.Save();
        }


        #endregion

        #region UI Items

        private void AddAnimeButton_Click(object sender, EventArgs e)
        {
            var animeData = AnimeManager.CreateAnime_Clear();

            var page = new AnimeInfo_Page(animeData);
            MainWindow.Global.GoToPage(page);
        }
        private void Notifications_Button_Click(object sender, RoutedEventArgs e)
        {
            RightPopupContent.Content = new NotificationsMenu();
        }
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
            if (SearchBox.Text == LocalizationHelper.GetTranslation("Search anime..."))
            {
                SearchBox.Text = "";
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = LocalizationHelper.GetTranslation("Search...");
            }
        }

        public void MenuMoreButton_Click(object sender, EventArgs e)
        {
            var menu = new MoreOptionsMenuControl();

            RightPopupContent.Content = menu;
        }

        #endregion
    }
}