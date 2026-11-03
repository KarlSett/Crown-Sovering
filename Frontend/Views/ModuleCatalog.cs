using System.Collections.Generic;

namespace Crown__.Frontend.Views
{
    // Replace these fixtures with Supabase-backed view models (RNF-02, RNF-03).
    // No domain operations or persistence live here.
    internal sealed class ModuleDefinition
    {
        public string Key, Title, Description, Action, Icon;
        public string[] Columns, Metrics;
        public string[][] Rows;
        public FieldDefinition[] Fields;
    }

    internal sealed class FieldDefinition
    {
        public string Label, Kind, Hint;
        public string[] Options;
        public FieldDefinition(string label, string kind = "text", string hint = "", params string[] options)
        {
            Label = label;
            Kind = kind;
            Hint = string.IsNullOrEmpty(hint) && kind != "choice" ? "Ingresa " + label.ToLowerInvariant() : hint;
            Options = options;
        }
    }

    internal static class ModuleCatalog
    {
        static ModuleCatalog()
        {
            All.AddRange(OperationsCatalog.Modules);
        }

        internal static readonly List<ModuleDefinition> All = new List<ModuleDefinition>
        {
            new ModuleDefinition { Key="Inicio", Title="Panel principal", Description="Sábado, 31 de octubre de 2026 · Conexión en tiempo real simulada (RNF-02)",
                Icon="M3,10 L12,3 L21,10 V21 H15 V14 H9 V21 H3 Z" },
            new ModuleDefinition { Key="Preferencias", Title="Opciones y preferencias", Description="Tema visual (Alt+T), idioma (RNF-25), simulación de red cloud (RNF-02) y atajos de teclado (RNF-24).",
                Icon="M4,6 H20 M4,12 H20 M4,18 H20 M8,4 V8 M15,10 V14 M10,16 V20" },
            new ModuleDefinition { Key="Reservas", Title="Reservas", Description="Registro individual, walk-in, grupal y corporativo con canal manual y control de disponibilidad (RF-01 a RF-09).", Action="Nueva reserva",
                Icon="M4,5 H20 V21 H4 Z M4,10 H20 M8,2 V7 M16,2 V7 M8,14 H11 M14,17 H17",
                Metrics=new[]{"14|Llegadas hoy", "6|Por confirmar", "8|Confirmadas", "0%|Sobreventa no autorizada"},
                Columns=new[]{"Reserva", "Huésped / canal manual", "Estancia", "Habitación / tarifa", "Estado"},
                Rows=new[]{
                    new[]{"RES-1048", "María Fernández · Correo directo", "31 oct – 03 nov", "204 · Deluxe ($148.50)", "Confirmada"},
                    new[]{"RES-1049", "Carlos Rojas · Mostrador", "31 oct – 02 nov", "118 · Clásica ($110.00)", "Por confirmar"},
                    new[]{"RES-1050", "Eco Tours S.A. · Agencia (manual)", "31 oct – 04 nov", "6 hab. · Cuenta maestra", "Confirmada"},
                    new[]{"RES-1051", "Lucía Gómez · Teléfono", "31 oct – 01 nov", "210 · Deluxe ($148.50)", "Confirmada"},
                    new[]{"RES-1052", "Diego Salazar · WhatsApp", "31 oct – 05 nov", "415 · Suite ($210.00)", "Por confirmar"},
                    new[]{"RES-1053", "Ana Mendoza · Mostrador", "01 nov – 03 nov", "122 · Clásica ($110.00)", "Cancelada"}},
                Fields=new[]{
                    new FieldDefinition("Modalidad de reserva", "choice", "", "Individual", "Walk-in", "Grupal", "Corporativa"),
                    new FieldDefinition("Nombre del huésped", "text", "Ej. María Fernández"),
                    new FieldDefinition("Correo de contacto", "email", "nombre@correo.com"),
                    new FieldDefinition("Llegada", "date", "dd/MM/aaaa"),
                    new FieldDefinition("Salida", "date", "dd/MM/aaaa"),
                    new FieldDefinition("Tipo de habitación", "choice", "", "Individual", "Doble", "Matrimonial", "Triple", "Suite", "Familiar"),
                    new FieldDefinition("Número de huéspedes", "integer", "Ej. 2 (de 1 a 6)"),
                    new FieldDefinition("Tarifa por noche (USD)", "amount", "Ej. 148.50"),
                    new FieldDefinition("Canal de origen (manual)", "choice", "", "Mostrador", "Teléfono", "WhatsApp", "Correo directo", "Agencia (manual)", "Convenio corporativo"),
                    new FieldDefinition("Habitación preferida", "optional", "Ej. 204 · Piso alto, sin humo (opcional)"),
                    new FieldDefinition("Observaciones", "optional", "Preferencias o notas para recepción (opcional)")}},
            new ModuleDefinition { Key="Habitaciones", Title="Habitaciones", Description="Mapa visual por piso, atributos físicos/adicionales y ciclo controlado de estados (RF-10 a RF-16).", Action="Registrar avería",
                Icon="M3,18 V7 M21,18 V7 M3,15 H21 M3,9 H21 V15 M8,9 V15",
                Metrics=new[]{"96|Ocupadas", "14|Disponibles", "6|En limpieza", "2|Bloqueadas / Mantenimiento"},
                Columns=new[]{"Habitación", "Categoría / atributos", "Piso", "Asignación / estado", "Estado"},
                Rows=new[]{
                    new[]{"101", "Clásica · 2 pers. · Accesible", "Piso 1", "Sin asignar · Sin humo", "Disponible"},
                    new[]{"118", "Clásica · 2 pers. · Libre de humo", "Piso 1", "Carlos Rojas", "Disponible"},
                    new[]{"122", "Clásica · 2 pers. · Estándar", "Piso 1", "Checklist en curso", "En limpieza"},
                    new[]{"201", "Deluxe · 3 pers. · Cuna disp.", "Piso 2", "Familia Herrera", "Ocupada"},
                    new[]{"204", "Deluxe · 2 pers. · Hipoalergénica", "Piso 2", "María Fernández", "Disponible"},
                    new[]{"210", "Deluxe · 2 pers. · Libre de humo", "Piso 2", "Lucía Gómez", "Disponible"},
                    new[]{"305", "Deluxe · 2 pers. · Vista jardín", "Piso 3", "Roberto Núñez", "Ocupada"},
                    new[]{"307", "Triple · 3 pers. · Grupo", "Piso 3", "Eco Tours S.A.", "Ocupada"},
                    new[]{"408", "Suite · 4 pers. · Balcón", "Piso 4", "Javier Vargas", "Ocupada"},
                    new[]{"410", "Suite · 4 pers. · Jacuzzi", "Piso 4", "Inspección obligatoria", "En limpieza"},
                    new[]{"412", "Suite · 4 pers. · Crítica", "Piso 4", "Climatización · Bloqueada", "Mantenimiento"},
                    new[]{"415", "Suite · 4 pers. · VIP sin humo", "Piso 4", "Diego Salazar", "Disponible"}},
                Fields=new[]{
                    new FieldDefinition("Habitación", "choice", "", "101", "118", "122", "201", "204", "210", "305", "307", "408", "410", "412", "415"),
                    new FieldDefinition("Prioridad", "choice", "", "Normal", "Alta", "Crítica (bloquea habitación)"),
                    new FieldDefinition("Descripción de la avería o daño", "notes", "Detalla el desperfecto detectado en la habitación")}},
            new ModuleDefinition { Key="CheckInOut", Title="Check-in y huéspedes", Description="Check-in/out normal, express y anticipado/late, llaves y directorio difuso de huéspedes unificado (RF-17 a RF-22, RF-27 a RF-31).", Action="Preparar movimiento",
                Icon="M3,4 H11 V20 H3 Z M15,6 A3,3 0 1 0 15,12 A3,3 0 1 0 15,6 M10,20 C10,15.5 20,15.5 20,20",
                Metrics=new[]{"14|Llegadas hoy", "11|Salidas hoy", "96|Huéspedes in-house", "5|Fichas VIP / Corporativo"},
                Columns=new[]{"Habitación", "Huésped", "Hora / modalidad", "Historial / saldo", "Estado"},
                Rows=new[]{
                    new[]{"204", "María Fernández", "13:30 · Normal", "3 estancias · Sin lactosa", "Llegada"},
                    new[]{"118", "Carlos Rojas", "14:00 · Express", "2 estancias · Confirmar doc.", "Llegada"},
                    new[]{"307", "Eco Tours S.A.", "14:45 · Grupal", "Cuenta maestra FM-007", "Llegada"},
                    new[]{"210", "Lucía Gómez", "15:20 · Anticipado", "8 estancias · Sin frutos secos", "Llegada"},
                    new[]{"201", "Familia Herrera", "11:00 · Normal", "$24.00 · Minibar pendiente", "Salida"},
                    new[]{"305", "Roberto Núñez", "12:30 · Late check-out", "$0.00 · Llaves activas", "Salida"},
                    new[]{"408", "Javier Vargas", "11:45 · Express", "$148.50 · Emitir factura", "Salida"}},
                Fields=new[]{
                    new FieldDefinition("Movimiento", "choice", "", "Check-in normal", "Check-in express", "Check-in anticipado", "Check-out normal", "Check-out parcial", "Check-out express", "Late check-out"),
                    new FieldDefinition("Reserva o habitación", "text", "Ej. RES-1048 o habitación 204"),
                    new FieldDefinition("Nombre del huésped", "text", "Ej. María Fernández"),
                    new FieldDefinition("Documento de identidad", "text", "Ej. CI 4829104 o Pasaporte"),
                    new FieldDefinition("Acompañantes, llaves u observaciones", "optional", "Ej. 1 acompañante, 2 tarjetas entregadas (opcional)")}},
            new ModuleDefinition { Key="Eventos", Title="Eventos y descuentos", Description="Calendario comercial de próximos eventos, feriados y congresos para anticipar promociones y descuentos (RF-07, RF-51).", Action="Preparar descuento",
                Icon="M4,5 H20 V21 H4 Z M4,10 H20 M8,2 V7 M16,2 V7 M12,13 L13.2,15.5 L16,15.8 L14,17.7 L14.5,20.3 L12,19 L9.5,20.3 L10,17.7 L8,15.8 L10.8,15.5 Z",
                Metrics=new[]{"7|Eventos próximos", "10% – 25%|Descuentos sugeridos", "3|Campañas por programar"},
                Columns=new[]{"Evento", "Nombre / categoría", "Fechas", "Descuento sugerido", "Estado"},
                Rows=HotelEventsAndLimits.EventRows(),
                Fields=new[]{
                    new FieldDefinition("Evento asociado", "text", "Ej. Congreso Andino de Medicina"),
                    new FieldDefinition("Desde", "date", "dd/MM/aaaa"),
                    new FieldDefinition("Hasta", "date", "dd/MM/aaaa"),
                    new FieldDefinition("Descuento sugerido (%)", "integer", "Ej. 15 (entre 1 y 100)"),
                    new FieldDefinition("Alcance del descuento", "choice", "", "Solo alojamiento", "Alojamiento + Comidas", "Alojamiento + Spa y Gym", "Paquete integral"),
                    new FieldDefinition("Condición comercial", "optional", "Ej. Estancia mínima de 2 noches (opcional)")}},
            new ModuleDefinition { Key="Convenios", Title="Convenios", Description="Convenios corporativos con datos fiscales, tarifas preferenciales, cuentas maestras, crédito y alertas de vencimiento (RF-25).", Action="Nuevo convenio",
                Icon="M3,7 H21 V21 H3 Z M8,7 V3 H16 V7 M3,12 H21 M10,12 V15 H14 V12",
                Metrics=new[]{"12|Convenios activos", "2|Alerta de vencimiento", "$8,500|Crédito corporativo disp."},
                Columns=new[]{"Convenio", "Empresa / NIT", "Tarifa / crédito", "Vigencia hasta", "Estado"},
                Rows=new[]{
                    new[]{"CON-012", "Andes Corp. · NIT 1020458021", "$125.00 · Lím. $3,000", "06 nov 2026", "Por renovar"},
                    new[]{"CON-011", "Eco Tours S.A. · NIT 1044910029", "$112.00 · Lím. $5,000", "31 dic 2026", "Activo"},
                    new[]{"CON-010", "Altiplano Business · NIT 1098320014", "$130.00 · Lím. $4,000", "31 mar 2027", "Activo"},
                    new[]{"CON-009", "Viajes del Sur · NIT 1003948011", "$118.00 · Lím. $2,000", "30 sep 2026", "Vencido"}},
                Fields=new[]{
                    new FieldDefinition("Empresa o agencia", "text", "Ej. Andes Corp. S.A."),
                    new FieldDefinition("Datos fiscales (NIT / RUC)", "text", "Ej. NIT 1020458021"),
                    new FieldDefinition("Contacto responsable", "text", "Ej. Paula Ríos"),
                    new FieldDefinition("Correo corporativo", "email", "convenios@empresa.com"),
                    new FieldDefinition("Tarifa preferencial (USD)", "amount", "Ej. 125.00"),
                    new FieldDefinition("Límite de crédito (USD)", "amount", "Ej. 3000.00"),
                    new FieldDefinition("Vigencia hasta", "date", "dd/MM/aaaa")}},
            new ModuleDefinition { Key="FNB", Title="Comidas y bebidas", Description="Desayunos incluidos por ocupación, menú de restaurante, room service y gestión de minibar (RF-32 a RF-35).", Action="Registrar consumo",
                Icon="M4,3 V10 Q7,14 10,10 V3 M7,3 V22 M18,3 Q13,7 18,13 V22 M18,3 V13",
                Metrics=new[]{"68|Desayunos hoy (cocina)", "$486.00|Consumos F&B", "8|Comandas / minibares"},
                Columns=new[]{"Comanda", "Habitación / mesa", "Servicio / modalidad", "Importe / cobro", "Estado"},
                Rows=new[]{
                    new[]{"FB-028", "204 · María Fernández", "Room service · Cargo al folio", "$32.00", "En preparación"},
                    new[]{"FB-027", "Mesa 06 · Restaurante", "Restaurante · Pago directo", "$48.00", "Por entregar"},
                    new[]{"FB-026", "201 · Familia Herrera", "Minibar · Cargo automático", "$24.00", "Por revisar"},
                    new[]{"FB-025", "Salón · Ocupación piso 2", "Desayuno incluido · Tarifa Deluxe", "$0.00 (Incluido)", "Entregada"},
                    new[]{"FB-024", "408 · Javier Vargas", "Restaurante · Cargo al folio", "$28.50", "Entregada"}},
                Fields=new[]{
                    new FieldDefinition("Habitación o mesa", "text", "Ej. Habitación 204 o Mesa 06"),
                    new FieldDefinition("Servicio", "choice", "", "Desayuno buffet", "Restaurante", "Room service", "Minibar"),
                    new FieldDefinition("Modalidad de cobro", "choice", "", "Cargo al folio", "Pago directo (efectivo / tarjeta)"),
                    new FieldDefinition("Detalle del consumo", "notes", "Platos, bebidas y cantidades solicitadas"),
                    new FieldDefinition("Importe (USD)", "amount", "Ej. 32.00"),
                    new FieldDefinition("Alergias o indicaciones", "optional", "Ej. Sin lactosa, sin frutos secos (opcional)")}},
            new ModuleDefinition { Key="GymSpa", Title="Spa y gimnasio", Description="Control de aforo de gimnasio, catálogo de spa por terapeuta y paquetes en horarios de baja afluencia (RF-37 a RF-39).", Action="Agendar cita / acceso",
                Icon="M12,2 C9,7 5,11 5,16 A7,7 0 0 0 19,16 C19,11 15,7 12,2 Z",
                Metrics=new[]{"18 / 20|Aforo actual gimnasio", "6|Citas de spa hoy", "4|Terapeutas activos"},
                Columns=new[]{"Hora", "Huésped / habitación", "Espacio / servicio", "Terapeuta / cobro", "Estado"},
                Rows=new[]{
                    new[]{"10:00", "Lucía Gómez · 210", "Spa · Masaje relajante 60 min", "Camila R. · Cargo al folio", "Finalizada"},
                    new[]{"11:30", "Diego Salazar · 415", "Spa · Circuito hidrotermal", "Andrés M. · Tarifa baja afluencia", "En curso"},
                    new[]{"14:00", "María Fernández · 204", "Spa · Sauna y aromaterapia", "Camila R. · Cobro directo", "Confirmada"},
                    new[]{"15:30", "Javier Vargas · 408", "Gimnasio · Entrenamiento", "Daniel V. · Con cobro", "Por confirmar"},
                    new[]{"17:00", "Carlos Rojas · 118", "Gimnasio · Entrada sala fitness", "Control aforo · Gratuito", "Confirmada"}},
                Fields=new[]{
                    new FieldDefinition("Huésped y habitación", "text", "Ej. María Fernández · Hab. 204"),
                    new FieldDefinition("Servicio", "choice", "", "Spa · Masaje relajante 60 min", "Spa · Circuito hidrotermal 45 min", "Gimnasio · Acceso gratuito", "Gimnasio · Acceso con cobro"),
                    new FieldDefinition("Terapeuta o responsable", "choice", "", "Camila R. (Spa)", "Andrés M. (Hidroterapia)", "Daniel V. (Fitness)"),
                    new FieldDefinition("Fecha", "date", "dd/MM/aaaa"),
                    new FieldDefinition("Hora", "time", "HH:mm"),
                    new FieldDefinition("Modalidad de cobro", "choice", "", "Cargo al folio", "Cobro directo", "Incluido en estancia"),
                    new FieldDefinition("Notas de salud o preferencias", "optional", "Presión arterial, lesiones o preferencias (opcional)")}},
            new ModuleDefinition { Key="Lavanderia", Title="Lavandería", Description="Registro de prendas por huésped y habitación con precios, estados, cargo al folio y control de daños o pérdidas (RF-40).", Action="Nueva orden",
                Icon="M8,3 L10,6 H14 L16,3 L22,7 L18,11 V22 H6 V11 L2,7 Z",
                Metrics=new[]{"9|Órdenes activas", "24|Prendas registradas", "0|Daños o pérdidas hoy"},
                Columns=new[]{"Orden", "Habitación / huésped", "Prendas / importe", "Cobro / entrega", "Estado"},
                Rows=new[]{
                    new[]{"LAV-041", "204 · María Fernández", "3 camisas, 2 pantalones · $18.00", "Cargo al folio · 17:00", "En lavado"},
                    new[]{"LAV-040", "415 · Diego Salazar", "1 traje exprés · $22.00", "Cargo al folio · 14:00", "En planchado"},
                    new[]{"LAV-039", "305 · Roberto Núñez", "4 prendas estándar · $14.00", "Pago directo · 12:00", "Lista para entregar"},
                    new[]{"LAV-038", "210 · Lucía Gómez", "2 vestidos delicados · $20.00", "Cargo al folio · 11:00", "Entregada"}},
                Fields=new[]{
                    new FieldDefinition("Habitación", "text", "Ej. 204"),
                    new FieldDefinition("Nombre del huésped", "text", "Ej. María Fernández"),
                    new FieldDefinition("Cantidad de prendas", "integer", "Ej. 5 (entre 1 y 100)"),
                    new FieldDefinition("Tipo de servicio", "choice", "", "Estándar", "Exprés", "Delicado / tintorería"),
                    new FieldDefinition("Precio total (USD)", "amount", "Ej. 18.00"),
                    new FieldDefinition("Modalidad de cobro", "choice", "", "Cargo al folio", "Pago directo"),
                    new FieldDefinition("Detalle de prendas y estado inicial", "notes", " Lista de prendas, colores u observaciones previas")}},
            new ModuleDefinition { Key="Finanzas", Title="Finanzas", Description="Pagos, devoluciones autorizadas, facturación electrónica separada por rubro, convenios, arqueo de caja y auditoría nocturna (RF-41 a RF-47).", Action="Registrar pago",
                Icon="M3,18 L9,12 L13,16 L21,5 M15,5 H21 V11",
                Metrics=new[]{"$3,920.10|Caja del turno", "$1,516.50|Cuentas por cobrar", "4|Facturas electrónicas hoy"},
                Columns=new[]{"Documento", "Concepto / titular", "Método / rubro", "Importe", "Estado"},
                Rows=new[]{
                    new[]{"FAC-901", "Javier Vargas · Hab. 408", "Tarjeta · Habitación + F&B", "$177.00", "Por cobrar"},
                    new[]{"CXC-112", "Eco Tours S.A. · Convenio", "Crédito convenio · Cuenta maestra", "$1,344.00", "Por cobrar"},
                    new[]{"PAG-306", "Andes Corp. · Anticipo", "Transferencia · Comprobante #884", "$672.00", "Conciliado"},
                    new[]{"PAG-305", "Lucía Gómez · Hab. 210", "Mixto (Efectivo + Tarjeta)", "$297.00", "Conciliado"},
                    new[]{"FAC-899", "Diego Salazar · Hab. 415", "Tarjeta · Servicios Spa", "$65.00", "Por facturar"}},
                Fields=new[]{
                    new FieldDefinition("Folio o cuenta maestra", "text", "Ej. F-204 o FM-007"),
                    new FieldDefinition("Titular o empresa", "text", "Ej. María Fernández o Andes Corp."),
                    new FieldDefinition("Importe (USD)", "amount", "Ej. 177.00"),
                    new FieldDefinition("Método de pago", "choice", "", "Efectivo", "Tarjeta", "Transferencia", "Convenio corporativo", "Pago mixto"),
                    new FieldDefinition("Comprobante de trazabilidad", "text", "Ej. Voucher #44910 o transferencia")}},
            new ModuleDefinition { Key="Reportes", Title="Reportes", Description="Dashboard gerencial en tiempo real (Ocupación, ADR, RevPAR), reportes estratégicos, operativos, normativos y envíos PDF/Excel (RF-55 a RF-59).", Action="Exportar reporte",
                Icon="M5,2 H14 L20,8 V22 H5 Z M14,2 V8 H20 M9,17 V13 M13,17 V11 M17,17 V14",
                Metrics=new[]{"82%|Ocupación actual", "$148.50|ADR (Tarifa media)", "$121.77|RevPAR", "$3,920.10|Ingresos del día"},
                Columns=new[]{"Reporte", "Alcance / indicadores", "Período", "Formato", "Estado"},
                Rows=new[]{
                    new[]{"Estratégico · Forecast", "Ocupación prevista, ADR, RevPAR y canales", "Noviembre 2026", "PDF / Excel", "Disponible"},
                    new[]{"Estratégico · Convenios", "Consumo, ocupación y saldo por empresa", "Octubre 2026", "PDF / Excel", "Disponible"},
                    new[]{"Operativo diario", "Llegadas, salidas, in-house, transporte y caja", "31 oct 2026", "PDF / Excel", "Disponible"},
                    new[]{"Cumplimiento normativo", "Libro de viajeros y auditoría fiscal local", "Octubre 2026", "PDF / Excel", "Disponible"}},
                Fields=new[]{
                    new FieldDefinition("Categoría de reporte", "choice", "", "Operativo diario (RF-57)", "Ocupación y forecast (RF-56)", "Ingresos por concepto y canal (RF-56)", "Rentabilidad por tarifa y paquete (RF-56)", "Consumo y saldo por convenio (RF-56)", "Cumplimiento normativo autoridades (RF-58)"),
                    new FieldDefinition("Desde", "date", "dd/MM/aaaa"),
                    new FieldDefinition("Hasta", "date", "dd/MM/aaaa"),
                    new FieldDefinition("Formato de exportación", "choice", "", "PDF", "Excel (.xlsx)")}},
            new ModuleDefinition { Key="Config", Title="Ajustes", Description="Parámetros generales del hotel, moneda principal única, plantillas con variables, impuestos, políticas y asistente inicial (RF-48 a RF-54).",
                Icon="M12,8 A4,4 0 1 0 12,16 A4,4 0 1 0 12,8 M12,2 V5 M12,19 V22 M2,12 H5 M19,12 H22 M5,5 L7,7 M17,17 L19,19 M5,19 L7,17 M17,7 L19,5" }
        };
    }
}
