using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Viola.Avalonia.GuiUtils
{
    public static class CAlert
    {
        internal static Func<Window, string, string, Task<bool>>? ConfirmOverride;
        internal static Func<Window, string, string, Task>? InfoOverride;
        internal static Func<Window, string, string, Task<string>>? InputOverride;

        private static Window CreateBaseWindow(string title, Window? owner)
        {
            return new Window
            {
                Title = title,
                Width = 400,
                CanResize = false,
                ShowInTaskbar = false,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };
        }

        public static Task<bool> ShowConfirmAsync(Window owner, string message, string title)
        {
            if (ConfirmOverride != null)
            {
                return ConfirmOverride(owner, message, title);
            }
            return ShowConfirmCore(owner, message, title);
        }

        private static Task<bool> ShowConfirmCore(Window owner, string message, string title)
        {
            var tcs = new TaskCompletionSource<bool>();
            var window = CreateBaseWindow(title, owner);

            var text = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(16, 16, 16, 8),
                MaxWidth = 360
            };
            var yesButton = new Button { Content = "Yes", Width = 90 };
            var noButton = new Button { Content = "No", Width = 90 };
            yesButton.Click += (_, _) =>
            {
                tcs.TrySetResult(true);
                window.Close();
            };
            noButton.Click += (_, _) =>
            {
                tcs.TrySetResult(false);
                window.Close();
            };
            window.Closed += (_, _) => tcs.TrySetResult(false);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Margin = new Thickness(16, 0, 16, 16)
            };
            buttons.Children.Add(yesButton);
            buttons.Children.Add(noButton);

            var panel = new StackPanel();
            panel.Children.Add(text);
            panel.Children.Add(buttons);
            window.Content = panel;

            window.ShowDialog(owner);
            return tcs.Task;
        }

        public static Task ShowInfoAsync(Window owner, string message, string title)
        {
            if (InfoOverride != null)
            {
                return InfoOverride(owner, message, title);
            }
            return ShowInfoCore(owner, message, title);
        }

        private static Task ShowInfoCore(Window owner, string message, string title)
        {
            var tcs = new TaskCompletionSource<bool>();
            var window = CreateBaseWindow(title, owner);

            var text = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(16, 16, 16, 8),
                MaxWidth = 360
            };
            var okButton = new Button { Content = "OK", Width = 90 };
            okButton.Click += (_, _) =>
            {
                tcs.TrySetResult(true);
                window.Close();
            };
            window.Closed += (_, _) => tcs.TrySetResult(true);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(16, 0, 16, 16)
            };
            buttons.Children.Add(okButton);

            var panel = new StackPanel();
            panel.Children.Add(text);
            panel.Children.Add(buttons);
            window.Content = panel;

            window.ShowDialog(owner);
            return tcs.Task;
        }

        public static Task<string> ShowInputAsync(Window owner, string message, string title)
        {
            if (InputOverride != null)
            {
                return InputOverride(owner, message, title);
            }
            return ShowInputCore(owner, message, title);
        }

        private static Task<string> ShowInputCore(Window owner, string message, string title)
        {
            var tcs = new TaskCompletionSource<string>();
            var window = CreateBaseWindow(title, owner);

            var text = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(16, 16, 16, 8),
                MaxWidth = 360
            };
            var input = new TextBox { Margin = new Thickness(16, 0, 16, 8) };
            input.AttachedToVisualTree += (_, _) => input.Focus();
            var okButton = new Button { Content = "OK", Width = 90 };
            var cancelButton = new Button { Content = "Cancel", Width = 90 };
            okButton.Click += (_, _) =>
            {
                tcs.TrySetResult(input.Text?.Trim() ?? "");
                window.Close();
            };
            cancelButton.Click += (_, _) =>
            {
                tcs.TrySetResult("");
                window.Close();
            };
            window.Closed += (_, _) => tcs.TrySetResult("");

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Margin = new Thickness(16, 0, 16, 16)
            };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);

            var panel = new StackPanel();
            panel.Children.Add(text);
            panel.Children.Add(input);
            panel.Children.Add(buttons);
            window.Content = panel;

            window.ShowDialog(owner);
            return tcs.Task;
        }
    }
}