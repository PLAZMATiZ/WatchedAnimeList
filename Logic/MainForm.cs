using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DeepL;
using FuzzySharp;
using System.Net.Http;

using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using System.Xml.Linq;
using System.ComponentModel;
using JikanDotNet;

namespace WachedAnimeList
{
    public partial class MainForm : Form
    {
        public static MainForm Global;
        public MainForm()
        {
            InitializeComponent();
            Global = this;

            new WachedAnimeSaveLoad().Initialize();
            new Settings().Initialize();
            new SiteParser().Initialize();
            new Resizer().Initialize();
            ResizeAll();

            SetupSearchDelay();
            SetupResizeDelay();

            this.BackColor = Color.FromArgb(10, 10, 10);

            this.Resize += (s, e) =>
            {
                resizeDelayTimer.Stop();
                resizeDelayTimer.Start();
            };
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }

        #region Add Anime

        private string AnimeName;
        private string AnimeNameEN;

        #region Buffer Button
        private void button1_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText();

                if (SiteParser.Global.UrlValidate(text))
                {
                    var animeInfo = SiteParse(text);
                    return;
                }
                else if (TextVerify(text))
                {
                    AnimeNameFormating(text);
                }
                else
                {
                    MessageBox.Show("Даун шо за хуйня а не текст");
                }
            }
            else
            {
                MessageBox.Show("Даун скопіюй нормально");
            }
        }
        private async Task SiteParse(string url)
        {
            string title = "";
            string date = "";
            await SiteParser.Global.SiteParse(url, (_title, _date) =>
            {
                title = _title;
                date = _date;
            });

            string[] parts = title.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                AnimeName = parts[0].Trim();
                AnimeNameEN = parts[1].Trim();
            }
            else
            {
                var (eng, other) = SplitByEnglish(title);
                if (eng != "" && other != "")
                {
                    AnimeName = other.Trim();
                    AnimeNameEN = eng.Trim();
                }
            }

            if (AnimeNameEN == "" || AnimeName == "" || AnimeNameEN == null || AnimeName == null)
            {
                MessageBox.Show("Силка хуйня");
                return;
            }
            CreateAnimeCard(AnimeNameEN, AnimeName);
        }
        private static bool TextVerify(string text)
        {
            int letters = 0;
            //int slash = 0;

            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    letters++;
                }
                //else if (c == '/')
                //{
                    //slash++;
                //}
            }

            if (letters < 3)
                return false;
            //if (slash > 1)
                //return false;
            return true;
        }

        private async void AnimeNameFormating(string text)
        {
            string name = "";
            string eng_Name = "";

            string[] parts = text.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                name = parts[0].Trim();
                eng_Name = parts[1].Trim();
            }
            else
            {
                var (eng, other) = SplitByEnglish(text);
                if (eng != "" && other != "")
                {
                    name = other.Trim();
                    eng_Name = eng.Trim();
                }
                else
                {
                    name = text;

                    var client = new Translator("49d710dd-2897-4129-b171-2ea0548043c8:fx");
                    var translatedText = await client.TranslateTextAsync(
                    text,
                    LanguageCode.Russian,
                    LanguageCode.EnglishAmerican);
                    eng_Name = translatedText.ToString();
                }
            }

            AnimeName = name;
            AnimeNameEN = eng_Name;

            CreateAnimeCard(eng_Name, name);
        }

        public async void CreateAnimeCard(string eng_Name, string name)
        {
            Anime title = await GetAnimeTitle(eng_Name);

            var animeData = await CreateWachedAnimeData(title, name);
            if (animeData == null)
                return;

            WachedAnimeSaveLoad.Global.AddAnime(animeData);

            AddAnimeCardsAsync([animeData]);
        }

        static bool IsEnglish(string word)
        {
            return Regex.IsMatch(word, "^[a-zA-Z]+$");
        }

        static (string eng, string other) SplitByEnglish(string input)
        {
            string[] words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            List<string> beforeEnglish = new();
            List<string> englishWords = new();

            bool foundEnglish = false;

            foreach (var word in words)
            {
                if (!foundEnglish && IsEnglish(word))
                {
                    foundEnglish = true;
                }

                if (foundEnglish)
                    englishWords.Add(word);
                else
                    beforeEnglish.Add(word);
            }

            return (string.Join(' ', englishWords), string.Join(' ', beforeEnglish));
        }
        #endregion

        #region Load Anime Icon

        public async Task<Anime> GetAnimeTitle(string animeNameEN)
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

        private async Task<WachedAnimeData> CreateWachedAnimeData(Anime anime, string animeName)
        {
            var wachedAnimeData = new WachedAnimeData();

            var title = anime.Titles.FirstOrDefault(t => t.Type == "English")?.Title
                     ?? anime.Titles.FirstOrDefault()?.Title
                     ?? "Unnamed";

            string imageUrl = anime.Images.JPG.ImageUrl;

            wachedAnimeData.AnimeNameEN = title;
            wachedAnimeData.AnimeName = animeName;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var imgData = await client.GetByteArrayAsync(imageUrl);
                    using (var ms = new MemoryStream(imgData))
                    {
                        wachedAnimeData.animeImage = System.Drawing.Image.FromStream(ms);
                    }
                }
            }
            catch
            {
                wachedAnimeData.animeImage = Properties.Resources._5350447830046734641;
            }

            // Очікуємо взаємодію з користувачем
            var tcs = new TaskCompletionSource<WachedAnimeData>();
            using (var form = new Form2(wachedAnimeData, tcs))
            {
                form.ShowDialog();
                return await tcs.Task; // чекаємо, поки користувач не натисне кнопку
            }
        }

        private Dictionary<string, Panel> cardCache = new();
        public void AddAnimeCardsAsync(WachedAnimeData[] animeArray)
        {
            if (animeArray == null || animeArray.Length == 0)
                return;

            bool isBulk = animeArray.Length > 1;
            if (isBulk)
            {
                animeListPanel.SuspendLayout();
                animeListPanel.Controls.Clear();
                cardCache.Clear();
            }

            foreach (var animeData in animeArray)
            {
                string key = animeData.AnimeNameEN.ToLowerInvariant();

                if (cardCache.ContainsKey(key))
                    continue;

                var card = new Panel
                {
                    Width = 160,
                    Height = 260,
                    Margin = new Padding(10),
                    BackColor = Color.FromArgb(23, 23, 23)
                };
                card.Paint += (s, e) =>
                {
                    Color borderColor = Color.FromArgb(46, 47, 47);
                    int borderWidth = 2;
                    Control p = (Control)s;
                    using (Pen pen = new Pen(borderColor, borderWidth))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                    }
                };

                var picture = new PictureBox
                {
                    Width = 140,
                    Height = 200,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Location = new Point(10, 10),
                    Image = animeData.animeImage,
                    Tag = animeData.AnimeNameEN
                };
                picture.Click += Card_Click;

                var label = new Label
                {
                    Text = animeData.AnimeName,
                    Width = 140,
                    Height = 40,
                    Location = new Point(5, 210),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.FromArgb(229, 229, 229),
                    AutoEllipsis = true
                };

                card.Controls.Add(picture);
                card.Controls.Add(label);

                animeListPanel.Controls.Add(card);
                cardCache[key] = card;
            }

            if (isBulk)
                animeListPanel.ResumeLayout();
            WachedAnimeSaveLoad.Global.Save();

        }


        public void Card_Click(object sender, EventArgs e)
        {
            var clickedCard = sender as PictureBox;
            var name = clickedCard.Tag as string;

            var data = WachedAnimeSaveLoad.Global.wachedAnimeDict[name];
            if (data == null)
                return;

            using (var newForm = new AnimeCardForm(data))
            {
                newForm.ShowDialog();
            }
            this.Show();
        }

        #endregion

        #endregion

        
        public void ReorderCards(string[] orderedNames)
        {
            if (orderedNames == null || orderedNames.Length == 0)
                return;

            animeListPanel.SuspendLayout();
            animeListPanel.Controls.Clear();

            foreach (var name in orderedNames)
            {
                var key = name.ToLowerInvariant();
                if (cardCache.TryGetValue(key, out var panel))
                {
                    animeListPanel.Controls.Add(panel);
                }
            }

            animeListPanel.ResumeLayout();
        }
        public void ReloadAnimeCards()
        {
            if (WachedAnimeSaveLoad.Global != null)
            {
                WachedAnimeSaveLoad.Global.Load();
                AddAnimeCardsAsync(WachedAnimeSaveLoad.Global.wachedAnimeDict.Values.ToArray());
            }
        }

        private void ClearAnimeCards()
        {
            animeListPanel.SuspendLayout();
            foreach (Control ctrl in animeListPanel.Controls)
            {
                // Відписуємо всі PictureBox події
                if (ctrl is Panel panel)
                {
                    foreach (Control inner in panel.Controls)
                    {
                        if (inner is PictureBox pic)
                        {
                            pic.Click -= Card_Click;
                            pic.Image?.Dispose(); // очищаємо картинку з памʼяті
                        }
                    }
                    panel.Dispose(); // саму панель
                }
            }

            animeListPanel.Controls.Clear();
            cardCache.Clear();
            animeListPanel.ResumeLayout();
            WachedAnimeSaveLoad.Global.wachedAnimeDict.Clear();
            GC.Collect(); // запуск GC (опціонально)
        }


        private void MainForm_Closing(object sender, FormClosingEventArgs e)
        {
            WachedAnimeSaveLoad.Global.Save();
            Settings.Global.SaveSettings();

            e.Cancel = true;
            this.Hide();
            ClearAnimeCards();
        }
    }


}
