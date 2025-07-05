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

namespace WatchedAnimeList.Helpers
{
    public static class AnimePostersLoader
    {
        public static async Task<List<string>> LoadImagesAsync(ObservableCollection<AnimeItemViewModel> targetCollection, string folderPath)
        {
            var semaphore = new SemaphoreSlim(4);
            var failed = new List<string>();
            var tasks = new List<Task>();

            foreach (var vm in targetCollection.ToList())
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var fileName = GetSafeImageFileName(vm.AnimeNameEN);
                        var fullPath = Path.Combine(folderPath, fileName);

                        BitmapImage? bitmap = null;
                        if (File.Exists(fullPath))
                        {
                            using var stream = File.OpenRead(fullPath);
                            bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                            bitmap.Freeze();
                        }
                        else
                        {
                            lock (failed)
                                failed.Add(vm.AnimeNameEN);
                        }

                        if (bitmap != null)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                vm.AnimeImage = bitmap;
                            });
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            if(failed.Count != 0)
            {
                var f = await DownLoadImagesAsync(targetCollection, folderPath, failed);
                Debug.Log($"Succesfully load {f.Count - failed.Count} / {failed.Count}");
                return f;
            }
            return failed;
        }
        public static async Task<List<string>> DownLoadImagesAsync(ObservableCollection<AnimeItemViewModel> targetCollection, string folderPath, List<string> failed)
        {
            var semaphore = new SemaphoreSlim(4);
            var tasks = new List<Task>();
            var httpClient = new HttpClient();

            Directory.CreateDirectory(folderPath);

            // Фільтруємо лише ті, що в списку failed
            var toDownload = targetCollection.Where(vm => failed.Contains(vm.AnimeNameEN)).ToList();

            // Очищаємо failed перед новою спробою
            failed.Clear();

            foreach (var vm in toDownload)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        try
                        {
                            var encodedName = Uri.EscapeDataString(vm.AnimeNameEN);
                            var url = $"https://api.jikan.moe/v4/anime?q={encodedName}&limit=1";
                            var response = await httpClient.GetAsync(url);

                            if (!response.IsSuccessStatusCode)
                            {
                                lock (failed) failed.Add(vm.AnimeNameEN);
                                return;
                            }

                            var json = await response.Content.ReadAsStringAsync();
                            var jObj = JObject.Parse(json);
                            var imageUrl = jObj["data"]?[0]?["images"]?["jpg"]?["image_url"]?.ToString();

                            if (string.IsNullOrEmpty(imageUrl))
                            {
                                lock (failed) failed.Add(vm.AnimeNameEN);
                                return;
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

                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                vm.AnimeImage = bitmap;
                            });
                        }
                        catch
                        {
                            lock (failed) failed.Add(vm.AnimeNameEN);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return failed;
        }


        public static async Task<List<string>> LoadImagesAsync(ObservableCollection<WachedAnimeData> targetCollection, string folderPath)
        {
            var semaphore = new SemaphoreSlim(4);
            var tempList = new List<WachedAnimeData>();
            var failedLoadings = new ConcurrentBag<string>();
            var tasks = new List<Task>();

            foreach (var original in targetCollection.ToList())
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var fileName = GetSafeImageFileName(original.AnimeNameEN);
                        var fullPath = Path.Combine(folderPath, fileName);

                        BitmapImage? bitmap = null;
                        if (File.Exists(fullPath))
                        {
                            using var stream = File.OpenRead(fullPath);
                            bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                            bitmap.Freeze();
                        }
                        else
                        {
                            // Якщо файл не знайдено, додаємо назву в failedLoadings
                            failedLoadings.Add(original.AnimeNameEN);
                        }

                        var updated = new WachedAnimeData
                        {
                            AnimeName = original.AnimeName,
                            AnimeNameEN = original.AnimeNameEN,
                            Rating = original.Rating,
                            ConnectedAnimeName = original.ConnectedAnimeName,
                            Genre = original.Genre,
                            AnimeImage = bitmap
                        };

                        lock (tempList)
                        {
                            tempList.Add(updated);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                targetCollection.Clear();
                foreach (var item in tempList)
                {
                    targetCollection.Add(item);
                }
            });

            return failedLoadings.ToList();
        }

        public static string GetSafeImageFileName(string animeTitle)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                animeTitle = animeTitle.Replace(c, '_');
            return animeTitle + ".jpg";
        }
    }
}
