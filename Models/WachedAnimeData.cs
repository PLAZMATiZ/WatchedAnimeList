using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace WatchedAnimeList.Models
{
    public class WachedAnimeData
    {
        [JsonIgnore]
        public BitmapImage AnimeImage { get; set; }

        public string AnimeName { get; set; }
        public string AnimeNameEN { get; set; }
        public string ConnectedAnimeName { get; set; }
        public string Genre { get; set; }
        public string WatchedEpisodes { get; set; }

        public int Rating { get; set; }
    }
    public class WachedAnimeSaveDataCollection
    {
        public WachedAnimeData[] dataCollection { get; set; }
    }
}
