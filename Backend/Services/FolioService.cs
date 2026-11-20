using System;
using System.Globalization;
using Crown__.Backend.Repositories;

namespace Crown__.Backend.Services
{
    public class FolioService
    {
        private const int LargoMaximoConcepto = 100;

        private readonly FolioRepository folios = new FolioRepository();
        private readonly AuditoriaService auditoria = new AuditoriaService();

        // Devuelve null si todo salió bien, o el motivo del problema.
        public string RegistrarPago(string actorId, string codigoReserva, decimal importe, string metodo, string referencia)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador", "Gerente");
            if (error != null) return error;
            if (importe <= 0) return "El importe debe ser mayor que cero.";

            if (!folios.RegistrarPago(codigoReserva, metodo, importe, out error, actor)) return error;

            string detalle = string.Format(CultureInfo.InvariantCulture, "{{\"importe\": {0}, \"metodo\": \"{1}\"}}", importe, metodo);
            auditoria.Registrar(actor, "Finanzas", "Registrar pago", codigoReserva + (string.IsNullOrWhiteSpace(referencia) ? "" : " · " + referencia.Trim()), null, detalle);
            return null;
        }

        // El concepto del formulario se asigna al concepto de cargo más parecido de la base.
        public string RegistrarCargo(string actorId, string codigoReserva, string concepto, decimal importe, string detalle)
        {
            int actor;
            string error = Autorizacion.Verificar(actorId, out actor, "Recepcionista", "Administrador");
            if (error != null) return error;
            if (importe <= 0) return "El importe debe ser mayor que cero.";

            string texto = concepto + (string.IsNullOrWhiteSpace(detalle) ? "" : " · " + detalle.Trim());
            if (!folios.RegistrarCargo(codigoReserva, ConceptoDe(concepto), Recortar(texto), importe, out error)) return error;

            auditoria.Registrar(actor, "Folios", "Registrar cargo", codigoReserva, null, Json(importe));
            return null;
        }

        internal static string ConceptoDe(string texto)
        {
            string t = (texto ?? "").ToLowerInvariant();
            if (t.Contains("aloj")) return "Alojamiento";
            if (t.Contains("restaur") || t.Contains("desayuno")) return "Restaurante";
            if (t.Contains("minibar")) return "Minibar";
            if (t.Contains("spa") || t.Contains("gimnasio")) return "Spa";
            if (t.Contains("lavander")) return "Lavanderia";
            if (t.Contains("transporte")) return "Transporte";
            return "Otros";
        }

        private static string Recortar(string texto)
        {
            return texto.Length <= LargoMaximoConcepto ? texto : texto.Substring(0, LargoMaximoConcepto);
        }

        private static string Json(decimal importe)
        {
            return string.Format(CultureInfo.InvariantCulture, "{{\"importe\": {0}}}", importe);
        }
    }
}
