using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WatchedAnimeList.Models;

namespace WatchedAnimeList.Helpers
{
    public class WachedAnimeSaveLoad
    {
        public static WachedAnimeSaveLoad Global;
        private string folderPath;
        public readonly Dictionary<string, WachedAnimeData> wachedAnimeDict = new();

        public void Initialize()
        {
            if (Global != null && Global != this)
                return;

            Global = this;

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            folderPath = Path.Combine(documentsPath, "RE ZERO", "WachedAnimeList");
            Directory.CreateDirectory(folderPath);
            Directory.CreateDirectory(Path.Combine(folderPath, "Anime Icons"));

            Load();
        }

        public WachedAnimeData GetAnimeByName(string name)
        {
            return wachedAnimeDict.TryGetValue(name, out var animeData) ? animeData : null;
        }

        public void AddAnime(WachedAnimeData animeData)
        {
            if (animeData == null || string.IsNullOrEmpty(animeData.AnimeNameEN)) return;
            wachedAnimeDict[animeData.AnimeNameEN] = animeData;
        }

        public bool RemoveAnimeByName(string name) => wachedAnimeDict.Remove(name);

        public async void Save()
        {
            if (folderPath == null) Initialize();
            if (wachedAnimeDict.Count == 0) return;

            string jsonPath = Path.Combine(folderPath, "anime_data.json");

            var data = new WachedAnimeSaveDataCollection
            {
                dataCollection = wachedAnimeDict.Values.ToArray()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(jsonPath, json);

            foreach (var item in wachedAnimeDict.Values)
            {
                string finalPath = Path.Combine(folderPath, "Anime Icons", GetSafeImageFileName(item.AnimeName));
                if (item.AnimeImage != null && !File.Exists(finalPath))
                {
                    SaveBitmapImageToFile(item.AnimeImage, finalPath);
                }
            }

            var drive = new GoogleDriveHelper();
            await drive.InitAsync();
            await drive.UploadJsonAsync(json, "anime_data.json");
        }

        public async void Load()
        {
            WachedAnimeSaveDataCollection data = null;
            var drive = new GoogleDriveHelper();
            await drive.InitAsync();
            string jsonText = await drive.DownloadJsonAsync("anime_data.json");

            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                data = TryDeserialize(jsonText);
            }
            else
            {
                string localPath = Path.Combine(folderPath, "anime_data.json");
                if (File.Exists(localPath))
                {
                    string json = File.ReadAllText(localPath);
                    data = TryDeserialize(json);
                }
            }

            if (data == null || data.dataCollection == null)
                return;

            wachedAnimeDict.Clear();

            foreach (var item in data.dataCollection)
            {
                var animeData = new WachedAnimeData
                {
                    AnimeName = item.AnimeName,
                    AnimeNameEN = item.AnimeNameEN,
                    Rating = item.Rating
                };

                string imagePath = Path.Combine(folderPath, "Anime Icons", GetSafeImageFileName(item.AnimeName));
                if (File.Exists(imagePath))
                {
                    animeData.AnimeImage = LoadImageFromFile(imagePath);
                }

                wachedAnimeDict[animeData.AnimeNameEN] = animeData;
            }
            MainWindow.Global.UpdateList();
        }

        private WachedAnimeSaveDataCollection TryDeserialize(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<WachedAnimeSaveDataCollection>(json);
                if (data?.dataCollection != null && data.dataCollection.All(x => !string.IsNullOrWhiteSpace(x.AnimeNameEN)))
                {
                    return data;
                }
                // Старий формат
                var old = JsonSerializer.Deserialize<WachedAnimeSaveDataCollectionPrevious>(json);
                return new WachedAnimeSaveDataCollection
                {
                    dataCollection = old.dataCollection.Select(x => new WachedAnimeData
                    {
                        AnimeName = x.animeName,
                        AnimeNameEN = x.animeNameEN,
                        Rating = x.rating
                    }).ToArray()
                };
            }
            catch
            {
                return null;
            }
        }

        public static string GetSafeImageFileName(string animeTitle)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                animeTitle = animeTitle.Replace(c, '_');
            return animeTitle + ".jpg";
        }

        private static void SaveBitmapImageToFile(BitmapImage image, string filePath)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using var stream = File.Create(filePath);
            encoder.Save(stream);
        }

        private static BitmapImage LoadImageFromFile(string path)
        {
            var image = new BitmapImage();
            using var stream = File.OpenRead(path);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
