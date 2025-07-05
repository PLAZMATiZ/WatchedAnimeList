using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DeepL;
using JikanDotNet;
using static System.Net.Mime.MediaTypeNames;
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using FuzzySharp;
using System.Net.Http;

using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using System.Xml.Linq;
using System.ComponentModel;

using WatchedAnimeList.Helpers;
using WatchedAnimeList.Logic;
using WatchedAnimeList.Models;
using WatchedAnimeList.ViewModels;
using WatchedAnimeList.Controls;
using System.Windows.Threading;
using System.Collections.ObjectModel;

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
                case "WatchAnimePage":
                    MainContent.Content = new WatchAnimePage();
                    break;
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
        #endregion
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized)
                this.Hide();
        }
    }
}