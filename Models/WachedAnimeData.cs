using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace WatchedAnimeList.Models
{
    /// <summary>
    /// Represents a watched anime with its details, image, genres, and watched episodes.
    /// </summary>
    public class WachedAnimeData
    {
        /// <summary>
        /// The poster image of the anime (not serialized to JSON).
        /// </summary>
        [JsonIgnore]
        public BitmapImage? AnimeImage { get; set; }

        /// <summary>
        /// The localized or custom anime name.
        /// </summary>
        public string? AnimeName { get; set; }

        /// <summary>
        /// The original (English or Japanese) name of the anime.
        /// </summary>
        [JsonPropertyName("AnimeNameEN")] // so that JSON can read the old field
        public string? OriginalName { get; set; }

        /// <summary>
        /// Name of any connected anime (e.g., prequel, sequel).
        /// </summary>
        public string? ConnectedAnimeName { get; set; }

        /// <summary>
        /// Comma-separated list of genres.
        /// </summary>
        public string? Genres { get; set; }

        /// <summary>
        /// Set of watched episode numbers.
        /// </summary>
        public SortedSet<int> WatchedEpisodesSet { get; set; } = new();

        /// <summary>
        /// The release date of the anime.
        /// </summary>
        public string? ReliaseDate { get; set; }

        /// <summary>
        /// The date when the anime was watched.
        /// </summary>
        public string? WatchedDate { get; set; }

        /// <summary>
        /// User rating of the anime.
        /// </summary>
        public int Rating { get; set; }
    }

    /// <summary>
    /// Collection wrapper for saving/loading multiple watched anime entries.
    /// </summary>
    public class WachedAnimeSaveDataCollection
    {
        /// <summary>
        /// Array of watched anime data.
        /// </summary>
        public WachedAnimeData[] dataCollection { get; set; } = [];
    }
}
