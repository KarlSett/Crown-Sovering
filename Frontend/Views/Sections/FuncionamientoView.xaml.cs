using System.Windows.Controls;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Sections
{
    /// <summary>Resumen operativo del día: ocupación, llegadas, salidas y movimientos.</summary>
    public partial class FuncionamientoView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;

        public FuncionamientoView()
        {
            InitializeComponent();
        }

        public void Cargar()
        {
            var resumen = servicio.ObtenerResumenOperativo();

            KpiOcupacion.Valor = Formato.Porcentaje(resumen.Ocupacion);
            KpiOcupacion.Pista = resumen.Ocupadas + " de " +
                                 (resumen.TotalHabitaciones - resumen.FueraDeServicio) + " operativas";
            KpiDisponibles.Valor = resumen.Disponibles.ToString();
            KpiLlegadas.Valor = resumen.Llegadas.ToString();
            KpiSalidas.Valor = resumen.Salidas.ToString();
            KpiEnCasa.Valor = resumen.EnCasa.ToString();
            KpiIngresos.Valor = Formato.Moneda(resumen.IngresosHoy);
            KpiSaldo.Valor = Formato.Moneda(resumen.SaldoPendiente);
            KpiActivas.Valor = resumen.ReservasActivas.ToString();

            CardEstados.Nota = resumen.TotalHabitaciones + " habitaciones · " + resumen.Ocupadas + " ocupadas";
            ListaEstados.ItemsSource = resumen.Estados;

            var llegadas = resumen.LlegadasHoy;
            if (llegadas.Count == 0)
                llegadas.Add(new LlegadaVista
                {
                    Huesped = "Sin llegadas previstas",
                    Detalle = "No hay check-ins programados para hoy.",
                    Hora = "—"
                });
            ListaLlegadas.ItemsSource = llegadas;

            var salidas = resumen.SalidasHoy;
            if (salidas.Count == 0)
                salidas.Add(new LlegadaVista
                {
                    Huesped = "Sin salidas previstas",
                    Detalle = "No hay check-outs programados para hoy.",
                    Hora = "—"
                });
            ListaSalidas.ItemsSource = salidas;

            GridMovimientos.ItemsSource = resumen.Movimientos;
        }
    }
}
