using System.Net.Http;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace WatchedAnimeList.Helpers
{
    public static class SiteParser
    {
        public static bool UrlValidate(string url)
        {
            bool isValid = Uri.TryCreate(url, UriKind.Absolute, out Uri? uri);
            return isValid;
        }
        public static async Task SiteParse(string url, Action<string, string> callback)
        {
            string title = "";
            string date = "";

            var source = GetAnimeSource(url);
            if (source == null)
                return;
            GetSourceContainers(source, out string[]? nameContainers, out string[]? dateContainers);
            if (nameContainers == null || dateContainers == null)
                return;

            try
            {
                (title, date) = await ParseAnimeInfoAsync(url, nameContainers, dateContainers);
            }
            catch (Exception ex)
            {
                Debug.Show($"Помилка при парсингу: {ex.Message}");
            }

            callback(title, date);
        }

        private static string? GetAnimeSource(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return null;

            string host = uri.Host.ToLowerInvariant();

            if (host.Contains("anilibria.tv"))
                return "anilibria.tv";
            else if (host.Contains("anilibria.wtf"))
                return "anilibria.wtf";
            else if (host.Contains("anilibria.top"))
                return "anilibria.top";
            else if (host.Contains("jut-su.net"))
                return "jut-su.net";
            else if (host.Contains("yummyanime.tv"))
                return "yummyanime.tv";

            return null;
        }
        private static void GetSourceContainers(string source, out string[]? name, out string[]? reliaseDate)
        {
            switch (source)
            {
                case "anilibria.tv":
                    name = new string[] { "//h1[contains(@class,'release-title')]"};
                    reliaseDate = new string[] {"//a[contains(@class,'release-season')]" };
                break;
                
                case "anilibria.wtf":
                    name = new string[] { "//div[contains(@class, 'text-autosize') and contains(@class, 'ff-heading')]", "//div[contains(@class, 'fz-70') and contains(@class, 'ff-heading')]" };
                    reliaseDate = new string[] { "//div[contains(@class, 'text-truncate')]" };
                break;

                case "anilibria.top":
                    name = new string[] { "//div[contains(@class, 'text-autosize ff-heading lh-110 font-weight-bold mb-1')]", "//div[contains(@class, 'fz-70') and contains(@class, 'ff-heading')]" };
                    reliaseDate = new string[] { "//div[contains(@class, 'text-truncate')]" };
                break;

                case "jut-su.net":
                    name = new string[] { "//div[contains(@class, 'jutsu-page__title-text cd-flex')]/h1", "//div[contains(@class, 'jutsu-page__original')]" };
                    reliaseDate = new string[] {};
                break;
                case "yummyanime.tv":
                    name = new string[] { "//div[contains(@class, 'inner-page__subtitle')]", "//div[contains(@class, 'inner-page__subtitle')]" };
                    reliaseDate = new string[] { };
                    break;
                default:
                    name = null;
                    reliaseDate = null;
                break;
            }
        }
        private static async Task<(string Title, string Date)> ParseAnimeInfoAsync(string url, string[] NameContainers, string[] DateContainers)
        {
            url = url.Trim();
            var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/122.0.0.0 Safari/537.36");
            http.DefaultRequestHeaders.Accept.ParseAdd("text/html");
            http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            string title = "";
            foreach (var container in NameContainers)
            {
                var titleNode = doc.DocumentNode.SelectSingleNode(container);
                if (titleNode == null)
                    Debug.Show($"Не знайдено елемент для XPath: {container}");

                title += titleNode?.InnerText.Trim() ?? "";
                title += " / ";
            }

            string date = "";
            foreach (var container in DateContainers)
            {
                var dateNode = doc.DocumentNode.SelectNodes(container);

                if (dateNode != null && DateContainers[0] == "//div[contains(@class, 'text-truncate')]")
                {
                    int i = 0;
                        foreach (var node in dateNode)
                        {
                            if (i == 3)
                        { 
                                date = node.InnerText.Trim();
                        }
                            i++;
                    }
                }
            }
            if(date == "")
                date = "No date";
            return (title, date);
        }
    }
}