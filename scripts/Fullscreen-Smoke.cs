using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Windows.Threading;

internal static class FullscreenSmoke
{
    [StructLayout(LayoutKind.Sequential)] struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [StructLayout(LayoutKind.Sequential)] struct Point { public int X, Y; }
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr window, uint flags);
    static void Pump()
    {
        Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        System.Threading.Thread.Sleep(250);
        Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
    }
    static void Key(Window window, System.Windows.Input.Key key)
    {
        var args = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), Environment.TickCount, key);
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        window.RaiseEvent(args);
    }
    [STAThread] static int Main()
    {
        Crown__.MainWindow window = null;
        try
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            foreach (string resource in new[] { "Frontend/Themes/HotelPalette.xaml", "Frontend/Styles/ModernControls.xaml", "Frontend/Styles/LiquidGlassStyles.xaml", "Frontend/Styles/DashboardStyles.xaml", "Frontend/Styles/OperationsStyles.xaml" })
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Crown &;component/" + resource, UriKind.Relative) });
            window = new Crown__.MainWindow();
            ((Frame)window.FindName("LoginFrame")).Source = null;
            window.Show(); Pump();
            var original = new System.Windows.Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight);
            var hwnd = new WindowInteropHelper(window).Handle;
            for (int cycle = 0; cycle < 3; cycle++)
            {
                var maximize = (Button)((StackPanel)((Grid)window.Content).Children[1]).Children[1];
                if (cycle == 0) maximize.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                else if (cycle == 1) Key(window, System.Windows.Input.Key.F11);
                else window.WindowState = WindowState.Maximized;
                Pump();
                var monitor = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
                GetMonitorInfo(MonitorFromWindow(hwnd, 2), ref monitor);
                Rect actual; GetWindowRect(hwnd, out actual);
                if (actual.Left != monitor.Monitor.Left || actual.Top != monitor.Monitor.Top || actual.Right != monitor.Monitor.Right || actual.Bottom != monitor.Monitor.Bottom)
                    throw new Exception("Fullscreen rectangle does not match the whole monitor: " + actual.Left + "," + actual.Top + " - " + actual.Right + "," + actual.Bottom);
                var bottom = WindowFromPoint(new Point { X = (actual.Left + actual.Right) / 2, Y = actual.Bottom - 2 });
                if (GetAncestor(bottom, 2) != hwnd) throw new Exception("Taskbar area is not covered by the active application.");
                if (window.Topmost) throw new Exception("Fullscreen must not force always-on-top.");
                if (cycle == 1)
                {
                    window.WindowState = WindowState.Minimized; Pump();
                    SystemCommands.RestoreWindow(window); Pump();
                    if (window.WindowState != WindowState.Maximized) throw new Exception("Minimize/restore lost fullscreen.");
                }
                if (cycle == 0) Key(window, System.Windows.Input.Key.Escape);
                else if (cycle == 1) maximize.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                else window.WindowState = WindowState.Normal;
                Pump();
                if (WindowChrome.GetWindowChrome(window) == null || window.ResizeMode != ResizeMode.CanResize)
                    throw new Exception("Resizable window chrome was not restored.");
                if (Math.Abs(window.Width - original.Width) > 1 || Math.Abs(window.Height - original.Height) > 1 || Math.Abs(window.Left - original.Left) > 1 || Math.Abs(window.Top - original.Top) > 1)
                    throw new Exception("Original window bounds were not restored.");
            }
            Console.WriteLine("PASS: full monitor bounds, taskbar covered, three restore cycles and minimize/restore; no always-on-top.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (window != null) window.Close(); }
    }
}
