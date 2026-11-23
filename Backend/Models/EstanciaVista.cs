using System;

namespace Crown__.Backend.Models
{
    public class EstanciaDetalle
    {
        public int EstanciaId { get; set; }
        public string TipoCheckin { get; set; }
        public string TipoCheckout { get; set; }
        public string Estado { get; set; }
    }

    public class LlaveVista
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public DateTime Emitida { get; set; }
        public DateTime? Devuelta { get; set; }
        public string Estado { get; set; }

        public string EmitidaTexto { get { return Emitida.ToString("dd/MM HH:mm"); } }
        public string DevueltaTexto { get { return Devuelta.HasValue ? Devuelta.Value.ToString("dd/MM HH:mm") : "—"; } }
    }
}
