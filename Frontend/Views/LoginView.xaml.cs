using System;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views
{
    // Acceso con usuario y contraseña de la base de datos.
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

        private void OnIngresar(object sender, RoutedEventArgs e)
        {
            IniciarSesion(TxtUsuario.Text, TxtContrasena.Password, false);
        }

        private void OnAccesoAdmin(object sender, RoutedEventArgs e)
        {
            // Credential for the administrator demo account created by datos_iniciales.sql.
            IniciarSesion("admin", "Admin#2026", true);
        }

        private void IniciarSesion(string usuario, string contrasena, bool accesoRapidoAdmin)
        {
            ResultadoLogin resultado;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                resultado = new AuthService().Ingresar(usuario, contrasena);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo conectar con la base de datos.\n" + ex.Message,
                                "Inicio de sesión", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            if (!resultado.Exito)
            {
                string mensaje = resultado.Mensaje;
                if (accesoRapidoAdmin)
                    mensaje += "\n\nEl acceso rápido requiere que scripts/datos_iniciales.sql haya creado la cuenta admin con su contraseña inicial.";
                MessageBox.Show(mensaje, "Inicio de sesión", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Sesion.Iniciar(resultado.Usuario);
            TxtContrasena.Clear();

            if (NavigationService != null)
                NavigationService.Navigate(new Uri("/Frontend/Views/DashboardView.xaml", UriKind.Relative));
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
