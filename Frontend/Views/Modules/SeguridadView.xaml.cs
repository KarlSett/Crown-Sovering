using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Data;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Modules
{
    public partial class SeguridadView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        public SeguridadView() { InitializeComponent(); }
        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void Cargar()
        {
            GridEmpleados.ItemsSource = DatosHotel.Empleados.ToList(); GridRoles.ItemsSource = DatosHotel.Roles.ToList();
            GridAsignaciones.ItemsSource = DatosHotel.AsignacionesRoles.ToList();
            CmbEmpleado.ItemsSource = DatosHotel.Empleados.ToList(); CmbRol.ItemsSource = DatosHotel.Roles.ToList();
            if (CmbEmpleado.Items.Count > 0 && CmbEmpleado.SelectedIndex < 0) CmbEmpleado.SelectedIndex = 0;
            if (CmbRol.Items.Count > 0 && CmbRol.SelectedIndex < 0) CmbRol.SelectedIndex = 0;
        }
        private void OnGuardarEmpleado(object sender, RoutedEventArgs e)
        {
            try { servicio.CrearEmpleado(TxtNombreEmpleado.Text, TxtApellidoEmpleado.Text, TxtCorreoEmpleado.Text, TxtTelefonoEmpleado.Text); TxtNombreEmpleado.Clear(); TxtApellidoEmpleado.Clear(); TxtCorreoEmpleado.Clear(); TxtTelefonoEmpleado.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Empleados", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnGuardarRol(object sender, RoutedEventArgs e)
        {
            try { servicio.CrearRol(TxtRolNombre.Text, TxtRolDescripcion.Text); TxtRolNombre.Clear(); TxtRolDescripcion.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Roles", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnAsignarRol(object sender, RoutedEventArgs e)
        {
            var empleado = CmbEmpleado.SelectedItem as EmpleadoDemo; var rol = CmbRol.SelectedItem as RolDemo;
            if (empleado == null || rol == null) { MessageBox.Show("Crea o selecciona un empleado y un rol.", "Asignación", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.AsignarRolEmpleado(empleado.Id, rol.Nombre); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Asignación", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
    }
}
