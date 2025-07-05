using System.IO;
using System.Windows;
using System.Windows.Input;

using WatchedAnimeList.Helpers;
using WatchedAnimeList.Controls;

namespace WatchedAnimeList
{
    public partial class MainWindow : Window
    {
        public static MainWindow Global;
        public MainPage mainPage;
        public MainWindow()
        {
            Global = this;
            mainPage = new();

            InitializeComponent();
            Initializer.Inithialize();

            MainPage();
        }

        public void MainPage()
        {
            MainContent.Content = mainPage;
        }
        public void GoToPage(string page)
        {
            switch(page)
            {
                case "MainPage":
                    MainContent.Content = mainPage;
                    break;
            }

        }
        #region UI Elements
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Закрити
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        // Мінімізувати
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // Тогл розгорнутого/віконного режиму
        private void WindowedButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void OnTorrentFileDropped(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string torrentFile = files.FirstOrDefault(f => f.EndsWith(".torrent"));

                if (torrentFile != null)
                {
                    WatchAnimePage page = new(torrentFile);
                    MainContent.Content = page;
                }
            }
        }

        #endregion
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized)
                this.Hide();
        }
    }
}