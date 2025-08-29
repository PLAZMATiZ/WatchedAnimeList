using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Specialized;
using WatchedAnimeList.Controls;

namespace WatchedAnimeList.Helpers
{
    /// <summary>
    /// Provides helper methods and storage for managing application notifications.
    /// Example of use:
    /// NotificationsHelper.CreateNotification("Text", OnClickAction.OpenAnimeInfo, "AnimeName");
    /// </summary>
    public static class NotificationsHelper
    {
        private static string? folderPath;

        /// <summary>
        /// Internal collection of notifications.
        /// </summary>
        private static readonly ObservableCollection<Notification> _notifications = new();

        /// <summary>
        /// Read-only access to notifications collection.
        /// </summary>
        public static ReadOnlyObservableCollection<Notification> Notifications { get; } = new(_notifications);

        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static event EventHandler<Notification>? _isCheckedChanged;

        /// <summary>
        /// Initializes the notifications system, sets up collection change handling
        /// </summary>
        /// <param name="_folderPath">Folder path where notifications will be saved/loaded</param>
        public static void Initialize(string _folderPath)
        {
            folderPath = _folderPath;

            _notifications.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (Notification n in e.NewItems)
                        n.PropertyChanged += Notification_PropertyChanged;

                if (e.OldItems != null)
                    foreach (Notification n in e.OldItems)
                        n.PropertyChanged -= Notification_PropertyChanged;
            };

            _ = LoadAsync();
            SubToNotificationsChange(SaveAsync);
        }

        /// <summary>
        /// Handles PropertyChanged for individual notifications
        /// </summary>
        private static void Notification_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Notification.IsChecked) && sender is Notification n)
            {
                _isCheckedChanged?.Invoke(null, n);
            }
        }

        /// <summary>
        /// Subscribes to notifications collection changes
        /// </summary>
        public static void SubToNotificationsChange(NotifyCollectionChangedEventHandler handler) =>
            _notifications.CollectionChanged += handler;

        /// <summary>
        /// Unsubscribes from notifications collection changes
        /// </summary>
        public static void UnsubToNotificationsChange(NotifyCollectionChangedEventHandler handler) =>
            _notifications.CollectionChanged -= handler;

        /// <summary>
        /// Subscribes to individual notification IsChecked changes
        /// </summary>
        public static void SubToIsCheckedChanged(EventHandler<Notification> handler) =>
            _isCheckedChanged += handler;

        /// <summary>
        /// Unsubscribes from individual notification IsChecked changes
        /// </summary>
        public static void UnsubToIsCheckedChanged(EventHandler<Notification> handler) =>
            _isCheckedChanged -= handler;

        /// <summary>
        /// Returns the total count of notifications
        /// </summary>
        public static int GetNotificationCount() => _notifications.Count;

        /// <summary>
        /// Returns the number of unread notifications
        /// </summary>
        public static int GetUnreadNotificationCount() => _notifications.Count(n => !n.IsChecked);

        /// <summary>
        /// Adds a pre-created notification
        /// </summary>
        public static void AddNotification(Notification notification) => _notifications.Add(notification);

        /// <summary>
        /// Creates and adds a new notification
        /// </summary>
        public static Notification CreateNotification(string messageText, OnClickAction onClickType, string onClickKey)
        {
            var notification = new Notification
            {
                MessageText = messageText,
                OnClickType = onClickType,
                OnClickKey = onClickKey
            };

            notification.OnClick = () =>
            {
                OnNotificationClick(notification);
            };

            return notification;
        }

        /// <summary>
        /// Handles notification click action
        /// </summary>
        public static void OnNotificationClick(Notification notification)
        {
            notification.IsChecked = true;

            if (notification.OnClickType == OnClickAction.OpenAnimeInfo)
            {
                var animeData = AnimeManager.GetAnimeByName(notification.OnClickKey);
                if (animeData is null)
                    Debug.Ex("animeData is null");

                var page = new AnimeInfo_Page(animeData);
                MainWindow.Global.GoToPage(page);
            }
        }

        /// <summary>
        /// Saves all notifications to JSON file
        /// </summary>
        public static void SaveAsync(object? sender, NotifyCollectionChangedEventArgs e) => _ = SaveAsync();
        public static async Task SaveAsync()
        {
            if (folderPath == null) throw new InvalidOperationException("folderPath is null");

            if (_notifications.Count == 0) return;

            string jsonPath = Path.Combine(folderPath, "notifications.json");

            var json = JsonSerializer.Serialize(_notifications.ToList(), jsonSerializerOptions);
            await File.WriteAllTextAsync(jsonPath, json);
        }

        /// <summary>
        /// Loads notifications from JSON file
        /// </summary>
        public static async Task LoadAsync()
        {
            if (folderPath == null) throw new InvalidOperationException("folderPath is null");

            string jsonPath = Path.Combine(folderPath, "notifications.json");
            if (!File.Exists(jsonPath)) return;

            string json = await File.ReadAllTextAsync(jsonPath);
            var loadedNotifications = JsonSerializer.Deserialize<List<Notification>>(json, jsonSerializerOptions);

            if (loadedNotifications == null) return;

            _notifications.Clear();
            foreach (var n in loadedNotifications)
            {
                n.OnClick = () => OnNotificationClick(n);
                _notifications.Add(n);
            }
        }
    }

    /// <summary>
    /// Represents a single notification with text, click action, and read state.
    /// </summary>
    public class Notification : INotifyPropertyChanged
    {
        private bool _isChecked = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Message text of the notification
        /// </summary>
        public required string MessageText { get; set; }

        /// <summary>
        /// Type of action when clicked
        /// </summary>
        public required OnClickAction OnClickType { get; set; }

        /// <summary>
        /// Key used for OnClick action
        /// </summary>
        public required string OnClickKey { get; set; }

        /// <summary>
        /// Action executed when notification clicked
        /// </summary>
        [JsonIgnore]
        public Action? OnClick { get; set; }

        /// <summary>
        /// Whether notification is checked (read). Triggers PropertyChanged.
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }
        }
    }

    /// <summary>
    /// Specifies available actions when a notification is clicked
    /// </summary>
    public enum OnClickAction
    {
        OpenAnimeInfo,
        OpenUrl
    }
}
