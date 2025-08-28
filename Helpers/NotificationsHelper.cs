using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WatchedAnimeList.Controls;
using System.ComponentModel;
using System.Collections.Specialized;

namespace WatchedAnimeList.Helpers
{
    /// <summary>
    /// Provides helper methods and storage for managing application notifications.
    /// example of use:
    /// NotificationsHelper.AddNotification(() => YOUR ACTION, "YOUR TEXT");
    /// </summary>
    public static class NotificationsHelper
    {
        /// <summary>
        /// Initializes the notifications system, sets up collection change handling
        /// </summary>
        public static void Initialize()
        {

        }

        /// <summary>
        /// Handles subscription and unsubscription to the Notifications collection change events.
        /// </summary>
        public static void SubToNotificationsChange(NotifyCollectionChangedEventHandler handler)
        {
            Notifications.CollectionChanged += handler;
        }
        
        public static void UnsubToNotificationsChange(NotifyCollectionChangedEventHandler handler)
        {
            Notifications.CollectionChanged -= handler;
        }
        
        /// <summary>
        /// Gets the collection of all active notifications.
        /// </summary>
        public static ObservableCollection<Notification> Notifications { get; } = new();

        /// <summary>
        /// Creates a new notification and adds it to the collection.
        /// When clicked, the notification is marked as checked and the provided action is executed.
        /// </summary>
        /// <param name="onClick">Action to execute when the notification is clicked.</param>
        /// <param name="text">The text content of the notification.</param>
        public static void AddNotification(Action onClick, string text)
        {
            var notification = new Notification
            {
                Text = text,
                OnClick = onClick,
                IsChecked = false
            };

            notification.OnClick = () =>
            {
                notification.IsChecked = true;
                onClick?.Invoke();
            };

            NotificationsHelper.Notifications.Add(notification);
        }
    }

    /// <summary>
    /// Represents a single notification with text, click action, and read state.
    /// </summary>
    public class Notification : INotifyPropertyChanged
    {
        private bool _isChecked;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets the action to be executed when the notification is clicked.
        /// </summary>
        public required Action OnClick { get; set; }

        /// <summary>
        /// Gets or sets the display text of the notification.
        /// </summary>
        public required string Text { get; set; }

        /// <summary>
        /// Gets or sets whether the notification has been checked (read).
        /// Triggers <see cref="PropertyChanged"/> when modified.
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

}
