namespace Crown__.Frontend
{
    /// <summary>Roles de la demostración. No hay autenticación ni permisos reales.</summary>
    public enum Rol
    {
        Administrador,
        Gerente,
        Recepcionista,
        Servicios
    }

    /// <summary>
    /// Estado de sesión de la demostración: qué rol eligió el usuario en el login.
    /// Solo se usa para mostrar u ocultar entradas del menú; el backend real deberá
    /// validar permisos en cada operación.
    /// </summary>
    public static class Sesion
    {
        public static Rol Rol { get; set; } = Rol.Recepcionista;
        public static int UsuarioId { get; private set; }
        public static string NombreUsuario { get; private set; }

        /// <summary>Abre el entorno local de desarrollo con la cuenta admin creada por los datos semilla.</summary>
        public static void IniciarAdminRapidoDev()
        {
            UsuarioId = 1;
            NombreUsuario = "admin · acceso rápido dev";
            Rol = Rol.Administrador;
            Crown__.Backend.Services.ServicioHotel.ActorId = UsuarioId.ToString();
        }

        /// <summary>Guarda al usuario autenticado; el backend recibe su identificador en cada operación.</summary>
        public static void Iniciar(Crown__.Backend.Models.Usuario usuario)
        {
            UsuarioId = usuario.Id;
            NombreUsuario = usuario.NombreCompleto;
            switch (usuario.Rol)
            {
                case "Administrador": Rol = Rol.Administrador; break;
                case "Gerente": Rol = Rol.Gerente; break;
                case "Servicios": Rol = Rol.Servicios; break;
                default: Rol = Rol.Recepcionista; break;
            }
            Crown__.Backend.Services.ServicioHotel.ActorId = usuario.Id.ToString();
        }

        public static void Cerrar()
        {
            UsuarioId = 0;
            NombreUsuario = null;
            Crown__.Backend.Services.ServicioHotel.ActorId = null;
        }

        public static bool EsAdministrador
        {
            get { return Rol == Rol.Administrador; }
        }

        public static string NombreRol
        {
            get
            {
                switch (Rol)
                {
                    case Rol.Administrador:
                        return "Administrador";
                    case Rol.Gerente:
                        return "Gerente";
                    case Rol.Servicios:
                        return "Personal de servicios";
                    default:
                        return "Recepcionista";
                }
            }
        }
    }
}
