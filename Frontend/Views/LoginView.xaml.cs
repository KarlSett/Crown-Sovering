using System;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Crown__.Frontend.Views
{
    // Presentation only. No authentication, navigation, persistence, or backend calls.
    public partial class LoginView : Page
    {
        private readonly Storyboard posterMotion = new Storyboard();
        private bool videoAvailable;
        private bool motionRunning;

        public LoginView()
        {
            InitializeComponent();
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
            MotionToggle.IsChecked = !SystemParameters.ClientAreaAnimation;
            posterMotion.Begin(this, true);
            motionRunning = true;
            // An optional local MP4 replaces the photograph. No network requests.
            var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                        "Frontend", "Assets", "Video", "hospitality.mp4");
            if (File.Exists(videoPath))
            {
                HospitalityVideo.Source = new Uri(videoPath, UriKind.Absolute);
                HospitalityVideo.Play();
            }
            ApplyMotionPreference();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            posterMotion.Remove(this);
            motionRunning = false;
            HospitalityVideo.Close();
            HospitalityVideo.Opacity = 0;
            videoAvailable = false;
        }

        private void UpdatePanelClip(object sender, SizeChangedEventArgs e)
        {
            HospitalityPanel.Clip = new RectangleGeometry(
                new Rect(0, 0, HospitalityPanel.ActualWidth, HospitalityPanel.ActualHeight), 22, 22);
        }

        private void OnVideoOpened(object sender, RoutedEventArgs e)
        {
            videoAvailable = true;
            HospitalityVideo.Opacity = 1;
            posterMotion.Pause(this);
            ApplyMotionPreference();
        }

        private void OnVideoEnded(object sender, RoutedEventArgs e)
        {
            HospitalityVideo.Position = TimeSpan.Zero;
            if (MotionToggle.IsChecked != true)
                HospitalityVideo.Play();
        }

        private void OnVideoFailed(object sender, ExceptionRoutedEventArgs e)
        {
            videoAvailable = false;
            HospitalityVideo.Opacity = 0;
            HospitalityVideo.Close();
            ApplyMotionPreference();
        }

        private void OnMotionToggle(object sender, RoutedEventArgs e)
        {
            ApplyMotionPreference();
        }

        private void ApplyMotionPreference()
        {
            bool paused = MotionToggle.IsChecked == true;
            AutomationProperties.SetName(MotionToggle,
                paused ? "Reanudar animación de fondo" : "Pausar animación de fondo");
            if (motionRunning)
            {
                if (paused || videoAvailable) posterMotion.Pause(this);
                else posterMotion.Resume(this);
            }
            if (videoAvailable)
            {
                if (paused) HospitalityVideo.Pause();
                else HospitalityVideo.Play();
            }
        }
    }
}