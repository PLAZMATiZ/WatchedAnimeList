using System.IO;
using System.Windows;

namespace WatchedAnimeList.Helpers
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public static class Debug
    {
        public static Action<string> LogAction;
        private static readonly string logPath = "log.txt";

        public static void Show(string message, NotificationType type = NotificationType.Info, string title = "WAL")
        {
            MessageBoxImage icon = MessageBoxImage.Information;

            switch (type)
            {
                case NotificationType.Success: icon = MessageBoxImage.Asterisk; break;
                case NotificationType.Warning: icon = MessageBoxImage.Warning; break;
                case NotificationType.Error: icon = MessageBoxImage.Error; break;
                default: icon = MessageBoxImage.Information; break;
            }

            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        public static void Log(string message)
        {
            string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";

            // Запис в файл
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logPath), formatted);

            // Відправка в UI, якщо підписалися
            LogAction?.Invoke(formatted);
        }


        public static void ShowAndLog(string message, NotificationType type = NotificationType.Info)
        {
            Show(message, type);
            Log(message);
        }
    }
}
