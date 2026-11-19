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
