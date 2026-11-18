using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Crown__.Backend.Models;
using Crown__.Backend.Services;

namespace Crown__.Frontend.Views.Sections
{
    /// <summary>Resumen diario, semanal y trimestral de las cuentas de ingreso.</summary>
    public partial class IngresosView : UserControl
    {
        private readonly ServicioHotel servicio = ServicioHotel.Instancia;
        private bool cargando = true;
        private PeriodoReporte periodoActual = PeriodoReporte.Diario;
        private DateTime referencia = DateTime.Today;

        public IngresosView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            cargando = false;
            Cargar();
        }

        public void Cargar()
        {
            var resumen = servicio.ObtenerIngresos(periodoActual, referencia);

            TxtPeriodo.Text = resumen.EtiquetaPeriodo;
            KpiCobrado.Valor = Formato.Moneda(resumen.TotalAbonos);
            KpiFacturado.Valor = Formato.Moneda(resumen.TotalCargos);
            KpiPorCobrar.Valor = Formato.Moneda(resumen.Saldo);
            KpiMovimientos.Valor = resumen.Transacciones.ToString();
            CardDetalle.Titulo = resumen.TituloDetalle;
            GridDetalle.Columns[0].Header = resumen.ColumnaDetalle;

            ListaCuentas.ItemsSource = resumen.Cuentas;

            decimal mayor = Math.Max(1m, Math.Max(resumen.Efectivo, Math.Max(resumen.Tarjeta, resumen.Transferencia)));
            ListaMedios.ItemsSource = new List<MedioPagoVista>
            {
                new MedioPagoVista
                {
                    Nombre = "Efectivo", Monto = Formato.Moneda(resumen.Efectivo),
                    AnchoBarra = 210.0 * (double)(resumen.Efectivo / mayor),
                    Color = (Brush)new BrushConverter().ConvertFromString("#2E8B67")
                },
                new MedioPagoVista
                {
                    Nombre = "Tarjeta", Monto = Formato.Moneda(resumen.Tarjeta),
                    AnchoBarra = 210.0 * (double)(resumen.Tarjeta / mayor),
                    Color = (Brush)new BrushConverter().ConvertFromString("#2C4C63")
                },
                new MedioPagoVista
                {
                    Nombre = "Transferencia", Monto = Formato.Moneda(resumen.Transferencia),
                    AnchoBarra = 210.0 * (double)(resumen.Transferencia / mayor),
                    Color = (Brush)new BrushConverter().ConvertFromString("#C9A54B")
                }
            };

            TxtResumenPromedio.Text = Formato.Moneda(resumen.PromedioUnidad);
            TxtResumenMejor.Text = resumen.MejorCuenta == null ? "—" : resumen.MejorCuenta.Cuenta;
            TxtResumenMedios.Text = Formato.Moneda(resumen.TotalMedios);

            GridDetalle.ItemsSource = resumen.Detalle
                .Select(d => new DetalleFilaVista
                {
                    Etiqueta = d.Etiqueta,
                    Cargos = Formato.Moneda(d.Cargos),
                    Abonos = Formato.Moneda(d.Abonos),
                    Saldo = Formato.Moneda(d.Saldo)
                })
                .ToList();
        }

        private void OnPeriodoChanged(object sender, RoutedEventArgs e)
        {
            if (cargando)
                return;

            var opcion = sender as RadioButton;
            if (opcion == null)
                return;

            switch (opcion.Tag as string)
            {
                case "Semanal":
                    periodoActual = PeriodoReporte.Semanal;
                    break;
                case "Trimestral":
                    periodoActual = PeriodoReporte.Trimestral;
                    break;
                default:
                    periodoActual = PeriodoReporte.Diario;
                    break;
            }

            referencia = DateTime.Today;
            Cargar();
        }

        private void OnPeriodoAnterior(object sender, RoutedEventArgs e)
        {
            DesplazarPeriodo(-1);
        }

        private void OnPeriodoSiguiente(object sender, RoutedEventArgs e)
        {
            DesplazarPeriodo(1);
        }

        private void DesplazarPeriodo(int direccion)
        {
            switch (periodoActual)
            {
                case PeriodoReporte.Diario:
                    referencia = referencia.AddDays(direccion);
                    break;
                case PeriodoReporte.Semanal:
                    referencia = referencia.AddDays(7 * direccion);
                    break;
                default:
                    referencia = referencia.AddMonths(3 * direccion);
                    break;
            }

            Cargar();
        }

        private void OnPeriodoHoy(object sender, RoutedEventArgs e)
        {
            referencia = DateTime.Today;
            Cargar();
        }
    }
}
