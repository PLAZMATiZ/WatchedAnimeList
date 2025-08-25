using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace WatchedAnimeList.Models
{
    public class WachedAnimeData
    {
        [JsonIgnore]
        public BitmapImage? AnimeImage { get; set; }

        public string? AnimeName { get; set; }
        public string? AnimeNameEN { get; set; }
        public string? ConnectedAnimeName { get; set; }
        public string? Genres { get; set; }
        public SortedSet<int> WatchedEpisodesSet { get; set; } = new();
        public string? ReliaseDate { get; set; }
        public string? WatchedDate { get; set; }

        public int Rating { get; set; }
    }
    public class WachedAnimeSaveDataCollection
    {
        public WachedAnimeData[] dataCollection { get; set; } = [];
    }
}
