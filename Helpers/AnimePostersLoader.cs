using System;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WatchedAnimeList.Models;
using System.Collections.Concurrent;
using WatchedAnimeList.ViewModels;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Security.Policy;

namespace WatchedAnimeList.Helpers
{
    public static class AnimePostersLoader
    {
        public static async Task<List<string>> LoadImagesAsync(
            ConcurrentDictionary<string, WachedAnimeData> collection,
            string folderPath,
            int batchSize = 60)
        {
            Directory.CreateDirectory(folderPath);
            var failed = new ConcurrentBag<string>();
            var httpClient = new HttpClient();

            // 1️⃣ Локальний кеш
            foreach (var anime in collection.Values)
            {
                if (string.IsNullOrEmpty(anime.AnimeNameEN))
                {
                    failed.Add("Unknown");
                    continue;
                }

                var safeFileName = GetSafeImageFileName(anime.AnimeNameEN);
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
                        failed.Add(anime.AnimeNameEN);
                    }
                }
                else
                {
                    failed.Add(anime.AnimeNameEN);
                }
            }

            // 2️⃣ Батчеве завантаження з Jikan API
            var toDownload = failed.ToList();
            failed.Clear();

            int delayMs = 2000;
            int maxDelay = 10000;

            for (int i = 0; i < toDownload.Count; i += batchSize)
            {
                var batch = toDownload.Skip(i).Take(batchSize).ToList();
                var tasks = new List<Task>();

                foreach (var name in batch)
                {
                    if (!collection.TryGetValue(name, out var anime)) continue;

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(delayMs);
                            var encodedName = Uri.EscapeDataString(name);
                            var url = $"https://api.jikan.moe/v4/anime?q={encodedName}&limit=1";

                            var response = await httpClient.GetAsync(url);

                            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                            {
                                delayMs = Math.Min(delayMs + 1000, maxDelay);
                                failed.Add(name);
                                return;
                            }

                            if (!response.IsSuccessStatusCode)
                            {
                                failed.Add(name);
                                return;
                            }

                            var json = await response.Content.ReadAsStringAsync();
                            var jObj = JObject.Parse(json);
                            var imageUrl = jObj["data"]?[0]?["images"]?["jpg"]?["image_url"]?.ToString();

                            if (string.IsNullOrEmpty(imageUrl))
                            {
                                failed.Add(name);
                                return;
                            }

                            var imageData = await httpClient.GetByteArrayAsync(imageUrl);
                            var safeFileName = GetSafeImageFileName(name);
                            var imagePath = Path.Combine(folderPath, safeFileName);
                            await File.WriteAllBytesAsync(imagePath, imageData);

                            using var memStream = new MemoryStream(imageData);
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = memStream;
                            bitmap.EndInit();
                            bitmap.Freeze();

                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                anime.AnimeImage = bitmap;
                            });

                            delayMs = Math.Max(500, delayMs - 200); // зменш delay якщо успішно
                        }
                        catch
                        {
                            failed.Add(name);
                        }
                    }));
                }

                await Task.WhenAll(tasks);
            }

            return failed.ToList();
        }


        public static async Task<List<string>> DownLoadImagesAsync(ObservableCollection<AnimeItemViewModel> targetCollection, string folderPath, List<string> failed)
        {
            var httpClient = new HttpClient();
            Directory.CreateDirectory(folderPath);

            foreach (var vm in targetCollection.Where(x => x.AnimeNameEN == null))
            {
                Console.WriteLine($"[WARN] Null AnimeNameEN у {vm}");
            }
            var toDownload = targetCollection
                .Where(vm => vm.AnimeNameEN != null && failed.Contains(vm.AnimeNameEN))
                .ToList();

            failed.Clear();

            int delayMs = 2000; // стартова затримка
            int maxDelay = 10000;

            foreach (var vm in toDownload)
            {
                if (vm.AnimeNameEN is null)
                    Debug.Ex("vm.AnimeNameEN is null");

                var encodedName = Uri.EscapeDataString(vm.AnimeNameEN);
                var url = $"https://api.jikan.moe/v4/anime?q={encodedName}&limit=1";

                try
                {
                    await Task.Delay(delayMs);
                    var response = await httpClient.GetAsync(url);

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Debug.Log($"[RATE LIMIT] {vm.AnimeNameEN} | Increasing delay to {delayMs + 1000}ms");
                        delayMs = Math.Min(delayMs + 1000, maxDelay);
                        failed.Add(vm.AnimeNameEN);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.Log($"[FAIL] {vm.AnimeNameEN} | Status: {response.StatusCode} | URL: {url}");
                        failed.Add(vm.AnimeNameEN);
                        continue;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var jObj = JObject.Parse(json);
                    var imageUrl = jObj["data"]?[0]?["images"]?["jpg"]?["image_url"]?.ToString();

                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        Debug.Log($"[NO IMAGE] {vm.AnimeNameEN} | URL: {url}");
                        failed.Add(vm.AnimeNameEN);
                        continue;
                    }

                    var imageData = await httpClient.GetByteArrayAsync(imageUrl);
                    var safeFileName = GetSafeImageFileName(vm.AnimeNameEN);
                    var imagePath = Path.Combine(folderPath, safeFileName);
                    await File.WriteAllBytesAsync(imagePath, imageData);

                    using var stream = new MemoryStream(imageData);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        vm.AnimeImage = bitmap;
                    });

                    // зменш delay, якщо все ок
                    delayMs = Math.Max(500, delayMs - 200);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[EXCEPTION] {vm.AnimeNameEN} | {ex.Message} | URL: {url}");
                    failed.Add(vm.AnimeNameEN);
                }
            }

            return failed;
        }

        public static string GetSafeImageFileName(string animeTitle)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                animeTitle = animeTitle.Replace(c, '_');
            return animeTitle + ".jpg";
        }
    }
}
