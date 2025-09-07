using MonoTorrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchedAnimeList.Models
{
    public class DownloadConfig
    {
        public List<int> SelectedEpisodes { get; set; } = new();
        public required string MagnetLinkHashes { get; set; }
    }
}
