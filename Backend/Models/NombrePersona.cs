namespace Crown__.Backend.Models
{
    public static class NombrePersona
    {
        // El último término es el apellido: "María Fernández" -> nombres "María", apellidos "Fernández".
        public static void Separar(string completo, out string nombres, out string apellidos)
        {
            string limpio = (completo ?? "").Trim();
            int corte = limpio.LastIndexOf(' ');
            nombres = corte < 0 ? limpio : limpio.Substring(0, corte).Trim();
            apellidos = corte < 0 ? "-" : limpio.Substring(corte + 1);
        }
    }
}
