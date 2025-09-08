using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WatchedAnimeList.Helpers;
using WatchedAnimeList.Logic;
using WatchedAnimeList.Models;
using WatchedAnimeList.ViewModels;

namespace WatchedAnimeList.Controls
{
    public partial class AnimeCard : System.Windows.Controls.UserControl
    {
        public AnimeCard()
        {
            InitializeComponent();
            ImageContainer.SizeChanged += (s, e) =>
            {
                var width = ImageContainer.ActualWidth;
                var height = ImageContainer.ActualHeight;

                ImageContainer.Clip = new RectangleGeometry
                {
                    Rect = new Rect(0, 0, width, height),
                    RadiusX = 10,
                    RadiusY = 10
                };
            };
            this.DataContextChanged += (s, e) =>
            {
                UpdateTooltip();
            };
            //Settings.Load()
            SetSize(0);
        }
        private void UpdateTooltip()
        {
            if (this.DataContext is AnimeItemViewModel model)
            {
                AnimeCardButton.ToolTip = model.OriginalName;
            }
            else
            {
                AnimeCardButton.ToolTip = null;
            }
        }

        public void SetSize(byte sizeLevel)
        {
            double controlWidth, controlHeight;
            double borderWidth, borderHeight;
            double imageHeight, fontSize;

            switch (sizeLevel)
            {
                case 0:
                    controlWidth = 140;
                    controlHeight = 240;
                    borderWidth = 120;
                    borderHeight = 220;
                    imageHeight = 170;
                    fontSize = 14;
                    break;
                case 2:
                    controlWidth = 180;
                    controlHeight = 320;
                    borderWidth = 160;
                    borderHeight = 300;
                    imageHeight = 250;
                    fontSize = 16;
                    break;
                case 1:
                default:
                    controlWidth = 160;
                    controlHeight = 280;
                    borderWidth = 140;
                    borderHeight = 260;
                    imageHeight = 210;
                    fontSize = 16;
                    break;
            }

            this.Width = controlWidth;
            this.Height = controlHeight;

            AnimeCardBorder.Width = borderWidth;
            AnimeCardBorder.Height = borderHeight;

            ImageContainer.Width = borderWidth;
            ImageContainer.Height = imageHeight;

            AnimeCardImage.Width = borderWidth;
            AnimeCardImage.Height = imageHeight;

            AnimeCardName.FontSize = fontSize;
        }
    }
}
