using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WatchedAnimeList.Helpers
{
    using ToolTip = System.Windows.Controls.ToolTip;

    public static class GlobalToolTip
    {
        private static ToolTip? _activeToolTip;
        private static FrameworkElement? _currentElement;

        static GlobalToolTip()
        {
            // швидке появлення тултіпів
            ToolTipService.InitialShowDelayProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(200) // 0-200 мс
            );

            // глобальна подія для всіх тултіпів
            EventManager.RegisterClassHandler(
                typeof(FrameworkElement),
                FrameworkElement.ToolTipOpeningEvent,
                new ToolTipEventHandler(OnToolTipOpening)
            );
        }

        public static void Init() { }

        private static void OnToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;

            // якщо тултіп не ToolTip, обгортаємо
            if (!(fe.ToolTip is ToolTip tt))
            {
                tt = new ToolTip { Content = fe.ToolTip };
                fe.ToolTip = tt;
            }

            _activeToolTip = tt;
            _currentElement = fe;

            // відкриваємо тултіп одразу
            var pos = Mouse.GetPosition(Application.Current.MainWindow);
            tt.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
            tt.Background = new SolidColorBrush(Color.FromArgb(180, 100, 100, 100));
            tt.Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            tt.PlacementTarget = null; // не прив’язувати до елемента
            tt.HorizontalOffset = pos.X + 80;
            tt.VerticalOffset = pos.Y + 50;
            tt.IsOpen = true;

            // стежимо за рухом мишки всередині елемента
            fe.MouseMove += Fe_MouseMove;
            fe.MouseLeave += Fe_MouseLeave;

            e.Handled = true; // щоб WPF не відкривав ще раз
        }

        private static void Fe_MouseMove(object sender, MouseEventArgs e)
        {
            if (_activeToolTip == null) return;

            var pos = e.GetPosition(Application.Current.MainWindow);

            // оновлюємо позицію тултіпу
            _activeToolTip.HorizontalOffset = pos.X + 80;
            _activeToolTip.VerticalOffset = pos.Y + 50;
        }


        private static void Fe_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_activeToolTip != null)
            {
                _activeToolTip.IsOpen = false;
                _activeToolTip = null;
            }

            if (sender is FrameworkElement fe)
            {
                fe.MouseMove -= Fe_MouseMove;
                fe.MouseLeave -= Fe_MouseLeave;
            }

            _currentElement = null;
        }
    }
}
