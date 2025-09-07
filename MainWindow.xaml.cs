using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WatchedAnimeList.Controls;
using WatchedAnimeList.Helpers;
using WatchedAnimeList.Logic;

namespace WatchedAnimeList
{
    public partial class MainWindow : Window
    {
        public static MainWindow Global = null!;
        public MainPage mainPage;
        public MainWindow()
        {
            Global = this;
            mainPage = new();

            InitializeComponent();
            Initializer.Inithialize();

            PagesHelper.GoToMainPage();

            // затичка
            AnimePostersLoader.IfLoadPoster = (isLoading) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (isLoading)
                        UpdateCircuit.Visibility = Visibility.Visible;
                    else
                        UpdateCircuit.Visibility = Visibility.Collapsed;
                });
            };

            this.MouseDown += Window_MouseDown;
            this.KeyDown += Window_KeyDown;
        }
        #region UI Elements
        #region ShortCuts
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Home)
            {
                PagesHelper.GoToMainPage();
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1)
                PagesHelper.GoBack();
            else if (e.ChangedButton == MouseButton.XButton2)
                PagesHelper.GoForward();
        }
        #endregion

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            Settings.SaveAll();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

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
                string? torrentFile = files.FirstOrDefault(f => f.EndsWith(".torrent"));
                if (torrentFile is null)
                    Debug.Ex("torrentFile is null");

                WatchAnimePage page = new();
                _ = page.HandleTorrentDrop(torrentFile);
                MainContent.Content = page;
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