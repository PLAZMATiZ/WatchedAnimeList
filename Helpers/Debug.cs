using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
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

    /// <summary>
    /// Provides logging and debugging utilities, including file logging,
    /// message box display, and exception throwing.
    /// </summary>
    public static class Debug
    {
        private static Action<string>? _logAction;

        // Підписка на лог
        public static void SubscribeLog(Action<string> action) => _logAction += action;

        public static void UnsubscribeLog(Action<string> action) => _logAction -= action;

        private static readonly string logPath = "log.txt";

        /// <summary>
        /// Logs a message to the file and optionally shows it in a MessageBox.
        /// </summary>
        /// <param name="message">The message to log or display.</param>
        /// <param name="type">The type of notification (Info, Success, Warning, Error).</param>
        /// <param name="file">The source file path (automatically filled by compiler).</param>
        /// <param name="line">The line number where the call occurred (auto).</param>
        /// <param name="member">The member name where the call occurred (auto).</param>
        public static void Log(string message, NotificationType type = NotificationType.Info,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            string fileName = Path.GetFileName(file);
            string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] [{fileName}:{line} - {member}] {message}{Environment.NewLine}";

            // Запис у файл
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logPath), formatted);

            // Відправка у UI або підписникам
            _logAction?.Invoke(formatted);
        }

        /// <summary>
        /// Shows a message box with the message and optionally logs it.
        /// </summary>
        /// <param name="message">The message to show.</param>
        /// <param name="type">The notification type.</param>
        /// <param name="file">Compiler filled file path.</param>
        /// <param name="line">Compiler filled line number.</param>
        /// <param name="member">Compiler filled member name.</param>
        public static void Show(string message, NotificationType type = NotificationType.Info,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            string fileName = Path.GetFileName(file);
            string fullMessage = $"[{fileName}:{line} - {member}] {message}";

            MessageBoxImage icon = type switch
            {
                NotificationType.Success => MessageBoxImage.Asterisk,
                NotificationType.Warning => MessageBoxImage.Warning,
                NotificationType.Error => MessageBoxImage.Error,
                _ => MessageBoxImage.Information
            };

            MessageBox.Show(fullMessage, "WAL", MessageBoxButton.OK, icon);
        }

        /// <summary>
        /// Logs and shows the message in one call.
        /// </summary>
        /// <param name="message">The message to log and show.</param>
        /// <param name="type">The notification type.</param>
        /// <param name="file">Compiler filled file path.</param>
        /// <param name="line">Compiler filled line number.</param>
        /// <param name="member">Compiler filled member name.</param>
        public static void ShowAndLog(string message, NotificationType type = NotificationType.Info,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Show(message, type, file, line, member);
            Log(message, type, file, line, member);
        }

        /// <summary>
        /// Logs the message and throws a standard Exception.
        /// </summary>
        /// <param name="message">The message to log and include in the exception.</param>
        /// <param name="type">The notification type.</param>
        /// <param name="file">Compiler filled file path.</param>
        /// <param name="line">Compiler filled line number.</param>
        /// <param name="member">Compiler filled member name.</param>
        public static void LogAndThrow(string message, NotificationType type = NotificationType.Error,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Log(message, type, file, line, member);
            throw new Exception(message); // або свій Exception тип
        }

        /// <summary>
        /// Logs the message and throws a custom exception type.
        /// </summary>
        /// <typeparam name="TException">The type of exception to throw.</typeparam>
        /// <param name="message">The message to log and include in the exception.</param>
        /// <param name="type">The notification type.</param>
        /// <param name="file">Compiler filled file path.</param>
        /// <param name="line">Compiler filled line number.</param>
        /// <param name="member">Compiler filled member name.</param>
        public static void LogAndThrow<TException>(string message, NotificationType type = NotificationType.Error,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "") where TException : Exception
        {
            Log(message, type, file, line, member);
            var ex = (TException)Activator.CreateInstance(typeof(TException), message)!;
            throw ex;
        }

        /// <summary>
        /// Logs a message and immediately throws an Exception with that message.
        /// </summary>
        /// <param name="message">The message to log and include in the exception.</param>
        /// <param name="type">The type of notification (default Info).</param>
        /// <param name="file">Automatically filled by compiler (caller file path).</param>
        /// <param name="line">Automatically filled by compiler (caller line number).</param>
        /// <param name="member">Automatically filled by compiler (caller member name).</param>
        [DoesNotReturn]
        public static void Ex(string message, NotificationType type = NotificationType.Info,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Log(message, type, file, line, member);
            throw new Exception(message);
        }
    }
}
