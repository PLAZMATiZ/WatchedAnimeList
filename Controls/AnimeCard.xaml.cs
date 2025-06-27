using System.Windows.Controls;
using System.Windows.Media;

namespace WatchedAnimeList.Controls
{
    public partial class AnimeCard : UserControl
    {
        public string AnimeName { get; private set; }

        public event EventHandler<string> CardClicked;

        public AnimeCard(string name, ImageSource? image = null)
        {
            InitializeComponent();

            AnimeName = name;
            AnimeCardName.Text = name;

            if (image != null)
                AnimeCardImage.Source = image;

            AnimeCardButton.Click += (s, e) => CardClicked?.Invoke(this, AnimeName);
        }
    }
}
