using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WatchedAnimeList.ViewModels;
using WatchedAnimeList.Helpers;
using Debug = WatchedAnimeList.Helpers.Debug;
using System.IO;
using System.Globalization;
using System.Windows.Controls.Primitives;
using MediaColor = System.Windows.Media.Color;
using MediaRectangle = System.Windows.Shapes.Rectangle;
using System.Windows.Threading;
using Point = System.Windows.Point;


namespace WatchedAnimeList.Controls
{
    /// <summary>
    /// Interaction logic for WatchAnimePage.xaml
    /// </summary>
    public partial class WatchAnimePage : System.Windows.Controls.UserControl
    {
        private MpvPlayer player;

        public WatchAnimePage()
        {
            InitializeComponent();

            var playerWithButtons = new PlayerWithButtons();
            host.Child = playerWithButtons;
            host.MouseDown += playerWithButtons.BtnPause_Click;
            player = playerWithButtons.player;

            this.KeyDown += Window_KeyDown;
            //
            test();
        }
        private async void test()
        {
            await Task.Delay(1000);
            string path = @"D:\_DOWN\Clevatess - AniLiberty [WEBRip 1080p]\Clevatess_[01].mkv";
            if (!System.IO.File.Exists(path))
                throw new FileNotFoundException("Відеофайл не знайдено", path);

            player.Load(path);
        }

        private void MainPage_Button_Click(object sender, EventArgs e)
        {
            MainWindow.Global.MainPage();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.D1:
                        player.ExecuteCommand(@"change-list glsl-shaders set ""~~/shaders/Anime4K_Clamp_Highlights.glsl;~~/shaders/Anime4K_Restore_CNN_VL.glsl;~~/shaders/Anime4K_Upscale_CNN_x2_VL.glsl;~~/shaders/Anime4K_AutoDownscalePre_x2.glsl;~~/shaders/Anime4K_AutoDownscalePre_x4.glsl;~~/shaders/Anime4K_Upscale_CNN_x2_M.glsl""");
                        player.ExecuteCommand(@"show-text ""Anime4K: Mode A (HQ)""");
                        break;
                    case System.Windows.Input.Key.D2:
                        player.ExecuteCommand(@"change-list glsl-shaders set ""~~/shaders/Anime4K_Clamp_Highlights.glsl;~~/shaders/Anime4K_Restore_CNN_Soft_VL.glsl;~~/shaders/Anime4K_Upscale_CNN_x2_VL.glsl;~~/shaders/Anime4K_AutoDownscalePre_x2.glsl;~~/shaders/Anime4K_AutoDownscalePre_x4.glsl;~~/shaders/Anime4K_Upscale_CNN_x2_M.glsl""");
                        player.ExecuteCommand(@"show-text ""Anime4K: Mode B (HQ)""");
                        break;
                    case System.Windows.Input.Key.D3:
                        player.ExecuteCommand(@"change-list glsl-shaders set ""~~/shaders/Anime4K_Clamp_Highlights.glsl;~~/shaders/Anime4K_Upscale_Denoise_CNN_x2_VL.glsl;~~/shaders/Anime4K_AutoDownscalePre_x2.glsl;~~/shaders/Anime4K_AutoDownscalePre_x4.glsl;~~/shaders/Anime4K_Upscale_CNN_x2_M.glsl""");
                        player.ExecuteCommand(@"show-text ""Anime4K: Mode C (HQ)""");
                        break;
                    case System.Windows.Input.Key.D4:
                        player.ExecuteCommand(@"change-list glsl-shaders set ""~~/shaders/Anime4K_Clamp_Highlights.glsl;~~/shaders/Anime4K_Restore_CNN_VL.glsl;~~/shaders/Anime4K_Upscale_CNN_x2_VL.glsl;~~/shaders/Anime4K_Restore_CNN_M.glsl;~~/shaders/Anime4K_AutoDownscalePre_x2.glsl;~~/shaders/Anime4K_AutoDownscalePre_x4.glsl;~~/shaders/Anime4K_Upscale_CNN_x2_M.glsl""");
                        player.ExecuteCommand(@"show-text ""Anime4K: Mode A+A (HQ)""");
                        break;
                    case System.Windows.Input.Key.D5:
                        player.ExecuteCommand(@"change-list glsl-shaders set ""~~/shaders/Anime4K_Clamp_Highlights.glsl;~~/shaders/Anime4K_Restore_CNN_Soft_VL.glsl;~~/shaders/Anime4K_Upscale_CNN_x2_VL.glsl;~~/shaders/Anime4K_AutoDownscalePre_x2.glsl;~~/shaders/Anime4K_AutoDownscalePre_x4.glsl;~~/shaders/Anime4K_Restore_CNN_Soft_M.glsl;~~/shaders/Anime4K_Upscale_CNN_x2_M.glsl""");
                        player.ExecuteCommand(@"show-text ""Anime4K: Mode B+B (HQ)""");
                        break;
                    case System.Windows.Input.Key.D6:
                        player.ExecuteCommand(@"change-list glsl-shaders set ""~~/shaders/Anime4K_Clamp_Highlights.glsl;~~/shaders/Anime4K_Upscale_Denoise_CNN_x2_VL.glsl;~~/shaders/Anime4K_AutoDownscalePre_x2.glsl;~~/shaders/Anime4K_AutoDownscalePre_x4.glsl;~~/shaders/Anime4K_Restore_CNN_M.glsl;~~/shaders/Anime4K_Upscale_CNN_x2_M.glsl""");
                        player.ExecuteCommand(@"show-text ""Anime4K: Mode C+A (HQ)""");
                        break;
                    case System.Windows.Input.Key.D0:
                        player.ExecuteCommand(@"change-list glsl-shaders set """"");
                        player.ExecuteCommand(@"show-text ""GLSL shaders cleared""");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.Show("Error executing shader command: " + ex.Message);
            }
        }

        private void left_Click(object sender, EventArgs agr)
        {
            player.ExecuteCommand("seek -5 relative");
        }
        private void right_Click(object sender, EventArgs agr)
        {
            player.ExecuteCommand("seek 5 relative");
        }
    }
}
