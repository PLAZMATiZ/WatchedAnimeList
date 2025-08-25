using System.Threading.Tasks;
using System.Windows.Controls;
using WatchedAnimeList.Helpers;
using WatchedAnimeList.Models;

namespace WatchedAnimeList.Controls
{
    /// <summary>
    /// Interaction logic for AnimeInfo_Page.xaml
    /// </summary>
    public partial class AnimeInfo_Page : UserControl
    {
        private readonly WachedAnimeData animeData = null!;
        public AnimeInfo_Page(WachedAnimeData _animeData)
        {
            InitializeComponent();
            LocaliseTextSetup();

            animeData = _animeData;
            _ = TextSetup();
            ImageSetup();
        }
        private void LocaliseTextSetup()
        {
            Debug.Log("LocaliseTextSetup |Start|", NotificationType.Info);

            BackToMain_Text.Text = LocalizationHelper.GetTranslation("BackToMain_Text");
            AddToBookmarks_Text.Text = LocalizationHelper.GetTranslation("AddToBookmarks_Text");

            AnimeNameHint_Text.Text = LocalizationHelper.GetTranslation("AnimeNameHint_Text");
            OriginalAnimeNameHint_Text.Text = LocalizationHelper.GetTranslation("OriginalAnimeNameHint_Text");
            AnimReliaseDateHint_Text.Text = LocalizationHelper.GetTranslation("AnimeReliaseDateHint_Text");
            AnimWatchDateHint_Text.Text = LocalizationHelper.GetTranslation("AnimeWatchDateHint_Text");
            AnimGenresHint_Text.Text = LocalizationHelper.GetTranslation("AnimGenresHint_Text");
            AnimeWatchedEpisodesHint_Text.Text = LocalizationHelper.GetTranslation("AnimeWatchedEpisodesHint_Text");

            Debug.Log("LocaliseTextSetup |End|", NotificationType.Info);
        }

        private async Task TextSetup()
        {
            Debug.Log("TextSetup |Start|", NotificationType.Info);

            if (animeData.AnimeName is null)
                Debug.Ex("animeData.AnimeName is null", NotificationType.Info);

            if (!string.IsNullOrEmpty(animeData.AnimeName))
            {
                AnimeName_Text.Text = animeData.AnimeName;
                OriginalAnimeName_Text.Text = animeData.AnimeNameEN;
            }
            else if (!string.IsNullOrEmpty(animeData.AnimeNameEN))
            {
                AnimeName_Text.Text = await LocalizationHelper.TranslateText(animeData.AnimeNameEN);
                OriginalAnimeName_Text.Text = animeData.AnimeNameEN;
            }
            else
            {
                AnimeName_Text.Text = "N/A";
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

            Debug.Log("TextSetup |End|", NotificationType.Info);
        }

        private void ImageSetup()
        {
            Debug.Log("ImageSetup |Start|", NotificationType.Info);

            if (animeData is null)
                Debug.Ex("animeData is null");

            if (animeData.AnimeImage is null)
                Debug.Log("animeData.AnimeImage is null", NotificationType.Warning);
            else
                AnimePoster_Image.Source = animeData.AnimeImage;

            Debug.Log("ImageSetup |End|", NotificationType.Info);
        }
        #region UIelements

        private void BackToMain_Button_Click(object sender, EventArgs e)
        {
            MainWindow.Global.MainPage();
        }
        private void AddToBookmarks_Button_Click(object sender, EventArgs e)
        {

        }
        #endregion

        public void Dispose()
        {

        }
    }
}
