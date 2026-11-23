using System;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    // Servicios que se cobran al folio del huésped (comidas y bebidas, spa, lavandería) y el inventario de productos.
    public class ServicioService
    {
        private static readonly string[] RolesDeServicio = { "Recepcionista", "Administrador", "Gerente", "Servicios" };

        private readonly ServicioRepository servicios = new ServicioRepository();
        private readonly FolioRepository folios = new FolioRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        private readonly ConsumosRepository consumos = new ConsumosRepository();

        public System.Collections.Generic.List<Crown__.Backend.Models.Producto> Productos() { return servicios.Productos(); }
        public System.Collections.Generic.List<Crown__.Backend.Models.ConsumoLinea> Consumos(int reservaId) { return consumos.Consumos(reservaId); }
        public System.Collections.Generic.List<Crown__.Backend.Models.ServicioVista> Servicios() { return consumos.Servicios(); }
        public System.Collections.Generic.List<Crown__.Backend.Models.CitaVista> Citas() { return consumos.Citas(); }
        public System.Collections.Generic.List<Crown__.Backend.Models.MovimientoInventarioVista> MovimientosInventario() { return consumos.MovimientosInventario(); }

        // Cobra el consumo de una estancia en efectivo contra su folio.
        public string CobrarConsumo(string actorId, string codigoReserva, decimal total)
        {
            if (total <= 0) return "No hay consumo para cobrar.";
            return new FolioService().RegistrarPago(actorId, codigoReserva, total, "Efectivo", "Cobro de consumo");
        }

        public string NuevoProducto(string actorId, string nombre, string categoria, decimal precio, int stock)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Administrador", "Gerente");
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del producto.";
            if (precio <= 0) return "El precio debe ser mayor que cero.";
            if (stock < 0) return "El stock no puede ser negativo.";
            if (consumos.ExisteProducto(nombre.Trim())) return "Ya existe el producto " + nombre.Trim() + ".";

            consumos.NuevoProducto(nombre.Trim(), categoria, precio, stock);
            auditoria.Registrar(actor, "Inventario", "Crear producto", nombre.Trim());
            return null;
        }

        public string NuevoServicio(string actorId, string nombre, string tipo, decimal precio, bool activo)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Administrador", "Gerente");
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del servicio.";
            if (string.IsNullOrWhiteSpace(tipo)) return "Elige el tipo de servicio.";
            if (precio <= 0) return "El precio debe ser mayor que cero.";
            if (consumos.ExisteServicio(nombre.Trim())) return "Ya existe el servicio " + nombre.Trim() + ".";

            consumos.NuevoServicio(nombre.Trim(), tipo, precio, activo);
            auditoria.Registrar(actor, "Servicios", "Crear servicio", nombre.Trim());
            return null;
        }

        // Devuelve null si todo salió bien, o el motivo del problema.
        public string ConsumirProducto(string actorId, string codigoReserva, int productoId, int cantidad)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, RolesDeServicio);
            if (error != null) return error;
            if (cantidad < 1) return "La cantidad debe ser al menos 1.";

            error = servicios.ConsumirProducto(codigoReserva, productoId, cantidad);
            if (error == null) auditoria.Registrar(actor, "Comidas y bebidas", "Registrar consumo", codigoReserva);
            return error;
        }

        public string CargarServicioSpa(string actorId, string codigoReserva, int servicioId)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, RolesDeServicio);
            if (error != null) return error;

            error = servicios.CargarServicioSpa(codigoReserva, servicioId);
            if (error == null) auditoria.Registrar(actor, "Spa y gimnasio", "Cargar servicio", codigoReserva);
            return error;
        }

        public string CargarLavanderia(string actorId, string codigoReserva, string detalle, decimal importe)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, RolesDeServicio);
            if (error != null) return error;
            if (importe <= 0) return "El importe debe ser mayor que cero.";
            if (string.IsNullOrWhiteSpace(detalle)) return "Describe las prendas.";

            string descripcion = "Lavandería · " + detalle.Trim();
            if (descripcion.Length > 100) descripcion = descripcion.Substring(0, 100);
            if (!folios.RegistrarCargo(codigoReserva, "Lavanderia", descripcion, importe, out error)) return error;

            auditoria.Registrar(actor, "Lavandería", "Cargar orden", codigoReserva);
            return null;
        }

        // movimiento: "Ingreso a almacén" suma al stock y "Registro de merma" resta.
        public string AjustarStock(string actorId, int productoId, string movimiento, int cantidad)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Administrador", "Gerente");
            if (error != null) return error;
            if (cantidad < 1) return "La cantidad debe ser al menos 1.";

            error = servicios.AjustarStock(productoId, movimiento == "Registro de merma" ? -cantidad : cantidad);
            if (error == null) auditoria.Registrar(actor, "Inventario", movimiento, "Producto " + productoId);
            return error;
        }
    }
}
