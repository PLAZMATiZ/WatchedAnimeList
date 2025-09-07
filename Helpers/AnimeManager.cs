using System;
using System.IO;
using System.Windows;
using System.Collections.Concurrent;
using System.Windows.Media.Imaging;
using WatchedAnimeList.Models;
using WatchedAnimeList.ViewModels;
using System.Text.Json;
using JikanDotNet;
using System.Xml.Linq;
using WatchedAnimeList.Controls;
using WatchedAnimeList.Logic;

namespace WatchedAnimeList.Helpers
{
    public static class AnimeManager
    {
        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        private static readonly ConcurrentDictionary<string, WachedAnimeData> watchedAnimeDict = new();
        private static string AppFolderPath = AppPaths.AppDocumentsFolderPath;
        private static string AppCompabilityFolderPath = AppPaths.AppCompabilityDocumentsFolderPath;

        public static async void Initialize()
        {
            Debug.Log("Ініціалізація AnimeManager", NotificationType.Info);

            if(!Directory.Exists(Path.Combine(AppFolderPath, "Anime Icons")))
            {
                Directory.CreateDirectory(Path.Combine(AppFolderPath, "Anime Icons"));
            }
            await LoadAsync();
            Debug.Log("AnimeManager успішно ініціалізовано", NotificationType.Info);
        }
        #region Save/Load

        public static async Task Save()
        {
            if (watchedAnimeDict.IsEmpty) return;

            string? jsonPath = Path.Combine(AppFolderPath, "anime_data.json");

            var data = new WachedAnimeSaveDataCollection
            {
                dataCollection = watchedAnimeDict.Values.ToArray()
            };

            var json = JsonSerializer.Serialize(data, jsonSerializerOptions);
            File.WriteAllText(jsonPath, json);

            foreach (var item in watchedAnimeDict.Values)
            {
                if (item is null)
                    Debug.Ex("item is null");
                if(item.OriginalName is null)
                    Debug.Ex("item.OriginalName is null");

                string finalPath = Path.Combine(AppFolderPath, "Anime Icons", GetSafeImageFileName(item.OriginalName));
                if (item.AnimeImage != null && !File.Exists(finalPath))
                {
                    SaveBitmapImageToFile(item.AnimeImage, finalPath);
                }
            }

            var drive = new GoogleDriveHelper();
            await drive.InitAsync();
            await drive.UploadJsonAsync(json, "anime_data.json");
        }
        public static async Task LoadAsync()
        {
            Debug.Log("Start local LoadAsync", NotificationType.Info);
            WachedAnimeSaveDataCollection? data = null;

            try
            {
                string localPath = Path.Combine(AppFolderPath, "anime_data.json");

                if (File.Exists(localPath))
                {
                    string json = await File.ReadAllTextAsync(localPath);
                    data = TryDeserialize(json);
                }
                else if (File.Exists(Path.Combine(AppCompabilityFolderPath, "anime_data.json")))
                {
                    localPath = Path.Combine(AppCompabilityFolderPath, "anime_data.json");

                    string json = await File.ReadAllTextAsync(localPath);
                    data = TryDeserialize(json);
                }

                if (data is null)
                    Debug.Ex("data is null");
                if (data.dataCollection is null)
                    Debug.Ex("data.dataCollection is null");


                watchedAnimeDict.Clear();
                ProcessAnimeCollectionParallel(data.dataCollection);

                var failed = await AnimePostersLoader.LoadImagesAsync(
                    watchedAnimeDict,
                    Path.Combine(AppFolderPath, "Anime Icons")
                );

                if (failed.Count > 0)
                    Debug.Log($"FailedLoadIcon = {failed.Count}");
            }
            catch 
            {
                Debug.Log("Local load failed", NotificationType.Warning);
            }
            // Google Drive
            try
            {
                Debug.Log("Start Google Drive LoadAsync", NotificationType.Info);
                var drive = new GoogleDriveHelper();
                await drive.InitAsync();

                string? jsonText = await drive.DownloadJsonAsync("anime_data.json");
                if (jsonText is null)
                    Debug.Ex("jsonText is null");

                if (!string.IsNullOrWhiteSpace(jsonText))
                {
                    data = TryDeserialize(jsonText) ?? new WachedAnimeSaveDataCollection();
                    ProcessAnimeCollectionParallel(data.dataCollection, skipExisting: true);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Google Drive load failed: {ex.Message}", NotificationType.Warning);
            }
        }

        private static void ProcessAnimeCollectionParallel(IEnumerable<WachedAnimeData> collection, bool skipExisting = false)
        {
            var addedItems = new ConcurrentBag<WachedAnimeData>();

            Parallel.ForEach(collection, new ParallelOptions { MaxDegreeOfParallelism = 4 }, animeData =>
            {
                if (animeData.OriginalName is null)
                    Debug.Ex("animeData.OriginalName is null");

                if (skipExisting && watchedAnimeDict.ContainsKey(animeData.OriginalName))
                    return;

                watchedAnimeDict[animeData.OriginalName] = animeData;
                addedItems.Add(animeData);
            });

            Application.Current.Dispatcher.Invoke(() =>
            {
                var onCardClick = new Action<string>(name =>
                {
                    OnAnimeCardClicked(name);
                });

                AnimeViewModel.Global.AnimeList.AddRange(
                    addedItems.Select(d => new AnimeItemViewModel(d, onCardClick))
                );
            });
        }

        private static WachedAnimeSaveDataCollection? TryDeserialize(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<WachedAnimeSaveDataCollection>(json);
                if (data?.dataCollection != null && data.dataCollection.All(x => !string.IsNullOrWhiteSpace(x.OriginalName)))
                {
                    return data;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static void SaveBitmapImageToFile(BitmapImage image, string filePath)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using var stream = File.Create(filePath);
            encoder.Save(stream);
        }
        public static string GetSafeImageFileName(string animeTitle)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                animeTitle = animeTitle.Replace(c, '_');
            return animeTitle + ".jpg";
        }
        #endregion

        #region temp
        
        public static void AddAnime(WachedAnimeData animeData)
        {
            if (animeData == null || string.IsNullOrEmpty(animeData.OriginalName)) return;
            watchedAnimeDict[animeData.OriginalName] = animeData;
        }
        public static bool RemoveAnimeByName(string name)
        {
            if (!watchedAnimeDict.ContainsKey(name))
                return false;
            return watchedAnimeDict.Remove(name, out _);
        }
        public static WachedAnimeData? GetAnimeByName(string name)
        {
            watchedAnimeDict.TryGetValue(name, out var data);
            return data;
        }

        public static bool ContainsAnime(string animeName)
        {
            return watchedAnimeDict.ContainsKey(animeName);
        }
        public static ConcurrentDictionary<string, WachedAnimeData> GetWatchedAnimeDict()
        {
            return watchedAnimeDict;
        }
        public static bool TryGetWachedAnimeData(string animeName, out WachedAnimeData? animeData)
        {
            var succes = watchedAnimeDict.TryGetValue(animeName, out animeData);
            return succes;
        }

        // test
        public static void AddEpisode(string titleName, int episode)
        {
            if (!watchedAnimeDict.TryGetValue(titleName, out var animeData))
            {
                animeData = CreateAnime_Name(titleName);
                AddAnime(animeData);

                var notification = NotificationsHelper.CreateNotification(
                    "Переглянуто нове аніме",
                    OnClickAction.OpenAnimeInfo,
                    titleName);

                NotificationsHelper.AddNotification(notification);
            }

            if (animeData.WatchedEpisodesSet == null)
                animeData.WatchedEpisodesSet = new SortedSet<int>();

            animeData.WatchedEpisodesSet.Add(episode);

            Debug.Log($"Епізод {episode} додано до переглянутих");
            _ = Save();
        }
        
        #endregion

        public static WachedAnimeData CreateAnime_Clear()
        {
            var wachedAnimeData = new WachedAnimeData();
            wachedAnimeData.WatchedDate = DateTime.Now.ToString();
            return wachedAnimeData;
        }
        public static WachedAnimeData CreateAnime_Name(string name)
        {
            var wachedAnimeData = new WachedAnimeData();
            wachedAnimeData.OriginalName = name;
            wachedAnimeData.WatchedDate = DateTime.Now.ToString();

            return wachedAnimeData;
        }

        public static async Task<Anime?> GetAnimeTitle_FromJikanAPI(string name)
        {
            var jikan = new Jikan();
            try
            {
                Debug.Log($"Пошук аніме (JikanAPI)", NotificationType.Info);
                var searchResult = await jikan.SearchAnimeAsync(name);
                var filtered = searchResult.Data.Where(a => a.Type != "Music").ToList();

                if (filtered?.Count > 0)
                {
                    var first = filtered.First();
                    return (filtered.First());
                }
                else
                {
                    Debug.Log($"Не вдалося знайти аніме (JikanAPI)", NotificationType.Warning);
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message, NotificationType.Error);
            }
            return null;
        }

        #region Other

        public static WachedAnimeData? GetAnimeDataByName(string name)
        {
            watchedAnimeDict.TryGetValue(name, out var data);
            return data;
        }
        
        public static void OnAnimeCardClicked(string OriginalName)
        {
            var animeData = GetAnimeByName(OriginalName);
            if (animeData is null)
                Debug.Ex($"animeData is null | {OriginalName} |");

            var page = new AnimeInfo_Page(animeData);
            PagesHelper.GoToPage(page);
        }
        #endregion

        #region Supply metods
        private static BitmapImage LoadImageFromBytes(byte[] imageData)
        {
            using var ms = new MemoryStream(imageData);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }
        #endregion
    }
}
