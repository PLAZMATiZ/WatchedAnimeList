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
