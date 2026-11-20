using System;
using System.Collections.Generic;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class ClientesService
    {
        public static readonly string[] TiposDeConsentimiento = { "Tratamiento de datos", "Marketing", "Compartir con terceros" };

        private static readonly string[] Roles = { "Recepcionista", "Administrador", "Gerente" };

        private readonly ClientesRepository clientes = new ClientesRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        public List<ClienteVista> Listar()
        {
            return clientes.Listar();
        }

        public List<ConsentimientoVista> Consentimientos(int huespedId)
        {
            return clientes.Consentimientos(huespedId);
        }

        // Crea (Id = 0) o actualiza un cliente. Devuelve null si todo salió bien, o el motivo del problema.
        public string Guardar(string actorId, ClienteVista datos)
        {
            int id;
            return Guardar(actorId, datos, out id);
        }

        public string Guardar(string actorId, ClienteVista datos, out int id)
        {
            id = datos.Id;
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, Roles);
            if (error != null) return error;

            if (string.IsNullOrWhiteSpace(datos.Nombre)) return "Falta el nombre del cliente.";
            if (datos.Apellidos != null && string.IsNullOrWhiteSpace(datos.Apellidos)) return "Faltan los apellidos del cliente.";
            if (string.IsNullOrWhiteSpace(datos.Documento)) return "Falta el documento del cliente.";
            if (datos.FechaNacimiento.HasValue && datos.FechaNacimiento.Value.Date > DateTime.Today) return "La fecha de nacimiento no puede ser futura.";
            if (clientes.ExisteDocumento(datos.Documento.Trim(), datos.Id))
                return "Ya existe un cliente con el documento " + datos.Documento.Trim() + ".";

            datos.Nombre = datos.Nombre.Trim();
            datos.Documento = datos.Documento.Trim();
            id = clientes.Guardar(datos);
            auditoria.Registrar(actor, "Clientes", datos.Id == 0 ? "Crear cliente" : "Editar cliente", datos.Documento);
            return null;
        }

        public string Eliminar(string actorId, ClienteVista cliente)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Administrador", "Gerente");
            if (error != null) return error;
            if (clientes.TieneReservas(cliente.Id)) return "El huésped tiene reservas registradas y no se puede eliminar.";

            clientes.Eliminar(cliente.Id);
            auditoria.Registrar(actor, "Clientes", "Eliminar cliente", cliente.Documento);
            return null;
        }

        public string GuardarConsentimiento(string actorId, int huespedId, string tipo, bool autorizado, DateTime fecha)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, Roles);
            if (error != null) return error;
            if (huespedId == 0) return "Selecciona un huésped.";
            if (string.IsNullOrWhiteSpace(tipo)) return "Selecciona el tipo de consentimiento.";

            clientes.GuardarConsentimiento(huespedId, tipo, autorizado, fecha);
            auditoria.Registrar(actor, "Clientes", "Registrar consentimiento", tipo + (autorizado ? " · autorizado" : " · no autorizado"));
            return null;
        }
    }
}
