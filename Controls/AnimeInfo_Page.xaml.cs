using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using WatchedAnimeList.Helpers;
using WatchedAnimeList.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WatchedAnimeList.Controls
{
    /// <summary>
    /// Interaction logic for AnimeInfo_Page.xaml
    /// </summary>
    public partial class AnimeInfo_Page : UserControl, IDisposable, IPage
    {
        public string PageName => "AnimeInfo_Page";

        private readonly WachedAnimeData animeData = null!;
        private bool _disposed = false;
        private bool _isEditingOriginalAnimeName = false;
        private bool IsEditingOriginalAnimeName
        {
            get => _isEditingOriginalAnimeName;
            set
            {
                _isEditingOriginalAnimeName = value;
                UpdateOriginalAnimeNameVisibility(value);
            }
        }

        private bool IsSaved = false;
        public AnimeInfo_Page(WachedAnimeData _animeData)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(_animeData.OriginalName))
                AnimeManager.ContainsAnime(_animeData.OriginalName);

            LocaliseTextSetup();

            animeData = _animeData;
            _ = TextSetup();
            ImageSetup();

            this.KeyDown += AnimeInfo_Page_KeyDown;

            OriginalAnimeName.ToolTip = animeData.OriginalName;
        }
        private void LocaliseTextSetup()
        {
            BackToMain_Text.Text = LocalizationHelper.GetTranslation("BackToMain_Text");
            AddToBookmarks_Text.Text = LocalizationHelper.GetTranslation("AddToBookmarks_Text");

            AnimeNameHint_Text.Text = LocalizationHelper.GetTranslation("AnimeNameHint_Text");
            OriginalAnimeNameHint_Text.Text = LocalizationHelper.GetTranslation("OriginalAnimeNameHint_Text");
            AnimeReliaseDateHint_Text.Text = LocalizationHelper.GetTranslation("AnimeReliaseDateHint_Text");
            AnimeWatchDateHint_Text.Text = LocalizationHelper.GetTranslation("AnimeWatchDateHint_Text");
            AnimeGenresHint_Text.Text = LocalizationHelper.GetTranslation("AnimGenresHint_Text");
            AnimeWatchedEpisodesHint_Text.Text = LocalizationHelper.GetTranslation("AnimeWatchedEpisodesHint_Text");
            Sync_Button_Text.Text = LocalizationHelper.GetTranslation("Sync_Button_Text");
            Sync_Button_ToolTip_Text.Text = LocalizationHelper.GetTranslation("Sync_Button_ToolTip_Text");
            Save_Button_Text.Text = LocalizationHelper.GetTranslation("Save_Button_Text");
            Save_Button_ToolTip_Text.Text = LocalizationHelper.GetTranslation("Save_Button_ToolTip_Text");
        }

        private async Task TextSetup()
        {
            if (animeData is null)
                Debug.Ex("animeData is null", NotificationType.Info);

            if (!string.IsNullOrEmpty(animeData.AnimeName))
            {
                AnimeName_Text.Text = animeData.AnimeName;
                OriginalAnimeName_Text.Text = animeData.OriginalName;
            }
            else if (!string.IsNullOrEmpty(animeData.OriginalName))
            {
                animeData.AnimeName = await LocalizationHelper.TranslateText(animeData.OriginalName);
                AnimeName_Text.Text = animeData.AnimeName;
                OriginalAnimeName_Text.Text = animeData.OriginalName;
            }
            else
            {
                AnimeName_Text.Text = "N/A";
                OriginalAnimeName_Text.Text = "N/A";
            }

            if (animeData.ReliaseDate is null)
            {
                Debug.Log("animeData.ReliaseDate is null", NotificationType.Info);
                AnimeReliaseDate_Text.Text = "N/A";
            }
            else
                AnimeReliaseDate_Text.Text = animeData.ReliaseDate;

            if (animeData.WatchedDate is null)
            {
                Debug.Log("animeData.WatchedDate is null", NotificationType.Info);
                AnimeWatchDate_Text.Text = "N/A";
            }
            else
                AnimeWatchDate_Text.Text = animeData.WatchedDate;

            if (animeData.Genres is null)
            {
                Debug.Log("animeData.Genres is null", NotificationType.Info);
                AnimeGenres_Text.Text = "N/A";
            }
            else
                AnimeGenres_Text.Text = animeData.Genres;

            if (animeData.WatchedEpisodesSet == null || animeData.WatchedEpisodesSet.Count == 0)
            {
                Debug.Log("animeData.WatchedEpisodesSet is null or empty", NotificationType.Info);
                AnimeWatchedEpisodes_Text.Text = "N/A";
            }
            else
                AnimeWatchedEpisodes_Text.Text = string.Join(",", animeData.WatchedEpisodesSet);
        }

        private void ImageSetup()
        {
            if (animeData is null)
                Debug.Ex("animeData is null");

            if (animeData.AnimeImage is null)
                AnimePoster_Image.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/def_poster.jpg"));
            else
                AnimePoster_Image.Source = animeData.AnimeImage;
        }
        #region UIelements

        private void BackToMain_Button_Click(object sender, EventArgs e)
        {
            PagesHelper.GoToMainPage();
        }
        private void AddToBookmarks_Button_Click(object sender, EventArgs e)
        {
            Debug.Show("WIP");
        }
        #endregion

        #region KeyboardShortcuts
        private void AnimeInfo_Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && IsEditingOriginalAnimeName)
            {
                animeData.OriginalName = OriginalAnimeName_Edit_Text.Text;
                OriginalAnimeName_Text.Text = animeData.OriginalName;
                IsEditingOriginalAnimeName = false;
                Keyboard.Focus(null);
                OriginalAnimeName.ToolTip = animeData.OriginalName;
            }
            else if (e.Key == Key.Escape && IsEditingOriginalAnimeName)
            {
                IsEditingOriginalAnimeName = false;
            }
        }

        #endregion

        #region UI Elements
        #region Logic

        private void UpdateOriginalAnimeNameVisibility(bool isEdit)
        {
            OriginalAnimeName_Text.Visibility = isEdit ? Visibility.Collapsed : Visibility.Visible;
            OriginalAnimeName_Edit_Text.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;

            if (isEdit)
            {
                OriginalAnimeName_Edit_Text.Text = OriginalAnimeName_Text.Text;
                Keyboard.Focus(OriginalAnimeName_Edit_Text);
                OriginalAnimeName_Edit_Text.CaretIndex = OriginalAnimeName_Edit_Text.Text.Length;
                OriginalAnimeName_Edit_Text.CaretBrush = Brushes.White;
            }
        }

        #endregion

        #region Buttons
        private void EditOriginalName_Button_Click(object sender, EventArgs e)
        {
            IsEditingOriginalAnimeName = !IsEditingOriginalAnimeName;
        }
        private void CopyName_Button_Click(object sender, EventArgs e)
        {
            if (animeData is null || animeData.AnimeName is null)
                Debug.Ex("animeData or animeData.AnimeName is null");
            Clipboard.SetText(animeData.AnimeName);
        }
        private void CopyOriginalName_Button_Click(object sender, EventArgs e)
        {
            if (animeData is null || animeData.OriginalName is null)
                Debug.Ex("animeData or animeData.OriginalName is null");
            Clipboard.SetText(animeData.OriginalName);
        }
        private void CopyReliaseDate_Button_Click(object sender, EventArgs e)
        {
            if (animeData is null || animeData.ReliaseDate is null)
                Debug.Ex("animeData or animeData.ReliaseDate is null");
            Clipboard.SetText(animeData.ReliaseDate);
        }
        private void CopyWatchDate_Button_Click(object sender, EventArgs e)
        {
            if (animeData is null || animeData.WatchedDate is null)
                Debug.Ex("animeData or animeData.WatchedDate is null");
            Clipboard.SetText(animeData.WatchedDate);
        }
        private void CopyGenres_Button_Click(object sender, EventArgs e)
        {
            if (animeData is null || animeData.Genres is null)
                Debug.Ex("animeData or animeData.Genres is null");
            Clipboard.SetText(animeData.Genres);
        }
        private void CopyWatchedEpisodes_Button_Click(object sender, EventArgs e)
        {
            if (animeData?.WatchedEpisodesSet == null || animeData.WatchedEpisodesSet.Count == 0)
                Debug.Ex("animeData або WatchedEpisodesSet пусті");

            string text = string.Join(",", animeData.WatchedEpisodesSet);
            Clipboard.SetText(text);
        }
        private async void Sync_Button_Click(object sender, EventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            await NetworkHelper.WaitForInternetAsync();

            if (animeData is null || animeData.OriginalName is null)
                Debug.Ex("animeData or animeData.OriginalName is null");

            var anime = await JikanHelper.GetAnime(animeData.OriginalName);
            if (anime is null)
                Debug.Ex("Anime is null");

            var _ReliaseDate = $"{anime.Aired.From:dd.MM.yyyy} - {anime.Aired.To:dd.MM.yyyy}";
            animeData.ReliaseDate = _ReliaseDate;

            animeData.Genres = string.Join(", ", anime.Genres.Select(g => g.Name));

            if(animeData.AnimeImage is null)
            {
                var animePoster = await AnimePostersLoader.DownloadImageAsync(anime.Images.JPG.ImageUrl);
                if (animePoster is null)
                    Debug.Ex("animePoster is null");
                animeData.AnimeImage = animePoster;
                
                ImageSetup();
            }

            _ = TextSetup();
            Mouse.OverrideCursor = null;
        }
        private void Save_Button_Click(object sender, EventArgs e)
        {
            if (!IsSaved && !string.IsNullOrEmpty(animeData.OriginalName))
            {
                IsSaved = true;

                AnimeManager.AddAnime(animeData);
            }
        }

        #endregion
        #endregion


        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                this.KeyDown -= AnimeInfo_Page_KeyDown;
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
