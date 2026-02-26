using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace RBAC_WPF_2026.Services;

public class NotificationService
{
    private static NotificationService? _instance;
    public static NotificationService Instance => _instance ??= new NotificationService();

    private NotificationService() { }

    public void ShowSuccess(string message, int durationMs = 3000)
    {
        ShowNotification(message, "#FF27AE60", "#FFE8F5E8", durationMs);
    }

    public void ShowError(string message, int durationMs = 4000)
    {
        ShowNotification(message, "#FFE74C3C", "#FFFDE8E8", durationMs);
    }

    public void ShowInfo(string message, int durationMs = 3000)
    {
        ShowNotification(message, "#FF3498DB", "#FFE8F4FD", durationMs);
    }

    private void ShowNotification(string message, string borderColor, string backgroundColor, int durationMs)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow == null) return;

            // Create notification popup
            var popup = new Popup
            {
                AllowsTransparency = true,
                StaysOpen = false,
                Placement = PlacementMode.Top,
                PlacementTarget = mainWindow,
                HorizontalOffset = (mainWindow.ActualWidth - 350) / 2,
                VerticalOffset = 60
            };

            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundColor)!),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderColor)!),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 15, 20, 15),
                MaxWidth = 350,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Gray,
                    Direction = 270,
                    ShadowDepth = 3,
                    Opacity = 0.3
                }
            };

            var textBlock = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderColor)!),
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = textBlock;
            popup.Child = border;

            // Show popup
            popup.IsOpen = true;

            // Auto-close after duration
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(durationMs)
            };

            timer.Tick += (s, e) =>
            {
                timer.Stop();
                popup.IsOpen = false;
            };

            timer.Start();

            // Close on click
            border.MouseDown += (s, e) =>
            {
                timer.Stop();
                popup.IsOpen = false;
            };
        });
    }
}