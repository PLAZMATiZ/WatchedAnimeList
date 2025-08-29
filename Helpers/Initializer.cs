using System.IO;

using WatchedAnimeList.Logic;

namespace WatchedAnimeList.Helpers
{
    public static class Initializer
    {
        public static void Inithialize()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folderPath = Path.Combine(documentsPath, "RE ZERO", "WachedAnimeList");

            Settings.Initialize();
            AnimeManager.Initialize(folderPath);
            LocalizationHelper.Initialize();
            NotificationsHelper.Initialize(folderPath);
        }
    }
}
