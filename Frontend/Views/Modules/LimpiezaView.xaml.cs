using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Controls;

namespace Crown__.Frontend.Views.Modules
{
    public partial class LimpiezaView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private TareaOperativa seleccion;
        private bool cargando;
        public LimpiezaView() { InitializeComponent(); }
        private void OnLoaded(object sender, RoutedEventArgs e) { Cargar(); }
        private void Cargar()
        {
            seleccion = null;
            cargando = true;
            var tareas = servicio.ListarTareas();
            LstPendientes.ItemsSource = tareas.FindAll(t => t.Estado == "Pendiente");
            LstEnCurso.ItemsSource = tareas.FindAll(t => t.Estado == "En curso");
            LstCompletadas.ItemsSource = tareas.FindAll(t => t.Estado == "Completada");
            GridChecklists.ItemsSource = Crown__.Backend.Data.DatosHotel.Checklists.ToList();
            CmbChecklist.ItemsSource = Crown__.Backend.Data.DatosHotel.Checklists.ToList();
            GridItemsChecklist.ItemsSource = Crown__.Backend.Data.DatosHotel.ItemsChecklist.ToList();
            cargando = false;
        }
        private void OnSeleccionTarea(object sender, SelectionChangedEventArgs e)
        {
            if (cargando) return;
            var lista = sender as ListBox;
            if (lista != null && lista.SelectedItem != null) seleccion = lista.SelectedItem as TareaOperativa;
            LstPendientes.SelectedItem = sender == LstPendientes ? LstPendientes.SelectedItem : null;
            LstEnCurso.SelectedItem = sender == LstEnCurso ? LstEnCurso.SelectedItem : null;
            LstCompletadas.SelectedItem = sender == LstCompletadas ? LstCompletadas.SelectedItem : null;
        }
        private void Crear(string tipo)
        {
            var datos = DialogoRapido.Mostrar(Window.GetWindow(this), tipo == "Limpieza" ? "Nueva tarea de limpieza" : "Registrar " + tipo.ToLower(),
                "Número de habitación", "Descripción");
            if (datos == null) return;
            try { servicio.CrearTarea(datos[0], tipo, datos[1]); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Operación", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnNuevaTarea(object sender, RoutedEventArgs e) { Crear("Limpieza"); }
        private void OnIncidencia(object sender, RoutedEventArgs e) { Crear("Incidencia"); }
        private void OnNuevaOrden(object sender, RoutedEventArgs e) { Crear("Mantenimiento"); }
        private void OnAvanzar(object sender, RoutedEventArgs e)
        {
            if (seleccion == null) { MessageBox.Show("Selecciona una tarea del tablero.", "Limpieza", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            servicio.AvanzarTarea(seleccion.Id); seleccion = null; Cargar();
        }
        private void OnGuardarChecklist(object sender, RoutedEventArgs e)
        {
            try { servicio.CrearChecklist(TxtChecklist.Text, TxtDescripcionChecklist.Text); TxtChecklist.Clear(); TxtDescripcionChecklist.Clear(); Cargar(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Checklists", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
        private void OnGuardarItemChecklist(object sender, RoutedEventArgs e)
        {
            var checklist = CmbChecklist.SelectedItem as ChecklistDemo;
            if (checklist == null) { MessageBox.Show("Crea o selecciona un checklist.", "Checklists", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            try { servicio.CrearItemChecklist(checklist.Id, TxtItemChecklist.Text); TxtItemChecklist.Clear(); GridItemsChecklist.ItemsSource = null; GridItemsChecklist.ItemsSource = Crown__.Backend.Data.DatosHotel.ItemsChecklist.ToList(); }
            catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Checklists", MessageBoxButton.OK, MessageBoxImage.Information); }
        }
    }
}
