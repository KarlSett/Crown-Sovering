using System.Collections.Generic;

namespace Crown__.Frontend.Views
{
    // Replace these fixtures with backend view models. No domain operations or persistence live here.
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
        { Label = label; Kind = kind; Hint = hint; Options = options; }
    }

    internal static class ModuleCatalog
    {
        static ModuleCatalog()
        {
            All.AddRange(OperationsCatalog.Modules);
            var reservation = All.Find(m => m.Key == "Reservas");
            reservation.Fields = new[] { new FieldDefinition("Nombre del huésped"), new FieldDefinition("Correo de contacto", "email", "nombre@correo.com"),
                new FieldDefinition("Llegada", "date", "dd/MM/aaaa"), new FieldDefinition("Salida", "date", "dd/MM/aaaa"),
                new FieldDefinition("Tipo de habitación", "choice", "", "Individual", "Doble", "Matrimonial", "Triple", "Suite", "Familiar"),
                new FieldDefinition("Número de huéspedes", "integer", "1 a 6"), new FieldDefinition("Tarifa por noche (USD)", "amount", "0.00"),
                new FieldDefinition("Canal de origen", "choice", "", "Teléfono", "WhatsApp", "Correo", "Mostrador", "Agencia", "Empresa"),
                new FieldDefinition("Habitación preferida", "optional", "Ej. 204"),
                new FieldDefinition("Observaciones", "optional") };
        }
        internal static readonly List<ModuleDefinition> All = new List<ModuleDefinition>
        {
            new ModuleDefinition { Key="Inicio", Title="Buenos días, Recepción", Description="Sábado, 31 de octubre de 2026 · Resumen del turno de muestra",
                Icon="M3,10 L12,3 L21,10 V21 H15 V14 H9 V21 H3 Z" },
            new ModuleDefinition { Key="Reservas", Title="Reservas", Description="Estancias próximas, disponibilidad y solicitudes de huéspedes.", Action="Nueva reserva",
                Icon="M4,5 H20 V21 H4 Z M4,10 H20 M8,2 V7 M16,2 V7 M8,14 H11 M14,17 H17",
                Metrics=new[]{"14|Llegadas hoy", "6|Por confirmar", "8|Confirmadas"},
                Columns=new[]{"Reserva", "Huésped / canal", "Estancia", "Habitación", "Estado"},
                Rows=new[]{
                    new[]{"RES-1048", "María Fernández · Booking", "31 oct – 03 nov", "204 · Deluxe", "Confirmada"},
                    new[]{"RES-1049", "Carlos Rojas · Directa", "31 oct – 02 nov", "118 · Clásica", "Por confirmar"},
                    new[]{"RES-1050", "Eco Tours S.A. · Agencia", "31 oct – 04 nov", "6 habitaciones", "Confirmada"},
                    new[]{"RES-1051", "Lucía Gómez · Teléfono", "31 oct – 01 nov", "210 · Deluxe", "Confirmada"},
                    new[]{"RES-1052", "Diego Salazar · Correo", "31 oct – 05 nov", "415 · Suite", "Por confirmar"},
                    new[]{"RES-1053", "Ana Mendoza · Directa", "01 nov – 03 nov", "122 · Clásica", "Cancelada"}},
                Fields=new[]{new FieldDefinition("Nombre del huésped", hint:"Nombre completo"), new FieldDefinition("Correo de contacto", "email", "nombre@correo.com"),
                    new FieldDefinition("Llegada", "date", "dd/MM/aaaa"), new FieldDefinition("Salida", "date", "dd/MM/aaaa"),
                    new FieldDefinition("Tipo de habitación", "choice", "", "Clásica", "Deluxe", "Suite"), new FieldDefinition("Número de huéspedes", "integer", "1 a 6")}},
            new ModuleDefinition { Key="Habitaciones", Title="Habitaciones", Description="Disponibilidad y coordinación de limpieza por piso.", Action="Solicitar mantenimiento",
                Icon="M3,18 V7 M21,18 V7 M3,15 H21 M3,9 H21 V15 M8,9 V15",
                Metrics=new[]{"96|Ocupadas", "14|Disponibles", "6|En limpieza", "2|Mantenimiento"},
                Columns=new[]{"Habitación", "Categoría", "Piso", "Asignación", "Estado"},
                Rows=new[]{new[]{"101", "Clásica · 2 personas", "Piso 1", "Sin asignar", "Disponible"}, new[]{"118", "Clásica · 2 personas", "Piso 1", "Carlos Rojas", "Disponible"},
                    new[]{"122", "Clásica · 2 personas", "Piso 1", "Sin asignar", "En limpieza"}, new[]{"201", "Deluxe · 3 personas", "Piso 2", "Familia Herrera", "Ocupada"},
                    new[]{"204", "Deluxe · 2 personas", "Piso 2", "María Fernández", "Disponible"}, new[]{"210", "Deluxe · 2 personas", "Piso 2", "Lucía Gómez", "Disponible"},
                    new[]{"305", "Deluxe · 2 personas", "Piso 3", "Roberto Núñez", "Ocupada"}, new[]{"307", "Deluxe · 3 personas", "Piso 3", "Eco Tours S.A.", "Ocupada"},
                    new[]{"408", "Suite · 4 personas", "Piso 4", "Javier Vargas", "Ocupada"}, new[]{"410", "Suite · 4 personas", "Piso 4", "Sin asignar", "En limpieza"},
                    new[]{"412", "Suite · 4 personas", "Piso 4", "Revisión de climatización", "Mantenimiento"}, new[]{"415", "Suite · 4 personas", "Piso 4", "Diego Salazar", "Disponible"}},
                Fields=new[]{new FieldDefinition("Habitación", "choice", "", "101", "118", "122", "201", "204", "210", "305", "307", "408", "410", "412", "415"),
                    new FieldDefinition("Prioridad", "choice", "", "Normal", "Alta", "Urgente"), new FieldDefinition("Descripción de la incidencia", "notes", "Describe la revisión necesaria")}},
            new ModuleDefinition { Key="CheckInOut", Title="Check-in / check-out", Description="Revisa las llegadas, salidas y pendientes antes de cada movimiento.", Action="Preparar movimiento",
                Icon="M3,3 H12 V21 H3 Z M9,12 H22 M18,8 L22,12 L18,16",
                Metrics=new[]{"14|Llegadas", "11|Salidas", "3|Saldos por revisar"},
                Columns=new[]{"Habitación", "Huésped", "Hora prevista", "Pendiente", "Estado"},
                Rows=new[]{new[]{"204", "María Fernández", "13:30", "Documento de identidad", "Llegada"}, new[]{"118", "Carlos Rojas", "14:00", "Confirmar datos", "Llegada"},
                    new[]{"307", "Eco Tours S.A.", "14:45", "Lista del grupo", "Llegada"}, new[]{"210", "Lucía Gómez", "15:20", "Firma de registro", "Llegada"},
                    new[]{"201", "Familia Herrera", "11:00", "$24.00 · Consumos abiertos", "Salida"}, new[]{"305", "Roberto Núñez", "12:30", "$0.00 · Late check-out", "Salida"},
                    new[]{"408", "Javier Vargas", "11:45", "$148.50 · Revisar cuenta", "Salida"}},
                Fields=new[]{new FieldDefinition("Movimiento", "choice", "", "Check-in", "Check-out"), new FieldDefinition("Reserva o habitación", hint:"Ej. RES-1048 o 204"),
                    new FieldDefinition("Nombre del huésped", hint:"Nombre completo"), new FieldDefinition("Documento de identidad", hint:"Número de documento"), new FieldDefinition("Observaciones", "optional", "Información para recepción (opcional)")}},
            new ModuleDefinition { Key="Huespedes", Title="Huéspedes", Description="Directorio de contactos, estancias y preferencias de atención.", Action="Nuevo huésped",
                Icon="M12,3 A4,4 0 1 0 12,11 A4,4 0 1 0 12,3 M4,21 V19 C4,12 20,12 20,19 V21",
                Metrics=new[]{"96|Huéspedes alojados", "18|Recurrentes", "7|Atención preferente"},
                Columns=new[]{"Ficha", "Huésped", "Contacto", "Estancias", "Estado"},
                Rows=new[]{new[]{"H-001", "María Fernández", "maria@example.com", "3 estancias", "Próxima llegada"}, new[]{"H-002", "Carlos Rojas", "+591 7000 0102", "1 estancia", "Próxima llegada"},
                    new[]{"H-003", "Lucía Gómez", "lucia@example.com", "8 estancias", "Recurrente"}, new[]{"H-004", "Diego Salazar", "diego@example.com", "12 estancias", "VIP"},
                    new[]{"H-005", "Javier Vargas", "+591 7000 0105", "2 estancias", "Alojado"}},
                Fields=new[]{new FieldDefinition("Nombre completo", hint:"Nombres y apellidos"), new FieldDefinition("Documento de identidad", hint:"Número de documento"), new FieldDefinition("Correo", "email", "nombre@correo.com"),
                    new FieldDefinition("Teléfono", "phone", "+591 7000 0000"), new FieldDefinition("Preferencias", "optional", "Preferencias de atención (opcional)")}},
            new ModuleDefinition { Key="Convenios", Title="Convenios", Description="Acuerdos corporativos, tarifas negociadas y próximas renovaciones.", Action="Nuevo convenio",
                Icon="M3,7 H21 V21 H3 Z M8,7 V3 H16 V7 M3,12 H21 M10,12 V15 H14 V12",
                Metrics=new[]{"12|Convenios activos", "3|Por renovar", "24|Noches corporativas hoy"},
                Columns=new[]{"Convenio", "Empresa / contacto", "Tarifa por noche", "Vigencia hasta", "Estado"},
                Rows=new[]{new[]{"CON-012", "Andes Corp. · Paula Ríos", "$125.00", "06 nov 2026", "Por renovar"}, new[]{"CON-011", "Eco Tours S.A. · Luis Pardo", "$112.00", "31 dic 2026", "Activo"},
                    new[]{"CON-010", "Altiplano Business · Eva Soto", "$130.00", "31 mar 2027", "Activo"}, new[]{"CON-009", "Viajes del Sur · Mateo Díaz", "$118.00", "30 sep 2026", "Vencido"}},
                Fields=new[]{new FieldDefinition("Empresa", hint:"Razón social"), new FieldDefinition("Contacto", hint:"Nombre del responsable"), new FieldDefinition("Correo", "email", "contacto@empresa.com"),
                    new FieldDefinition("Tarifa por noche (USD)", "amount", "0.00"), new FieldDefinition("Vigencia hasta", "date", "dd/MM/aaaa")}},
            new ModuleDefinition { Key="FNB", Title="Alimentos y bebidas", Description="Comandas del restaurante, servicio a la habitación y minibar.", Action="Nueva comanda",
                Icon="M4,3 V10 Q7,14 10,10 V3 M7,3 V22 M18,3 Q13,7 18,13 V22 M18,3 V13",
                Metrics=new[]{"8|Comandas abiertas", "$486.00|Consumos del turno", "3|Entregas pendientes"},
                Columns=new[]{"Comanda", "Destino / huésped", "Servicio", "Importe", "Estado"},
                Rows=new[]{new[]{"FB-028", "204 · María Fernández", "Room service · Almuerzo", "$32.00", "En preparación"}, new[]{"FB-027", "Mesa 06 · Restaurante", "Menú del día × 2", "$48.00", "Por entregar"},
                    new[]{"FB-026", "201 · Familia Herrera", "Minibar · Bebidas", "$24.00", "Por revisar"}, new[]{"FB-025", "Mesa 02 · Restaurante", "Desayuno × 3", "$54.00", "Entregada"},
                    new[]{"FB-024", "408 · Javier Vargas", "Room service · Café", "$8.50", "Entregada"}},
                Fields=new[]{new FieldDefinition("Destino", hint:"Habitación o mesa"), new FieldDefinition("Servicio", "choice", "", "Restaurante", "Room service", "Minibar"),
                    new FieldDefinition("Detalle del pedido", "notes", "Productos y cantidades"), new FieldDefinition("Importe (USD)", "amount", "0.00"), new FieldDefinition("Indicaciones", "optional", "Alergias o indicaciones (opcional)")}},
            new ModuleDefinition { Key="GymSpa", Title="Gimnasio y spa", Description="Agenda de bienestar y disponibilidad de espacios.", Action="Agendar servicio",
                Icon="M12,2 C9,7 5,11 5,16 A7,7 0 0 0 19,16 C19,11 15,7 12,2 Z",
                Metrics=new[]{"18 / 20|Aforo del gimnasio", "6|Citas de spa hoy", "2|Cabinas disponibles"},
                Columns=new[]{"Hora", "Huésped / habitación", "Servicio", "Responsable", "Estado"},
                Rows=new[]{new[]{"10:00", "Lucía Gómez · 210", "Masaje relajante · 60 min", "Camila R.", "Finalizada"}, new[]{"11:30", "Diego Salazar · 415", "Circuito de spa · 45 min", "Andrés M.", "En curso"},
                    new[]{"14:00", "María Fernández · 204", "Masaje relajante · 60 min", "Camila R.", "Confirmada"}, new[]{"15:30", "Javier Vargas · 408", "Entrenamiento · 45 min", "Daniel V.", "Por confirmar"},
                    new[]{"17:00", "Carlos Rojas · 118", "Circuito de spa · 45 min", "Andrés M.", "Confirmada"}},
                Fields=new[]{new FieldDefinition("Huésped / habitación", hint:"Nombre y habitación"), new FieldDefinition("Servicio", "choice", "", "Masaje relajante · 60 min", "Circuito de spa · 45 min", "Entrenamiento · 45 min"),
                    new FieldDefinition("Fecha", "date", "dd/MM/aaaa"), new FieldDefinition("Hora", "time", "HH:mm"), new FieldDefinition("Preferencias", "optional", "Indicaciones de atención (opcional)")}},
            new ModuleDefinition { Key="Lavanderia", Title="Lavandería", Description="Seguimiento de prendas y entregas a las habitaciones.", Action="Nueva orden",
                Icon="M8,3 L10,6 H14 L16,3 L22,7 L18,11 V22 H6 V11 L2,7 Z",
                Metrics=new[]{"9|Órdenes en proceso", "24|Prendas por entregar", "3|Servicios exprés"},
                Columns=new[]{"Orden", "Habitación / huésped", "Prendas", "Entrega prevista", "Estado"},
                Rows=new[]{new[]{"LAV-041", "204 · María Fernández", "3 camisas · 2 pantalones", "31 oct · 17:00", "En lavado"}, new[]{"LAV-040", "415 · Diego Salazar", "1 traje · Exprés", "31 oct · 14:00", "En planchado"},
                    new[]{"LAV-039", "305 · Roberto Núñez", "4 prendas · Estándar", "31 oct · 12:00", "Lista para entregar"}, new[]{"LAV-038", "210 · Lucía Gómez", "2 vestidos · Delicado", "31 oct · 11:00", "Entregada"}},
                Fields=new[]{new FieldDefinition("Habitación", hint:"Ej. 204"), new FieldDefinition("Nombre del huésped", hint:"Nombre completo"), new FieldDefinition("Cantidad de prendas", "integer", "1 a 100"),
                    new FieldDefinition("Servicio", "choice", "", "Estándar", "Exprés", "Delicado"), new FieldDefinition("Detalle de prendas", "notes", "Tipo, color e indicaciones")}},
            new ModuleDefinition { Key="Finanzas", Title="Finanzas", Description="Caja del turno, movimientos y documentos pendientes.", Action="Preparar movimiento",
                Icon="M3,18 L9,12 L13,16 L21,5 M15,5 H21 V11",
                Metrics=new[]{"$3,920.10|Caja del turno", "$172.50|Saldos pendientes", "4|Facturas por emitir"},
                Columns=new[]{"Documento", "Concepto / titular", "Método", "Importe", "Estado"},
                Rows=new[]{new[]{"MOV-308", "Alojamiento · Javier Vargas", "Tarjeta", "$148.50", "Por cobrar"}, new[]{"MOV-307", "Minibar · Familia Herrera", "Cargo a habitación", "$24.00", "Por cobrar"},
                    new[]{"MOV-306", "Anticipo · Eco Tours S.A.", "Transferencia", "$672.00", "Conciliado"}, new[]{"MOV-305", "Alojamiento · Lucía Gómez", "Efectivo", "$297.00", "Conciliado"},
                    new[]{"MOV-304", "Servicio spa · Diego Salazar", "Tarjeta", "$65.00", "Por facturar"}},
                Fields=new[]{new FieldDefinition("Concepto", hint:"Descripción del movimiento"), new FieldDefinition("Titular / habitación", hint:"Nombre o número de habitación"),
                    new FieldDefinition("Importe (USD)", "amount", "0.00"), new FieldDefinition("Método", "choice", "", "Efectivo", "Tarjeta", "Transferencia", "Cargo a habitación"),
                    new FieldDefinition("Referencia", "optional", "Número de comprobante (opcional)")}},
            new ModuleDefinition { Key="Reportes", Title="Reportes", Description="Indicadores de alojamiento y resúmenes operativos del período.", Action="Preparar reporte",
                Icon="M5,2 H14 L20,8 V22 H5 Z M14,2 V8 H20 M9,17 V13 M13,17 V11 M17,17 V14",
                Metrics=new[]{"82%|Ocupación de muestra", "$148.50|Tarifa media diaria", "$121.77|Ingreso por habitación"},
                Columns=new[]{"Reporte", "Contenido", "Período", "Formato", "Estado"},
                Rows=new[]{new[]{"Ocupación", "Habitaciones y categorías", "25 – 31 oct 2026", "PDF / Excel", "Disponible"}, new[]{"Ingresos", "Alojamiento y servicios", "Octubre 2026", "PDF / Excel", "Disponible"},
                    new[]{"Caja", "Movimientos por turno", "31 oct 2026", "PDF / Excel", "Disponible"}, new[]{"Housekeeping", "Limpieza y mantenimiento", "31 oct 2026", "PDF / Excel", "Disponible"}},
                Fields=new[]{new FieldDefinition("Tipo de reporte", "choice", "", "Ocupación", "Ingresos", "Caja", "Housekeeping"), new FieldDefinition("Desde", "date", "dd/MM/aaaa"),
                    new FieldDefinition("Hasta", "date", "dd/MM/aaaa"), new FieldDefinition("Formato", "choice", "", "PDF", "Excel")}},
            new ModuleDefinition { Key="Config", Title="Ajustes", Description="Preferencias de apariencia, recepción y notificaciones.",
                Icon="M12,8 A4,4 0 1 0 12,16 A4,4 0 1 0 12,8 M12,2 V5 M12,19 V22 M2,12 H5 M19,12 H22 M5,5 L7,7 M17,17 L19,19 M5,19 L7,17 M17,7 L19,5" }
        };
    }
}
