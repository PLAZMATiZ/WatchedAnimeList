using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using WatchedAnimeList.Models;
using WatchedAnimeList.ViewModels;
using Windows.Media.Protection.PlayReady;

namespace WatchedAnimeList.Helpers
{
    public static class AnimePostersLoader
    {
        public static Action<bool> IfLoadPoster = (a) => { };

        public static async Task<List<string>> LoadImagesAsync(
            ConcurrentDictionary<string, WachedAnimeData> collection,
            string folderPath,
            int batchSize = 8)
        {
            Directory.CreateDirectory(folderPath);
            var failed = new ConcurrentBag<string>();
            var httpClient = new HttpClient();

            foreach (var anime in collection.Values)
            {
                if (string.IsNullOrEmpty(anime.OriginalName))
                {
                    failed.Add("Unknown");
                    continue;
                }

                var safeFileName = GetSafeImageFileName(anime.OriginalName);
                var imagePath = Path.Combine(folderPath, safeFileName);

                if (File.Exists(imagePath))
                {
                    try
                    {
                        using var stream = File.OpenRead(imagePath);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        anime.AnimeImage = bitmap;
                    }
                    catch
                    {
                        failed.Add(anime.OriginalName);
                    }
                }
                else
                {
                    failed.Add(anime.OriginalName);
                }
            }

            if (failed.Count > 0)
            {
                IfLoadPoster.Invoke(true);
                var toDownload = failed.ToList();
                failed.Clear();

                int delayMs = 2000;

                var semaphore = new SemaphoreSlim(2);
                var tasks = new List<Task>();

                foreach (var (batch, batchIndex) in toDownload
                             .Select((name, idx) => (name, idx))
                             .GroupBy(x => x.idx / batchSize)
                             .Select((grp, i) => (grp.Select(x => x.name).ToList(), i)))
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var localTasks = new List<Task>();

                        foreach (var name in batch)
                        {
                            if (!collection.TryGetValue(name, out var animeData)) continue;

                            localTasks.Add(Task.Run(async () =>
                            {
                                await semaphore.WaitAsync();
                                try
                                {
                                    await Task.Delay(delayMs);

                                    var anime = await JikanHelper.GetAnime(name);
                                    if (anime is null)
                                    {
                                        failed.Add(name);
                                        return;
                                    }

                                    var imageUrl = anime.Images?.JPG?.ImageUrl;
                                    if (string.IsNullOrEmpty(imageUrl))
                                    {
                                        failed.Add(name);
                                        return;
                                    }

                                    var bitmap = await AnimePostersLoader.DownloadImageAsync(imageUrl);
                                    if (bitmap is null)
                                    {
                                        failed.Add(name);
                                        return;
                                    }

                                    var safeFileName = GetSafeImageFileName(name);
                                    var imagePath = Path.Combine(folderPath, safeFileName);
                                    SaveBitmap(bitmap, imagePath);

                                    await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        animeData.AnimeImage = bitmap;
                                    });
                                }
                                catch
                                {
                                    failed.Add(name);
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            }));
                        }

                        await Task.WhenAll(localTasks);
                    }));
                }

                await Task.WhenAll(tasks);
            }
            IfLoadPoster.Invoke(false);
            return failed.ToList();
        }

        public static void SaveBitmap(BitmapImage bitmap, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder(); // можна JpegBitmapEncoder
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }
        }

        public static async Task<BitmapImage?> DownloadImageAsync(string url)
        {
            var httpClient = new HttpClient();

            try
            {
                var bytes = await httpClient.GetByteArrayAsync(url);
                using var ms = new MemoryStream(bytes);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.Log($"Помилка при завантаженні: {ex.Message}", NotificationType.Warning);
                return null;
            }
        }

        public static string GetSafeImageFileName(string animeTitle)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                animeTitle = animeTitle.Replace(c, '_');
            return animeTitle + ".jpg";
        }
    }
}