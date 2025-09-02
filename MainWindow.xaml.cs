using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using WatchedAnimeList.Helpers;
using WatchedAnimeList.Controls;

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

            MainPage();


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
        }

        public void MainPage(bool disposePrevious = true)
        {
            if (disposePrevious && MainContent.Content is IDisposable disposable)
            {
                disposable.Dispose();
                MainContent.Content = null;
            }
            MainContent.Content = mainPage;
        }
        public void GoToPage(UserControl page, bool disposePrevious = true)
        {
            if (disposePrevious && MainContent.Content is IDisposable disposable)
            {
                disposable.Dispose();
                MainContent.Content = null;
            }
            MainContent.Content = page;
        }

        #region UI Elements

        Point LastMousePosition;
        private void ResiseBorder_MouseDown(object sender, MouseEventArgs e)
        {
            var mousePos = Mouse.GetPosition(this);


            LastMousePosition = mousePos;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
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

                WatchAnimePage page = new(torrentFile);
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