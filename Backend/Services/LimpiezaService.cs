using System;
using System.Collections.Generic;
using Crown__.Backend.Models;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class LimpiezaService
    {
        private static readonly string[] Roles = { "Servicios", "Administrador", "Gerente" };

        private readonly LimpiezaRepository repositorio = new LimpiezaRepository();
        private readonly AveriaRepository averias = new AveriaRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        public List<TareaLimpieza> Tareas() { return repositorio.Tareas(); }
        public List<EmpleadoVista> Empleados() { return repositorio.Empleados(); }
        public List<ChecklistVista> Checklists() { return repositorio.Checklists(); }
        public List<ChecklistItemVista> Items() { return repositorio.Items(); }
        public List<InspeccionVista> Inspecciones() { return repositorio.Inspecciones(); }
        public List<Averia> Incidencias() { return averias.ObtenerTodas(); }
        public List<OrdenVista> Ordenes() { return repositorio.Ordenes(); }
        public List<ObjetoOlvidadoVista> Objetos() { return repositorio.Objetos(); }

        // Cada operación devuelve null si salió bien, o el motivo del problema.
        private string Autorizar(string actorId, out int actor)
        {
            return Autorizacion.Verificar(actorId, out actor, Roles);
        }

        public string NuevaTarea(string actorId, int habitacionId, string tipo, DateTime programada, EmpleadoVista empleado)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (habitacionId == 0) return "Selecciona la habitación.";
            if (tipo != "LIMPIEZA" && tipo != "INSPECCION") return "El tipo de tarea no es válido.";

            repositorio.NuevaTarea(habitacionId, tipo, programada, empleado == null ? (int?)null : empleado.Id);
            auditoria.Registrar(actor, "Limpieza", "Nueva tarea", tipo + " · habitación " + habitacionId);
            return null;
        }

        public string AvanzarTarea(string actorId, TareaLimpieza tarea)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (tarea == null) return "Selecciona una tarea.";
            if (tarea.Estado == "COMPLETADA") return "La tarea ya está completada.";
            if (tarea.Tipo == "Inspección" && tarea.Estado == "EN_CURSO") return "Registra el resultado en la pestaña Inspecciones para completar la inspección.";

            string siguiente = tarea.Estado == "PENDIENTE" ? "EN_CURSO" : "COMPLETADA";
            error = repositorio.CambiarEstado(tarea.Id, siguiente, actor);
            if (error == null) auditoria.Registrar(actor, "Limpieza", "Avanzar tarea", "Hab. " + tarea.Habitacion + " → " + siguiente);
            return error;
        }

        public string RegistrarInspeccion(string actorId, TareaLimpieza tarea, EmpleadoVista empleado, bool aprobada, string observaciones)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (tarea == null) return "Selecciona la tarea de inspección.";

            error = repositorio.RegistrarInspeccion(tarea.Id, empleado == null ? (int?)null : empleado.Id, aprobada, observaciones, actor);
            if (error == null) auditoria.Registrar(actor, "Limpieza", "Inspección", "Hab. " + tarea.Habitacion + (aprobada ? " · aprobada" : " · rechazada"));
            return error;
        }

        public string GuardarChecklist(string actorId, string nombre, string descripcion)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del checklist.";
            if (repositorio.ExisteChecklist(nombre.Trim())) return "Ya existe el checklist " + nombre.Trim() + ".";

            repositorio.GuardarChecklist(nombre.Trim(), descripcion);
            auditoria.Registrar(actor, "Limpieza", "Crear checklist", nombre.Trim());
            return null;
        }

        public string GuardarItem(string actorId, ChecklistVista checklist, string descripcion)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (checklist == null) return "Selecciona el checklist.";
            if (string.IsNullOrWhiteSpace(descripcion)) return "Describe el ítem.";

            repositorio.GuardarItem(checklist.Id, descripcion.Trim());
            auditoria.Registrar(actor, "Limpieza", "Agregar ítem", checklist.Nombre);
            return null;
        }

        // La severidad llega como en las averías: Baja, Media, Alta o Crítica.
        public string RegistrarIncidencia(string actorId, string numeroHabitacion, string severidad, string descripcion)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (string.IsNullOrWhiteSpace(numeroHabitacion)) return "Selecciona la habitación.";
            if (string.IsNullOrWhiteSpace(descripcion)) return "Describe la incidencia.";

            error = averias.Registrar(numeroHabitacion, actor, descripcion.Trim(), severidad);
            if (error == null) auditoria.Registrar(actor, "Mantenimiento", "Registrar incidencia", "Hab. " + numeroHabitacion + " · " + severidad);
            return error;
        }

        public string ResolverIncidencia(string actorId, int incidenciaId)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;

            error = averias.Resolver(incidenciaId, actor);
            if (error == null) auditoria.Registrar(actor, "Mantenimiento", "Resolver incidencia", "MT-" + incidenciaId.ToString("000"));
            return error;
        }

        public string AsignarOrden(string actorId, OrdenVista orden, EmpleadoVista empleado)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (orden == null) return "Selecciona la orden.";
            if (empleado == null) return "Selecciona el empleado.";

            error = repositorio.AsignarOrden(orden.Id, empleado.Id);
            if (error == null) auditoria.Registrar(actor, "Mantenimiento", "Asignar orden", orden.OrdenTexto + " → " + empleado.Nombre);
            return error;
        }

        public string RegistrarObjeto(string actorId, int habitacionId, string descripcion)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (habitacionId == 0) return "Selecciona la habitación.";
            if (string.IsNullOrWhiteSpace(descripcion)) return "Describe el objeto.";

            repositorio.RegistrarObjeto(habitacionId, descripcion.Trim());
            auditoria.Registrar(actor, "Limpieza", "Objeto olvidado", descripcion.Trim());
            return null;
        }

        public string EntregarObjeto(string actorId, ObjetoOlvidadoVista objeto)
        {
            int actor;
            string error = Autorizar(actorId, out actor);
            if (error != null) return error;
            if (objeto == null) return "Selecciona el objeto.";
            if (objeto.Estado == "Entregado") return "El objeto ya fue entregado.";

            repositorio.EntregarObjeto(objeto.Id);
            auditoria.Registrar(actor, "Limpieza", "Entregar objeto", objeto.Descripcion);
            return null;
        }
    }
}
