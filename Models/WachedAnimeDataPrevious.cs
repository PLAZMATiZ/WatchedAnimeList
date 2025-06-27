using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace WatchedAnimeList.Models
{
    public class WachedAnimeDataPrevious
    {
        [JsonIgnore]
        public BitmapImage AnimeImage;

        public string animeName { get; set; }
        public string animeNameEN { get; set; }
        public string connectedAnimeName { get; set; }

        public int rating { get; set; }
    }
    public class WachedAnimeSaveDataCollectionPrevious
    {
        public WachedAnimeDataPrevious[] dataCollection { get; set; }
    }
}