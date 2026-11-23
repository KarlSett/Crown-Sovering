using System.Collections.Generic;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class FacturacionService
    {
        private static readonly string[] Roles = { "Recepcionista", "Administrador", "Gerente" };

        private readonly FacturacionRepository repositorio = new FacturacionRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        public List<FolioItem> Folios() { return repositorio.Folios(); }
        public List<CargoVista> Cargos(int folioId) { return repositorio.Cargos(folioId); }
        public List<ConceptoVista> Conceptos() { return repositorio.Conceptos(); }
        public List<FacturaVista> Facturas(int folioId) { return repositorio.Facturas(folioId); }
        public List<NotaCreditoVista> NotasCredito(int folioId) { return repositorio.NotasCredito(folioId); }

        // Cada operación devuelve null si salió bien, o el motivo del problema.
        public string AgregarCargo(string actorId, FolioItem folio, ConceptoVista concepto, string descripcion, decimal cantidad, decimal precio)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, Roles);
            if (error != null) return error;
            if (folio == null) return "Selecciona un folio.";
            if (concepto == null) return "Selecciona el concepto del cargo.";
            if (cantidad <= 0) return "La cantidad debe ser mayor que cero.";
            if (precio <= 0) return "El precio debe ser mayor que cero.";

            error = repositorio.AgregarCargo(folio.FolioId, concepto.Id, descripcion, cantidad, precio);
            if (error == null) auditoria.Registrar(actor, "Facturación", "Registrar cargo", folio.Reserva + " · " + concepto.Nombre);
            return error;
        }

        public string NuevoFolio(string actorId, FolioItem folio, string nombre)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, Roles);
            if (error != null) return error;
            if (folio == null) return "Selecciona el folio de la estancia.";
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del folio.";

            error = repositorio.NuevoFolio(folio.FolioId, nombre.Trim());
            if (error == null) auditoria.Registrar(actor, "Facturación", "Nuevo folio", folio.Reserva + " · " + nombre.Trim());
            return error;
        }

        public string TransferirCargo(string actorId, FolioItem origen, CargoVista cargo, FolioItem destino)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, Roles);
            if (error != null) return error;
            if (origen == null || cargo == null) return "Selecciona el folio y el cargo a transferir.";
            if (destino == null) return "Selecciona el folio de destino.";

            error = repositorio.TransferirCargo(cargo.Id, destino.FolioId, actor);
            if (error == null) auditoria.Registrar(actor, "Facturación", "Transferir cargo", origen.Reserva + " → " + destino.Reserva + " · " + cargo.Concepto);
            return error;
        }

        public string GuardarConcepto(string actorId, string nombre, string categoria)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Administrador", "Gerente");
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del concepto.";
            if (repositorio.ExisteConcepto(nombre.Trim())) return "Ya existe el concepto " + nombre.Trim() + ".";

            repositorio.GuardarConcepto(nombre.Trim(), categoria);
            auditoria.Registrar(actor, "Facturación", "Crear concepto", nombre.Trim());
            return null;
        }

        public string EmitirFactura(string actorId, FolioItem folio)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, Roles);
            if (error != null) return error;
            if (folio == null) return "Selecciona un folio.";

            error = repositorio.EmitirFactura(folio.FolioId);
            if (error == null) auditoria.Registrar(actor, "Facturación", "Emitir factura", folio.Reserva);
            return error;
        }

        public string CrearNotaCredito(string actorId, FacturaVista factura, string motivo, decimal monto)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Administrador", "Gerente");
            if (error != null) return error;
            if (factura == null) return "Selecciona la factura.";
            if (monto <= 0) return "El monto debe ser mayor que cero.";

            error = repositorio.CrearNotaCredito(factura, motivo, monto);
            if (error == null) auditoria.Registrar(actor, "Facturación", "Nota de crédito", factura.Numero + " · " + monto);
            return error;
        }
    }
}
