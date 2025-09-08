using System.IO;

using WatchedAnimeList.Logic;

namespace WatchedAnimeList.Helpers
{
    public static class Initializer
    {
        public static void Inithialize()
        {
            Settings.Initialize();
            AnimeManager.Initialize();
            LocalizationHelper.Initialize();
            NotificationsHelper.Initialize();
        }
    }
}
