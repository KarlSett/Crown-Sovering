using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Crown__.Frontend.Views
{
    // Presentation only. No authentication, navigation, persistence, or backend calls.
    public partial class LoginView : Page
    {
        private static readonly string[] Themes = { "Light", "Dark", "Brand" };
        private static readonly string[] ThemeLabels = { "Claro", "Oscuro", "Marca" };

        private readonly Storyboard posterMotion = new Storyboard();
        private int themeIndex;

        public LoginView() : this("Light") { }

        // theme: tema heredado del dashboard para mantener la apariencia al volver.
        public LoginView(string theme)
        {
            InitializeComponent();
            themeIndex = Array.IndexOf(Themes, theme);
            if (themeIndex < 0) themeIndex = 0;
            ApplyTheme();
            AddPosterAnimation(ScaleTransform.ScaleXProperty);
            AddPosterAnimation(ScaleTransform.ScaleYProperty);
        }

        private void AddPosterAnimation(DependencyProperty property)
        {
            var animation = new DoubleAnimation(1, 1.08, TimeSpan.FromSeconds(24))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(animation, PosterScale);
            Storyboard.SetTargetProperty(animation, new PropertyPath(property));
            posterMotion.Children.Add(animation);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            posterMotion.Begin(this, true);
            // An optional local MP4 replaces the photograph. No network requests.
            var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                        "Frontend", "Assets", "Video", "hospitality.mp4");
            if (File.Exists(videoPath))
            {
                HospitalityVideo.Source = new Uri(videoPath, UriKind.Absolute);
                HospitalityVideo.Play();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            posterMotion.Remove(this);
            HospitalityVideo.Close();
            HospitalityVideo.Opacity = 0;
        }

        private void UpdatePanelClip(object sender, SizeChangedEventArgs e)
        {
            HospitalityPanel.Clip = new RectangleGeometry(
                new Rect(0, 0, HospitalityPanel.ActualWidth, HospitalityPanel.ActualHeight), 22, 22);
        }

        private void OnVideoOpened(object sender, RoutedEventArgs e)
        {
            HospitalityVideo.Opacity = 1;
            posterMotion.Pause(this);
        }

        private void OnVideoEnded(object sender, RoutedEventArgs e)
        {
            HospitalityVideo.Position = TimeSpan.Zero;
            HospitalityVideo.Play();
        }

        private void OnVideoFailed(object sender, ExceptionRoutedEventArgs e)
        {
            HospitalityVideo.Opacity = 0;
            HospitalityVideo.Close();
        }

        // --- Tema cíclico: Claro -> Oscuro -> Marca ---
        private void OnThemeCycle(object sender, RoutedEventArgs e)
        {
            themeIndex = (themeIndex + 1) % Themes.Length;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Tag = Themes[themeIndex];
            if (ThemeButton != null)
                ThemeButton.Content = ThemeLabels[themeIndex];
        }

        // --- Mostrar / ocultar la contraseña ---
        private void OnEyeToggle(object sender, RoutedEventArgs e)
        {
            if (EyeToggle.IsChecked == true)
            {
                RevealBox.Text = TxtContrasena.Password;
                TxtContrasena.Visibility = Visibility.Collapsed;
                RevealBox.Visibility = Visibility.Visible;
                PassPlaceholder.Visibility = Visibility.Collapsed;
                RevealBox.Focus();
                RevealBox.CaretIndex = RevealBox.Text.Length;
            }
            else
            {
                if (TxtContrasena.Password != RevealBox.Text)
                    TxtContrasena.Password = RevealBox.Text;
                RevealBox.Visibility = Visibility.Collapsed;
                TxtContrasena.Visibility = Visibility.Visible;
                UpdatePassPlaceholder();
                TxtContrasena.Focus();
            }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdatePassPlaceholder();
            if (RevealBox.Visibility == Visibility.Visible && RevealBox.Text != TxtContrasena.Password)
                RevealBox.Text = TxtContrasena.Password;
        }

        private void OnRevealTextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtContrasena.Password != RevealBox.Text)
                TxtContrasena.Password = RevealBox.Text;
        }

        private void UpdatePassPlaceholder()
        {
            bool empty = string.IsNullOrEmpty(TxtContrasena.Password);
            bool revealing = RevealBox.Visibility == Visibility.Visible;
            PassPlaceholder.Visibility = (empty && !revealing) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Navega al panel principal heredando el tema elegido en el login.
        // Frontend only; no credential validation.
        private void OnIngresar(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null)
                NavigationService.Navigate(new DashboardView((string)Tag));
        }
    }
}
