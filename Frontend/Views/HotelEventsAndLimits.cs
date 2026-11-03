using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Crown__.Frontend.Security;

namespace Crown__.Frontend.Views
{
    public sealed class ThemeOptionItem
    {
        public string Key { get; set; }
        public string ShortLabel { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public SolidColorBrush NavBrush { get; set; }
        public SolidColorBrush SurfaceBrush { get; set; }
        public SolidColorBrush AccentBrush { get; set; }
        public override string ToString() { return Title; }
    }

    public sealed class RoleOptionItem
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public override string ToString() { return Title; }
    }

    internal sealed class HotelEventItem
    {
        internal string Code, Title, Category, SuggestedDiscount, TargetServices, DemandLevel, Status, Notes;
        internal DateTime Start, End;
    }

    internal sealed class ServiceLimitItem
    {
        internal string Code, ModuleKey, ServiceName, Schedule, Capacity, State, Reason;
    }

    internal sealed class GuestHistoryProfile
    {
        internal string Code, FullName, DocumentId, Email, Phone, Company, Segment, Stays, Preferences, Allergies, Notes;
    }

    // Presentation-only fixtures for themes, demo role selection, upcoming discount events and service limits.
    internal static class HotelEventsAndLimits
    {
        private static SolidColorBrush FrozenBrush(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
            brush.Freeze();
            return brush;
        }

        internal static readonly ThemeOptionItem[] ThemeOptions = {
            new ThemeOptionItem {
                Key = "Light", ShortLabel = "Claro", Title = "Claro · Petróleo y marfil",
                Subtitle = "Superficie luminosa para operación diurna",
                NavBrush = FrozenBrush("#17384F"), SurfaceBrush = FrozenBrush("#F3F6F8"), AccentBrush = FrozenBrush("#8A682C")
            },
            new ThemeOptionItem {
                Key = "Dark", ShortLabel = "Oscuro", Title = "Oscuro · Noche y dorado",
                Subtitle = "Contraste descansado para turnos nocturnos",
                NavBrush = FrozenBrush("#10202C"), SurfaceBrush = FrozenBrush("#14232F"), AccentBrush = FrozenBrush("#D9BB79")
            },
            new ThemeOptionItem {
                Key = "Brand", ShortLabel = "Marca", Title = "Marca · Escala esmeralda",
                Subtitle = "Paleta institucional (#EBFEF7 · #02C798 · #004D40 · #003029)",
                NavBrush = FrozenBrush("#003029"), SurfaceBrush = FrozenBrush("#CEFDE9"), AccentBrush = FrozenBrush("#02C798")
            }
        };

        internal static readonly RoleOptionItem[] DemoRoleOptions = {
            new RoleOptionItem { Key = "Administrador", Title = "Administrador", Summary = "Gestión administrativa, eventos, descuentos y configuración" },
            new RoleOptionItem { Key = "Gerente", Title = "Gerente", Summary = "Gestión comercial, calendario de eventos y límite de servicios" },
            new RoleOptionItem { Key = "Recepcionista", Title = "Recepcionista", Summary = "Reservas, check-in y huéspedes, habitaciones y servicios" }
        };

        internal static readonly string[] GuestColumns = { "Ficha / Doc.", "Huésped / empresa", "Contacto", "Segmento / estancias", "Estado" };
        internal static readonly string[][] GuestRows = {
            new[] { "H-001 · CI 4829104", "María Fernández · Andes Corp.", "maria@example.com · +591 7000 0101", "Corporativo · 3 estancias", "Próxima llegada" },
            new[] { "H-002 · CI 5910238", "Carlos Rojas ·Particular", "+591 7000 0102 · carlos@example.com", "Recurrente · 2 estancias", "Próxima llegada" },
            new[] { "H-003 · PAS A94821", "Lucía Gómez · Altiplano Business", "lucia@example.com · +591 7000 0103", "Recurrente · 8 estancias", "Activo" },
            new[] { "H-004 · CI 3301942", "Diego Salazar · Inversiones Sur", "diego@example.com · +591 7000 0104", "VIP · 12 estancias", "VIP" },
            new[] { "H-005 · CI 6120495", "Javier Vargas · Eco Tours S.A.", "+591 7000 0105 · javier@example.com", "Grupo · 2 estancias", "Alojado" }
        };
        internal static readonly FieldDefinition[] GuestFields = {
            new FieldDefinition("Nombre completo", "text", "Ej. María Fernández"),
            new FieldDefinition("Documento de identidad", "text", "Ej. CI 4829104 o Pasaporte"),
            new FieldDefinition("Correo electrónico", "email", "nombre@correo.com"),
            new FieldDefinition("Teléfono", "phone", "+591 7000 0000"),
            new FieldDefinition("Empresa o agencia", "optional", "Ej. Andes Corp. (opcional)"),
            new FieldDefinition("Segmento", "choice", "", "VIP", "Recurrente", "Corporativo", "Agencia", "Grupo"),
            new FieldDefinition("Preferencias, alergias y notas", "optional", "Ej. Piso alto, libre de humo, alergia a frutos secos (opcional)")
        };

        internal static readonly GuestHistoryProfile[] GuestProfiles = {
            new GuestHistoryProfile {
                Code = "H-001", FullName = "María Fernández", DocumentId = "CI 4829104", Email = "maria@example.com", Phone = "+591 7000 0101",
                Company = "Andes Corp.", Segment = "Corporativo", Stays = "3 estancias previas (sin incidencias)",
                Preferences = "Piso alto, habitación libre de humo, almohada hipoalergénica",
                Allergies = "Intolerancia a la lactosa (informar a desayunos F&B)",
                Notes = "Convenio corporativo CON-012 vigente; solicita factura electrónica a razón social."
            },
            new GuestHistoryProfile {
                Code = "H-004", FullName = "Diego Salazar", DocumentId = "CI 3301942", Email = "diego@example.com", Phone = "+591 7000 0104",
                Company = "Inversiones Sur", Segment = "VIP", Stays = "12 estancias previas",
                Preferences = "Suite piso 4, prensa matutina, circuito hidrotermal a las 11:30",
                Allergies = "Sin alergias registradas",
                Notes = "Beneficio VIP activo: late check-out sin recargo sujeto a disponibilidad."
            },
            new GuestHistoryProfile {
                Code = "H-003", FullName = "Lucía Gómez", DocumentId = "PAS A94821", Email = "lucia@example.com", Phone = "+591 7000 0103",
                Company = "Altiplano Business", Segment = "Recurrente", Stays = "8 estancias previas",
                Preferences = "Vista interior silenciosa, agua sin gas en minibar",
                Allergies = "Alergia a frutos secos",
                Notes = "Consentimiento de datos otorgado el 12/08/2026."
            }
        };

        internal static readonly string[][] ExternalDirectoryContacts = {
            new[] { "EXT-901 · NIT 1029384021", "Valeria Montaño · Minera del Illimani", "vmontano@illimani.bo · +591 7150 4410", "Directorio corporativo externo", "Disponible" },
            new[] { "EXT-902 · PAS B774120", "Martín Echeverría · Consultora Andina", "mecheverria@andina.org · +591 7200 8831", "Directorio corporativo externo", "Disponible" },
            new[] { "EXT-903 · CI 7491023", "Camila Torrez · Agencia Horizonte", "reservas@horizonte.travel · +591 7640 1920", "Directorio de agencias externo", "Disponible" }
        };

        internal static readonly HotelEventItem[] UpcomingEvents = {
            new HotelEventItem {
                Code = "EVT-201", Title = "Feriado Todos Santos · Puente", Category = "Feriado / puente",
                Start = new DateTime(2026, 11, 1), End = new DateTime(2026, 11, 3),
                SuggestedDiscount = "15%", TargetServices = "Alojamiento + Comidas", DemandLevel = "Demanda media", Status = "Por programar",
                Notes = "Sugerir 15% de descuento en estancias de 3 noches con desayuno buffet incluido."
            },
            new HotelEventItem {
                Code = "EVT-202", Title = "Congreso Andino de Medicina", Category = "Corporativo / congreso",
                Start = new DateTime(2026, 11, 12), End = new DateTime(2026, 11, 15),
                SuggestedDiscount = "12%", TargetServices = "Alojamiento + Spa y Gym", DemandLevel = "Demanda alta", Status = "Activa",
                Notes = "Tarifa preferente para asistentes acreditados con acceso a gimnasio y descuento en cabinas de spa."
            },
            new HotelEventItem {
                Code = "EVT-203", Title = "Festival Gastronómico y Cultural", Category = "Temporada / cultura",
                Start = new DateTime(2026, 11, 20), End = new DateTime(2026, 11, 22),
                SuggestedDiscount = "18%", TargetServices = "Alojamiento + Comidas", DemandLevel = "Demanda media", Status = "Sugerido",
                Notes = "Promoción de fin de semana con menú degustación en restaurante y salida flexible."
            },
            new HotelEventItem {
                Code = "EVT-204", Title = "Semana Pre-Diciembre · Baja demanda", Category = "Promoción baja demanda",
                Start = new DateTime(2026, 11, 26), End = new DateTime(2026, 11, 30),
                SuggestedDiscount = "25%", TargetServices = "Paquete integral", DemandLevel = "Demanda baja", Status = "Prioridad",
                Notes = "Ventana de baja ocupación prevista: activar hasta 25% de descuento por reserva anticipada."
            },
            new HotelEventItem {
                Code = "EVT-205", Title = "Foro Empresarial e Inversiones", Category = "Corporativo / congreso",
                Start = new DateTime(2026, 12, 8), End = new DateTime(2026, 12, 10),
                SuggestedDiscount = "10%", TargetServices = "Solo alojamiento", DemandLevel = "Demanda alta", Status = "Programada",
                Notes = "Descuento corporativo moderado por alta ocupación; prever cupos de restaurante y traslados."
            },
            new HotelEventItem {
                Code = "EVT-206", Title = "Gala de Navidad y Fin de Año", Category = "Temporada / cultura",
                Start = new DateTime(2026, 12, 24), End = new DateTime(2026, 12, 31),
                SuggestedDiscount = "15%", TargetServices = "Paquete integral", DemandLevel = "Demanda alta", Status = "Por programar",
                Notes = "Paquete familiar de 4 noches con cena especial, desayuno buffet y circuito de spa."
            },
            new HotelEventItem {
                Code = "EVT-207", Title = "Semana Wellness de Verano", Category = "Promoción baja demanda",
                Start = new DateTime(2027, 1, 14), End = new DateTime(2027, 1, 18),
                SuggestedDiscount = "20%", TargetServices = "Alojamiento + Spa y Gym", DemandLevel = "Demanda baja", Status = "Sugerido",
                Notes = "Impulsar ocupación de mitad de enero con paquete habitación deluxe + masaje + entrenamiento."
            }
        };

        internal static string[][] EventRows()
        {
            return UpcomingEvents.Select(e => new[] {
                e.Code,
                e.Title + " · " + e.Category,
                e.Start.ToString("dd MMM", System.Globalization.CultureInfo.GetCultureInfo("es-BO")) + " – " + e.End.ToString("dd MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-BO")),
                e.SuggestedDiscount + " · " + e.TargetServices,
                e.Status
            }).ToArray();
        }

        private static List<ServiceLimitItem> serviceLimits;

        static HotelEventsAndLimits()
        {
            ResetServiceLimits();
        }

        internal static IList<ServiceLimitItem> ServiceLimits { get { return serviceLimits; } }

        internal static void ResetServiceLimits()
        {
            serviceLimits = new List<ServiceLimitItem> {
                new ServiceLimitItem { Code = "SRV-FNB-1", ModuleKey = "FNB", ServiceName = "Comidas · Restaurante y buffet", Schedule = "06:30–23:00", Capacity = "60 plazas", State = "Operativo", Reason = "Desayuno 06:30–10:30, almuerzo 12:30–15:30 y cena 19:00–23:00." },
                new ServiceLimitItem { Code = "SRV-FNB-2", ModuleKey = "FNB", ServiceName = "Comidas · Room service y minibar", Schedule = "07:00–23:00", Capacity = "Cocina de piso", State = "Horario limitado", Reason = "Servicio nocturno restringido de 23:00 a 07:00 por mantenimiento preventivo." },
                new ServiceLimitItem { Code = "SRV-SPA-1", ModuleKey = "GymSpa", ServiceName = "Spa · Cabinas e hidroterapia", Schedule = "09:00–21:00", Capacity = "4 cabinas · 8 plazas", State = "Operativo", Reason = "Atención con cita previa; circuito hidrotermal de 45 min." },
                new ServiceLimitItem { Code = "SRV-GYM-1", ModuleKey = "GymSpa", ServiceName = "Gimnasio · Sala fitness", Schedule = "06:00–22:00", Capacity = "18 / 20 plazas", State = "Aforo limitado", Reason = "Cupo preventivo limitado a 20 personas simultáneas." },
                new ServiceLimitItem { Code = "SRV-LAV-1", ModuleKey = "Lavanderia", ServiceName = "Lavandería y planchado exprés", Schedule = "07:00–19:00", Capacity = "15 órdenes exprés", State = "Operativo", Reason = "Entregas en el día para recepciones antes de las 11:00." },
                new ServiceLimitItem { Code = "SRV-TRA-1", ModuleKey = "Transporte", ServiceName = "Traslados aeropuerto y ciudad", Schedule = "05:00–23:30", Capacity = "3 unidades activas", State = "Operativo", Reason = "Coordinación sujeta a vuelos confirmados en recepción." }
            };
        }

        internal static void ApplySimulatedLimit(string serviceName, string state, string schedule, string capacity, string reason)
        {
            var target = serviceLimits.FirstOrDefault(s => s.ServiceName == serviceName);
            if (target == null) return;
            target.State = state;
            target.Schedule = schedule;
            target.Capacity = capacity;
            target.Reason = reason;
        }
    }
}
