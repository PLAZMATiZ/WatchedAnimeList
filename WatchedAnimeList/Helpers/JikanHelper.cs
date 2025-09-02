using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JikanDotNet;

namespace WatchedAnimeList.Helpers
{
    public static class JikanHelper
    {
        public static async Task<Anime?> GetAnime(string animeNameEN)
        {
            var jikan = new Jikan();

            try
            {
                var searchResult = await jikan.SearchAnimeAsync(animeNameEN);
                var filtered = searchResult.Data.Where(a => a.Type != "Music").ToList();

                if (filtered?.Count > 0)
                {
                    var first = filtered.First();
                    return (filtered.First());
                }
                else
                {
                    Console.WriteLine("Аніме не знайдено.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Помилка: " + ex.Message);
            }
            return null;
        }
    }
}
