using System;
using System.Linq;

namespace Crown__.Backend.Models
{
    public class Usuario
    {
        private static readonly string[] JerarquiaDeRoles = { "Administrador", "Gerente", "Recepcionista", "Servicios" };

        public int Id { get; set; }
        public string NombreCompleto { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string[] Roles { get; set; } = new string[0];
        public bool Activo { get; set; }
        public DateTime? UltimoAcceso { get; set; }

        // Rol principal: si el usuario tiene varios, el de más jerarquía.
        public string Rol
        {
            get { return JerarquiaDeRoles.FirstOrDefault(r => Roles.Contains(r)) ?? Roles.FirstOrDefault(); }
        }
    }
}
