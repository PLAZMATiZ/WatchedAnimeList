using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WatchedAnimeList.Helpers;

namespace WatchedAnimeList.Controls
{
    /// <summary>
    /// Interaction logic for NotificationsMenu.xaml
    /// </summary>
    public partial class NotificationsMenu : UserControl
    {
        public NotificationsMenu()
        {
            InitializeComponent();
            NotificationsHelper.SubToNotificationsChange(NotificationsChanged);
            NotificationsRefresh();
        }

        private void Notification_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is Notification n)
            {
                n.OnClick?.Invoke();
            }
        }
        private void NotificationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            NotificationsRefresh();
        }

        private void NotificationsRefresh()
        {
            if (NotificationsHelper.Notifications.Count == 0)
                Notifications_.Text = LocalizationHelper.GetTranslation("N/A_Notifications");
            else
                Notifications_.Text = "";
        }
        private void Exit_Clik(object sender, RoutedEventArgs e)
        {
            MainPage.Global.RightPopupContent.Content = null;
        }
    }
}
