using System;
using System.Collections.Generic;
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
using WatchedAnimeList.Helpers;

namespace WatchedAnimeList.Controls
{
    /// <summary>
    /// Interaction logic for SettingsConfig_Page.xaml
    /// </summary>
    public partial class SettingsConfig_Page : UserControl, IPage
    {
        public string PageName => "SettingsConfig_Page";
        public SettingsConfig_Page()
        {
            InitializeComponent();
            TextInit();
        }

        public void Initialize()
        {
            var pageHistory = PagesHelper.GetPreviousPageName;
            if(pageHistory != null)
                PageHistory_TextBlock.Text = string.Join("/", pageHistory);
        }

        private void TextInit()
        {

        }
    }
}
