using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;
using Crown__.Frontend.Views.Dialogs;

namespace Crown__.Frontend.Views.Modules
{
    /// <summary>Tareas de limpieza e inspección, checklists, incidencias, órdenes de mantenimiento y objetos olvidados.</summary>
    public partial class LimpiezaView : UserControl
    {
        private static readonly string[] Severidades = { "Baja", "Media", "Alta", "Crítica" };

        private readonly LimpiezaService servicio = new LimpiezaService();
        private List<Habitacion> habitaciones = new List<Habitacion>();
        private List<EmpleadoVista> empleados = new List<EmpleadoVista>();
        private List<TareaLimpieza> tareas = new List<TareaLimpieza>();
        private TareaLimpieza seleccionada;
        private bool cambiando;

        public LimpiezaView()
        {
            InitializeComponent();
            CmbResultado.ItemsSource = new[] { "Aprobada", "Rechazada" };
            CmbResultado.SelectedIndex = 0;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Cargar();
        }

        public void Cargar()
        {
            try
            {
                habitaciones = ServicioHotel.Instancia.ListarHabitaciones();
                empleados = servicio.Empleados();
                tareas = servicio.Tareas();

                cambiando = true;
                ListaPendientes.ItemsSource = tareas.Where(t => t.Estado == "PENDIENTE").ToList();
                ListaEnCurso.ItemsSource = tareas.Where(t => t.Estado == "EN_CURSO").ToList();
                ListaCompletadas.ItemsSource = tareas.Where(t => t.Estado == "COMPLETADA").OrderByDescending(t => t.Programada).Take(30).ToList();
                cambiando = false;
                Seleccionar(null);

                var checklists = servicio.Checklists();
                GridChecklists.ItemsSource = checklists;
                CmbChecklist.ItemsSource = checklists;
                if (checklists.Count > 0 && CmbChecklist.SelectedItem == null) CmbChecklist.SelectedIndex = 0;
                GridItems.ItemsSource = servicio.Items();

                CmbTareaInspeccion.ItemsSource = tareas.Where(t => t.Tipo == "Inspección" && t.Estado != "COMPLETADA").ToList();
                CmbEmpleadoInspeccion.ItemsSource = empleados;
                CmbEmpleadoOrden.ItemsSource = empleados;
                CmbHabitacionObjeto.ItemsSource = habitaciones.Select(h => h.NumeroHabitacion).ToList();
                if (CmbHabitacionObjeto.SelectedItem == null && habitaciones.Count > 0) CmbHabitacionObjeto.SelectedIndex = 0;

                GridInspecciones.ItemsSource = servicio.Inspecciones();
                GridIncidencias.ItemsSource = servicio.Incidencias();
                GridOrdenes.ItemsSource = servicio.Ordenes();
                GridObjetos.ItemsSource = servicio.Objetos();
                TxtAviso.Text = string.Empty;
            }
            catch (Exception ex)
            {
                TxtAviso.Text = "No se pudo leer la base de datos: " + ex.Message;
            }
        }

        private void OnSeleccionTarea(object sender, SelectionChangedEventArgs e)
        {
            if (cambiando) return;
            var origen = (ListBox)sender;
            var tarea = origen.SelectedItem as TareaLimpieza;
            cambiando = true;
            foreach (var lista in new[] { ListaPendientes, ListaEnCurso, ListaCompletadas })
                if (lista != origen) lista.SelectedItem = null;
            cambiando = false;
            Seleccionar(tarea);
        }

        private void Seleccionar(TareaLimpieza tarea)
        {
            seleccionada = tarea;
            BtnAvanzar.IsEnabled = tarea != null && tarea.Estado != "COMPLETADA";
            BtnAvanzar.Content = tarea != null && tarea.Estado == "PENDIENTE" ? "Iniciar tarea" : "Completar tarea";
        }

        private void Aplicar(Func<string> operacion, string exito)
        {
            string error;
            try { error = operacion(); }
            catch (Exception ex) { error = "No se pudo guardar.\n" + ex.Message; }

            if (error != null) { MessageBox.Show(error, "Limpieza y mantenimiento", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            Backend.Data.DatosHotel.Refrescar();
            Cargar();
            TxtAviso.Text = exito;
        }

        private string Actor { get { return Sesion.UsuarioId.ToString(); } }

        private int IdHabitacion(string numero)
        {
            var h = habitaciones.FirstOrDefault(x => x.NumeroHabitacion == numero);
            return h == null ? 0 : h.Id;
        }

        private void OnNuevaTarea(object sender, RoutedEventArgs e)
        {
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Nueva tarea",
                Campo.Lista("Habitación", habitaciones.Select(h => (object)h.NumeroHabitacion)),
                Campo.Lista("Tipo", new object[] { "Limpieza", "Inspección" }),
                Campo.Fecha("Fecha programada", DateTime.Today),
                Campo.Lista("Asignar a (opcional)", new object[] { "Sin asignar" }.Concat(empleados)));
            if (v == null) return;

            string tipo = (string)v[1] == "Inspección" ? "INSPECCION" : "LIMPIEZA";
            var fecha = ((DateTime?)v[2] ?? DateTime.Today).Date.AddHours(DateTime.Now.Hour);
            Aplicar(() => servicio.NuevaTarea(Actor, IdHabitacion((string)v[0]), tipo, fecha, v[3] as EmpleadoVista), "Tarea creada.");
        }

        private void OnAvanzar(object sender, RoutedEventArgs e)
        {
            var tarea = seleccionada;
            Aplicar(() => servicio.AvanzarTarea(Actor, tarea), "Tarea actualizada.");
        }

        private void OnNuevaIncidencia(object sender, RoutedEventArgs e)
        {
            var v = FormularioWindow.Pedir(Window.GetWindow(this), "Registrar incidencia",
                Campo.Lista("Habitación", habitaciones.Select(h => (object)h.NumeroHabitacion)),
                Campo.Lista("Severidad", Severidades), Campo.Memo("Descripción"));
            if (v == null) return;
            Aplicar(() => servicio.RegistrarIncidencia(Actor, (string)v[0], (string)v[1], (string)v[2]), "Incidencia registrada.");
        }

        private void OnGuardarChecklist(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.GuardarChecklist(Actor, TxtChecklistNombre.Text, TxtChecklistDescripcion.Text), "Checklist guardado.");
            TxtChecklistNombre.Clear();
            TxtChecklistDescripcion.Clear();
        }

        private void OnGuardarItem(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.GuardarItem(Actor, CmbChecklist.SelectedItem as ChecklistVista, TxtItemDescripcion.Text), "Ítem guardado.");
            TxtItemDescripcion.Clear();
        }

        private void OnRegistrarInspeccion(object sender, RoutedEventArgs e)
        {
            bool aprobada = (CmbResultado.SelectedItem as string) == "Aprobada";
            Aplicar(() => servicio.RegistrarInspeccion(Actor, CmbTareaInspeccion.SelectedItem as TareaLimpieza,
                                                       CmbEmpleadoInspeccion.SelectedItem as EmpleadoVista, aprobada, TxtObservaciones.Text), "Inspección registrada.");
            TxtObservaciones.Clear();
        }

        private void OnResolver(object sender, RoutedEventArgs e)
        {
            var a = GridIncidencias.SelectedItem as Averia;
            if (a == null) { MessageBox.Show("Selecciona una incidencia de la tabla.", "Incidencias", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            Aplicar(() => servicio.ResolverIncidencia(Actor, a.Id), "Incidencia resuelta.");
        }

        private void OnAsignarOrden(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.AsignarOrden(Actor, GridOrdenes.SelectedItem as OrdenVista, CmbEmpleadoOrden.SelectedItem as EmpleadoVista), "Orden asignada.");
        }

        private void OnRegistrarObjeto(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.RegistrarObjeto(Actor, IdHabitacion(CmbHabitacionObjeto.SelectedItem as string), TxtObjeto.Text), "Objeto registrado.");
            TxtObjeto.Clear();
        }

        private void OnEntregarObjeto(object sender, RoutedEventArgs e)
        {
            Aplicar(() => servicio.EntregarObjeto(Actor, GridObjetos.SelectedItem as ObjetoOlvidadoVista), "Objeto entregado.");
        }
    }
}
