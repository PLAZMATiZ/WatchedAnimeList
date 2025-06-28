using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Win32;
using WatchedAnimeList.Helpers;

namespace WatchedAnimeList
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private NotifyIcon _notifyIcon;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Створюємо MainWindow
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show(); // Показуємо вікно

            // Створюємо трей-іконку
            _notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icon.ico")),
                Visible = true,
                Text = "WatchedAnimeList"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Відкрити", null, (s, ev) => ShowMainWindow());
            contextMenu.Items.Add("Вийти", null, (s, ev) => Shutdown());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, ev) => ShowMainWindow();

            string appName = "WatchedAnimeList";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            #if !DEBUG
            // Додати до автозапуску тільки у Release
            //AutorunHelper.EnableAutorun("WatchedAnimeList", exePath);
            
            // Перевірити, чи увімкнено автозапуск
            bool isEnabled = AutorunHelper.IsAutorunEnabled(appName);
            #endif
        }

        private void ShowMainWindow()
        {
            if (MainWindow == null)
                MainWindow = new MainWindow();

            if (!MainWindow.IsVisible)
                MainWindow.Show();

            if (MainWindow.WindowState == WindowState.Minimized)
                MainWindow.WindowState = WindowState.Normal;

            MainWindow.Activate();
        }
    }
}
