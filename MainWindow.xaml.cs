using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;

namespace Crown__
{
    public partial class MainWindow : Window
    {
        private bool updatingWindowState;
        private bool fullScreen;
        private WindowChrome normalChrome;
        private ResizeMode normalResizeMode;
        private Rect normalBounds;
        public MainWindow()
        {
            InitializeComponent();
            PreviewKeyDown += OnWindowKeyDown;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (updatingWindowState) return;
            if (WindowState == WindowState.Maximized && !fullScreen)
            {
                updatingWindowState = true;
                try
                {
                    normalBounds = RestoreBounds;
                    normalChrome = WindowChrome.GetWindowChrome(this);
                    normalResizeMode = ResizeMode;
                    // WindowChrome reserves the work area. A borderless, non-resizable
                    // maximized window uses the full monitor, including the taskbar area.
                    WindowState = WindowState.Normal;
                    WindowChrome.SetWindowChrome(this, null);
                    ResizeMode = ResizeMode.NoResize;
                    fullScreen = true;
                    WindowState = WindowState.Maximized;
                }
                finally { updatingWindowState = false; }
            }
            else if (WindowState == WindowState.Normal && fullScreen)
            {
                updatingWindowState = true;
                try
                {
                    fullScreen = false;
                    ResizeMode = normalResizeMode;
                    WindowChrome.SetWindowChrome(this, normalChrome);
                    if (!normalBounds.IsEmpty)
                    {
                        Left = normalBounds.Left; Top = normalBounds.Top;
                        Width = normalBounds.Width; Height = normalBounds.Height;
                    }
                }
                finally { updatingWindowState = false; }
            }
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                ToggleMaximizeWindow(this, new RoutedEventArgs()); e.Handled = true;
            }
            else if (e.Key == Key.Escape && fullScreen)
            {
                SystemCommands.RestoreWindow(this); e.Handled = true;
            }
        }

        // Window presentation only. Authentication is intentionally absent.
        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void ToggleMaximizeWindow(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }
    }
}
