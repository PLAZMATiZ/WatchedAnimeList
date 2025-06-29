using System;
using System.Windows;
using System.IO;
using Application = System.Windows.Application;
using System.Reflection;

namespace WatchedAnimeList.Helpers
{
    public class TrayIconService : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private Window _mainWindow;

        public TrayIconService(Window mainWindow)
        {
            _mainWindow = mainWindow;

            _notifyIcon = new NotifyIcon();
            var assembly = Assembly.GetExecutingAssembly();
            using Stream iconStream = assembly.GetManifestResourceStream("WatchedAnimeList.Assets.Icon.ico");

            if (iconStream != null)
            {
                _notifyIcon.Icon = new Icon(iconStream);
            }
            else
            {
                Debug.Show("Icon not found in embedded resources.");
            }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Watched Anime List";

            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // Контекстне меню
            var contextMenu = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem("Відкрити");
            openItem.Click += (s, e) => ShowMainWindow();
            var exitItem = new ToolStripMenuItem("Вийти");
            exitItem.Click += (s, e) => ExitApp();

            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            if (_mainWindow == null) return;

            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;

            _mainWindow.Show();
            _mainWindow.Activate();
        }

        private void ExitApp()
        {
            _notifyIcon.Visible = false;
            Application.Current.Shutdown();
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
        }
    }
}
