using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Usuarios, empleados, roles y asignación de roles.</summary>
    public partial class SeguridadView : UserControl
    {
        private readonly SeguridadService servicio = new SeguridadService();

        public SeguridadView()
        {
            InitializeComponent();
            CmbEstado.ItemsSource = new[] { "Activo", "Inactivo" };
            CmbEstado.SelectedIndex = 0;
        }

        private string Actor { get { return Sesion.UsuarioId.ToString(); } }

        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }

        public void Cargar()
        {
            try
            {
                var usuarios = servicio.Usuarios();
                var empleados = servicio.Empleados();
                var roles = servicio.Roles();
                GridUsuarios.ItemsSource = usuarios;
                GridEmpleados.ItemsSource = empleados;
                GridRoles.ItemsSource = roles;
                GridAsignaciones.ItemsSource = servicio.Asignaciones();
                CmbEmpleado.ItemsSource = new object[] { "Sin empleado" }.Concat(empleados).ToList();
                if (CmbEmpleado.SelectedItem == null) CmbEmpleado.SelectedIndex = 0;
                CmbUsuarioRol.ItemsSource = usuarios;
                CmbRol.ItemsSource = roles;
                if (CmbUsuarioRol.SelectedItem == null && usuarios.Count > 0) CmbUsuarioRol.SelectedIndex = 0;
                if (CmbRol.SelectedItem == null && roles.Count > 0) CmbRol.SelectedIndex = 0;
                TxtAviso.Text = string.Empty;
            }
            catch (Exception ex)
            {
                TxtAviso.Text = "No se pudo leer la base de datos: " + ex.Message;
            }
        }

        private void Aplicar(Func<string> operacion, string exito)
        {
            string error;
            try { error = operacion(); }
            catch (Exception ex) { error = "No se pudo guardar.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Cargar();
            TxtAviso.Text = exito;
        }

        private UsuarioGestion Usuario
        {
            get
            {
                var u = GridUsuarios.SelectedItem as UsuarioGestion;
                if (u == null) MessageBox.Show("Selecciona un usuario de la tabla.", "Seguridad", MessageBoxButton.OK, MessageBoxImage.Information);
                return u;
            }
        }

        private void OnGuardarUsuario(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.NuevoUsuario(Actor, TxtUsuario.Text, CmbEmpleado.SelectedItem as EmpleadoFicha, TxtClave.Password, (CmbEstado.SelectedItem as string) == "Activo"), "Usuario creado.");
            TxtUsuario.Clear();
            TxtClave.Clear();
        }

        private void OnCambiarClave(object sender, RoutedEventArgs e)
        {
            var u = Usuario;
            if (u == null) return;
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Nueva contraseña · " + u.Username, Campo.Clave("Contraseña (mínimo 8, con letras y números)"));
            if (v == null) return;
            Aplicar(() => servicio.CambiarPassword(Actor, u, (string)v[0]), "Contraseña actualizada.");
        }

        private void OnAlternarEstado(object sender, RoutedEventArgs e)
        {
            var u = Usuario;
            if (u == null) return;
            bool activar = u.Estado != "Activo";
            if (MessageBox.Show("¿" + (activar ? "Activar" : "Desactivar") + " la cuenta " + u.Username + "?", "Seguridad", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            Aplicar(() => servicio.CambiarEstado(Actor, u, activar), "Cuenta actualizada.");
        }

        private void OnGuardarEmpleado(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.NuevoEmpleado(Actor, TxtEmpNombre.Text, TxtEmpApellido.Text, TxtEmpCorreo.Text, TxtEmpTelefono.Text), "Empleado guardado.");
            TxtEmpNombre.Clear(); TxtEmpApellido.Clear(); TxtEmpCorreo.Clear(); TxtEmpTelefono.Clear();
        }

        private void OnGuardarRol(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.NuevoRol(Actor, TxtRolNombre.Text, TxtRolDescripcion.Text), "Rol guardado.");
            TxtRolNombre.Clear(); TxtRolDescripcion.Clear();
        }

        private void OnAsignar(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.AsignarRol(Actor, CmbUsuarioRol.SelectedItem as UsuarioGestion, CmbRol.SelectedItem as RolGestion), "Rol asignado.");
        }

        private void OnQuitar(object sender, RoutedEventArgs e)
        {
            var a = GridAsignaciones.SelectedItem as AsignacionVista;
            if (a == null) { MessageBox.Show("Selecciona una asignación de la tabla.", "Seguridad", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            if (MessageBox.Show("¿Quitar el rol " + a.Rol + " a " + a.Usuario + "?", "Seguridad", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            Aplicar(() => servicio.QuitarRol(Actor, a), "Rol quitado.");
        }
    }
}
