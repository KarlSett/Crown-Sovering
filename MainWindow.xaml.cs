using System.Windows;
using System.Windows.Input;
using Crown__.Frontend.Views;

namespace Crown__
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var login = LoginFrame.Content as LoginView;
            if (login != null)
            {
                login.HandleShortcut(e);
                return;
            }
            var dashboard = LoginFrame.Content as DashboardView;
            if (dashboard != null)
                dashboard.HandleShortcut(e);
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