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

        public static readonly string[] DemoRoles = { "Administrador", "Recepcion", "Housekeeping", "Supervisor", "Mantenimiento", "AlimentosBebidas", "Bienestar", "Lavanderia", "Finanzas", "Transporte" };
        private static readonly string[] Modules = { "Inicio", "Reservas", "Habitaciones", "CheckInOut", "Huespedes", "Convenios", "FNB", "GymSpa", "Lavanderia", "Finanzas", "Reportes", "Config", "Housekeeping", "Mantenimiento", "Transporte", "Tarifas", "Folios", "Inventario", "Alertas", "Operacion", "Usuarios", "Auditoria" };

        // Working interpretation of RF responsibilities. Not a production role directory.
        public static FrontendAccessContext CreateDemonstration(string role)
        {
            var grants = new HashSet<string>(StringComparer.Ordinal);
            string label = role;
            Action<string, string[]> add = (module, actions) => { grants.Add(module + ".View"); foreach (string action in actions) grants.Add(module + "." + action); };
            if (role == "Administrador")
                foreach (string module in Modules) add(module, new[] { "Write", "Cancel", "Approve", "Export", "Manage", "Privacy" });
            else if (DemoRoles.Contains(role))
            {
                add("Inicio", new string[0]); add("Alertas", new[] { "Write" });
                switch (role)
                {
                    case "Recepcion":
                        label = "Recepción";
                        foreach (string module in new[] { "Reservas", "CheckInOut", "Huespedes", "Folios", "Operacion", "Transporte" }) add(module, new[] { "Write" });
                        add("Habitaciones", new string[0]); add("Convenios", new string[0]); add("Reservas", new[] { "Cancel" }); break;
                    case "Housekeeping": add("Housekeeping", new[] { "Write" }); add("Habitaciones", new string[0]); break;
                    case "Supervisor":
                        label = "Supervisión de pisos";
                        add("Housekeeping", new[] { "Write", "Approve", "Manage" }); add("Habitaciones", new[] { "Write" }); add("Mantenimiento", new[] { "Write" }); break;
                    case "Mantenimiento": add("Mantenimiento", new[] { "Write" }); add("Habitaciones", new string[0]); break;
                    case "AlimentosBebidas": label = "Alimentos y bebidas"; add("FNB", new[] { "Write" }); add("Inventario", new[] { "Write" }); break;
                    case "Bienestar": add("GymSpa", new[] { "Write" }); break;
                    case "Lavanderia": label = "Lavandería"; add("Lavanderia", new[] { "Write" }); break;
                    case "Finanzas":
                        foreach (string module in new[] { "Finanzas", "Folios" }) add(module, new[] { "Write" });
                        add("Reportes", new[] { "Export" }); break;
                    case "Transporte": add("Transporte", new[] { "Write" }); break;
                }
            }
            return new FrontendAccessContext(grants.Count == 0 ? "" : "demo-" + role, "Personal de muestra", label ?? "Sin perfil", grants, true);
        }
    }
}
