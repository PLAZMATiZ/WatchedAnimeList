using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WatchedAnimeList.Controls;
using WatchedAnimeList.Logic;
using WatchedAnimeList.Models;
using WatchedAnimeList.ViewModels;

namespace WatchedAnimeList.Helpers
{
    public static class Initializer
    {
        public static void Inithialize()
        {
            new WachedAnimeSaveLoad().Initialize();
            new SiteParser().Initialize();
            new Settings();
        }
        
    }
}
