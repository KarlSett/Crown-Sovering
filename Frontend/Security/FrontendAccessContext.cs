using System;
using System.Collections.Generic;
using System.Linq;

namespace Crown__.Frontend.Security
{
    // Presentation permissions only. A trusted backend must authorize every real operation.
    // No role name, client flag or constructor is an authentication boundary.
    public sealed class FrontendAccessContext
    {
        private readonly HashSet<string> permissions;
        public string UserId { get; private set; }
        public string DisplayName { get; private set; }
        public string RoleLabel { get; private set; }
        public bool IsDemonstration { get; private set; }
        public IEnumerable<string> Permissions { get { return permissions.OrderBy(p => p).ToArray(); } }

        public FrontendAccessContext(string userId, string displayName, string roleLabel, IEnumerable<string> grantedPermissions)
            : this(userId, displayName, roleLabel, grantedPermissions, false) { }

        private FrontendAccessContext(string userId, string displayName, string roleLabel, IEnumerable<string> grantedPermissions, bool demonstration)
        {
            UserId = userId ?? "";
            DisplayName = displayName ?? "Personal";
            RoleLabel = roleLabel ?? "Sin perfil";
            IsDemonstration = demonstration;
            permissions = new HashSet<string>(grantedPermissions ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
        }

        public bool Allows(string module, string action)
        {
            return !string.IsNullOrWhiteSpace(UserId) && permissions.Contains(module + ".View") && permissions.Contains(module + "." + action);
        }

        public static readonly string[] DemoRoles = { "Administrador", "Gerente", "Recepcionista" };

        private static string NormalizeRoleKey(string role)
        {
            if (string.Equals(role, "Recepcion", StringComparison.Ordinal))
                return "Recepcionista";
            return role ?? "";
        }

        public static string GetRoleDisplayName(string role)
        {
            switch (NormalizeRoleKey(role))
            {
                case "Administrador": return "Administrador";
                case "Gerente": return "Gerente";
                case "Recepcionista": return "Recepcionista";
                default: return role ?? "Sin perfil";
            }
        }

        // Only 3 roles exist in demonstration: Administrador, Gerente and Recepcionista.
        // Administrador and Gerente focus on administrative, commercial, event-discount and service-limit modules (not Reservas/front-desk).
        public static FrontendAccessContext CreateDemonstration(string role)
        {
            string normalized = NormalizeRoleKey(role);
            var grants = new HashSet<string>(StringComparer.Ordinal);
            string label = GetRoleDisplayName(normalized);
            Action<string, string[]> add = (module, actions) =>
            {
                grants.Add(module + ".View");
                foreach (string action in actions) grants.Add(module + "." + action);
            };

            if (DemoRoles.Contains(normalized))
            {
                add("Inicio", new string[0]);
                add("Alertas", new[] { "Write" });
                add("Preferencias", new[] { "Write" });

                switch (normalized)
                {
                    case "Administrador":
                        foreach (string module in new[] { "Eventos", "Tarifas", "Convenios", "Finanzas", "Reportes", "FNB", "GymSpa", "Lavanderia", "Transporte", "Inventario", "Usuarios", "Auditoria", "Config" })
                            add(module, new[] { "Write", "Cancel", "Approve", "Export", "Manage", "Privacy", "Limit" });
                        break;

                    case "Gerente":
                        add("Eventos", new[] { "Write", "Manage" });
                        add("Tarifas", new[] { "Write", "Manage" });
                        add("Convenios", new[] { "Write", "Manage" });
                        add("Finanzas", new[] { "Write", "Approve", "Export" });
                        add("Reportes", new[] { "Export", "Manage" });
                        add("FNB", new[] { "Write", "Manage", "Limit" });
                        add("GymSpa", new[] { "Write", "Manage", "Limit" });
                        add("Lavanderia", new[] { "Limit" });
                        add("Transporte", new[] { "Limit" });
                        add("Inventario", new[] { "Write" });
                        break;

                    case "Recepcionista":
                        add("Reservas", new[] { "Write", "Cancel" });
                        add("Habitaciones", new[] { "Write", "Manage" });
                        add("CheckInOut", new[] { "Write", "Privacy", "Manage" });
                        add("Folios", new[] { "Write" });
                        add("Operacion", new[] { "Write" });
                        add("Housekeeping", new[] { "Write", "Manage", "Approve" });
                        foreach (string service in new[] { "FNB", "GymSpa", "Mantenimiento", "Lavanderia", "Transporte" })
                            add(service, new[] { "Write" });
                        break;
                }
            }

            return new FrontendAccessContext(
                grants.Count == 0 ? "" : "demo-" + normalized,
                "Personal de muestra",
                grants.Count == 0 ? "Sin perfil" : label,
                grants,
                true);
        }
    }
}
