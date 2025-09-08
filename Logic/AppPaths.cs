using System.IO;

namespace WatchedAnimeList.Logic
{
    public static class AppPaths
    {
        private readonly static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private readonly static string documentsFolderPath = Path.Combine(documentsPath, "re_Birth", "WatchedAnimeList");
        private readonly static string documentsFolderPath_Compability = Path.Combine(documentsPath, "RE ZERO", "WachedAnimeList");

        public static string AppDocumentsFolderPath
        {
            get
            {
                if (!Directory.Exists(documentsFolderPath))
                {
                    Directory.CreateDirectory(documentsFolderPath);
                }
                return documentsFolderPath;
            }
        }

        public static string AppCompabilityDocumentsFolderPath
        {
            get
            {
                return documentsFolderPath_Compability;
            }
        }

        public static void Initialize()
        {

        }

    }
}
