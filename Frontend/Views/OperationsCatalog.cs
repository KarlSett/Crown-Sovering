using System.Collections.Generic;
using System.Linq;

namespace Crown__.Frontend.Views
{
    internal sealed class WorkflowDefinition
    {
        internal string Key, Module, Title, Description, Permission, Requirements;
        internal FieldDefinition[] Fields;
        internal bool Guided;
    }

    // UI specification and synthetic examples. No availability, accounting or persistence logic.
    internal static class OperationsCatalog
    {
        private const string TaskIcon = "M5,3 H19 V21 H5 Z M8,8 H16 M8,12 H16 M8,16 H13";
        private static FieldDefinition F(string name, string kind = "text", string hint = "") { return new FieldDefinition(name, kind, hint); }
        private static FieldDefinition C(string name, params string[] options) { return new FieldDefinition(name, "choice", "", options); }
        private static FieldDefinition Date(string name) { return F(name, "date", "dd/MM/aaaa"); }
        private static FieldDefinition Money(string name)
        {
            bool zeroAllowed = new[]{"Saldo", "Comisión", "Límite", "Fondo", "Efectivo contado", "Tarjetas", "Paquetes de referencia", "Impuestos de referencia", "Porcentaje", "Cargo / umbral"}.Any(prefix => name.StartsWith(prefix));
            return F(name, zeroAllowed ? "nonnegative" : "amount", "0.00");
        }
        private static WorkflowDefinition W(string module, string key, string title, string permission, string rf, string description, params FieldDefinition[] fields)
        { return new WorkflowDefinition { Module = module, Key = key, Title = title, Permission = permission, Requirements = rf, Description = description, Fields = fields }; }
        private static ModuleDefinition M(string key, string title, string description, string action, string[] metrics, string[] columns, string[][] rows, params FieldDefinition[] fields)
        { return new ModuleDefinition { Key = key, Title = title, Description = description, Action = action, Metrics = metrics, Columns = columns, Rows = rows, Fields = fields, Icon = TaskIcon }; }

        internal static readonly ModuleDefinition[] Modules = {
            M("Housekeeping", "Housekeeping", "Limpieza, cobertura nocturna e inspecciones por habitación.", "Preparar tarea",
                new[]{"6|Limpiezas pendientes", "2|Inspecciones", "1|Fuera de horario"},
                new[]{"Tarea", "Habitación / tipo", "Responsable", "Horario", "Estado"},
                new[]{new[]{"HK-041", "410 · Suite de lujo", "Elena Ríos", "10:00–11:00", "Inspección pendiente"}, new[]{"HK-042", "122 · Doble estándar", "Sofía Lima", "11:00–11:40", "En limpieza"}, new[]{"HK-043", "204 · Doble deluxe", "Elena Ríos", "12:00–12:40", "Pendiente"}},
                F("Habitación"), C("Servicio", "Limpieza diaria", "A pedido", "Turndown"), F("Responsable"), F("Hora prevista", "time", "HH:mm")),
            M("Mantenimiento", "Mantenimiento", "Incidencias, prioridades y habitaciones fuera de servicio.", "Nueva incidencia",
                new[]{"3|Órdenes abiertas", "1|Prioridad crítica", "2|En revisión"},
                new[]{"Orden", "Habitación / avería", "Responsable", "Prioridad", "Estado"},
                new[]{new[]{"MT-019", "412 · Climatización", "Mario León", "Crítica", "Bloqueada"}, new[]{"MT-018", "210 · Grifería", "Ana Paredes", "Media", "En revisión"}, new[]{"MT-017", "Gimnasio · Caminadora", "Mario León", "Alta", "Pendiente"}},
                F("Habitación o área"), C("Prioridad", "Baja", "Media", "Alta", "Crítica"), F("Incidencia", "notes"), F("Responsable")),
            M("Transporte", "Transporte", "Llegadas al aeropuerto, rutas y coordinación de conductores.", "Programar traslado",
                new[]{"4|Traslados del día", "1|En ruta", "2|Por asignar"},
                new[]{"Traslado", "Pasajero / destino", "Vuelo / hora", "Conductor", "Estado"},
                new[]{new[]{"TR-071", "María Fernández · Hotel", "OB-604 · 13:00", "Luis Vega · 2040-ABC", "En ruta"}, new[]{"TR-072", "Eco Tours · Aeropuerto", "LA-365 · 16:30", "Por asignar", "Programado"}, new[]{"TR-070", "Carlos Rojas · Hotel", "OB-602 · 09:15", "Luis Vega · 2040-ABC", "Completado"}},
                F("Huésped / habitación"), C("Trayecto", "Aeropuerto a hotel", "Hotel a aeropuerto"), F("Vuelo"), Date("Fecha"), F("Hora", "time", "HH:mm"), F("Conductor / vehículo"), Money("Costo"), Money("Comisión de agencia"), C("Pago", "Cargo al folio", "Directo al transportista")),
            M("Tarifas", "Tarifas y paquetes", "Temporadas, promociones y simulación de precios.", "Preparar tarifa",
                new[]{"6|Tarifas vigentes", "3|Paquetes", "2|Temporadas"},
                new[]{"Plan", "Habitación / canal", "Vigencia", "Precio base", "Estado"},
                new[]{new[]{"BAR-01", "Doble deluxe · Directo", "01 oct – 30 nov", "$148.50", "Activa"}, new[]{"COR-02", "Suite · Empresas", "01 nov – 31 dic", "$210.00", "Programada"}, new[]{"PKG-03", "Estancia y bienestar", "01 oct – 31 dic", "$185.00", "Activa"}},
                F("Nombre del plan"), C("Capacidad", "Individual", "Doble", "Matrimonial", "Triple", "Suite", "Familiar"), C("Canal", "Directo", "Teléfono", "WhatsApp", "Correo", "Mostrador", "Agencia", "Empresa"), Date("Desde"), Date("Hasta"), Money("Tarifa base")),
            M("Folios", "Folios y cuentas", "Detalle de alojamiento, consumos, abonos y saldos de muestra.", "Preparar cargo",
                new[]{"42|Folios abiertos", "3|Cuentas maestras", "5|Por revisar"},
                new[]{"Folio", "Titular / habitación", "Cargos", "Saldo pendiente", "Estado"},
                new[]{new[]{"F-204", "María Fernández · 204", "$478.50", "$178.50", "Abierto"}, new[]{"F-201", "Familia Herrera · 201", "$321.00", "$24.00", "Por revisar"}, new[]{"FM-007", "Eco Tours · Cuenta maestra", "$2,016.00", "$1,344.00", "Abierto"}},
                F("Folio"), C("Concepto", "Alojamiento", "Restaurante", "Minibar", "Spa", "Gimnasio", "Lavandería", "Transporte", "Persona adicional"), Money("Importe"), F("Referencia"), F("Observaciones", "optional")),
            M("Inventario", "Inventario F&B", "Insumos, reposición de minibar y registro de mermas.", "Preparar movimiento",
                new[]{"3|Insumos bajo mínimo", "2|Reposiciones", "1|Merma por revisar"},
                new[]{"Producto", "Descripción / almacén", "Existencia", "Mínimo", "Estado"},
                new[]{new[]{"INS-021", "Café en grano · Cocina", "4 kg", "5 kg", "Bajo mínimo"}, new[]{"INS-014", "Agua mineral · Minibar", "72 unidades", "48 unidades", "Disponible"}, new[]{"INS-006", "Fruta de temporada · Cocina", "8 kg", "10 kg", "Bajo mínimo"}},
                F("Producto"), C("Movimiento", "Ingreso", "Reposición de minibar", "Consumo", "Merma"), F("Cantidad", "integer", "Unidades enteras"), F("Motivo", "notes")),
            M("Alertas", "Alertas y avisos", "Pendientes del área, prioridades y responsables de atención.", "Preparar aviso",
                new[]{"3|Pendientes del área", "1|Prioridad alta", "2|En seguimiento"},
                new[]{"Aviso", "Detalle", "Área", "Responsable", "Estado"},
                new[]{new[]{"AL-041", "Inspección de la suite 410", "Housekeeping", "Supervisión", "Alta"}, new[]{"AL-042", "Climatización de la 412", "Mantenimiento", "Mario León", "Alta"}, new[]{"AL-043", "Vuelo OB-604 próximo a llegar", "Transporte", "Luis Vega", "Pendiente"}, new[]{"AL-044", "Saldo del folio F-201", "Finanzas", "Caja", "Pendiente"}, new[]{"AL-045", "Llegada de grupo a las 14:45", "Recepción", "Turno de tarde", "Pendiente"}, new[]{"AL-046", "Reposición de café", "Alimentos y bebidas", "Cocina", "Pendiente"}, new[]{"AL-047", "Aforo del gimnasio próximo al límite", "Bienestar", "Recepción de spa", "Alta"}, new[]{"AL-048", "Entrega exprés de la 415", "Lavandería", "Turno de tarde", "Pendiente"}},
                F("Asunto"), F("Mensaje", "notes"), C("Prioridad", "Normal", "Alta", "Crítica"), F("Responsable")),
            M("Operacion", "Operación del turno", "Bitácora, llaves, solicitudes, incidencias y objetos olvidados.", "Nueva novedad",
                new[]{"5|Novedades", "2|Solicitudes abiertas", "1|Objeto por devolver"},
                new[]{"Registro", "Detalle / habitación", "Tipo", "Responsable", "Estado"},
                new[]{new[]{"OP-031", "204 · Preparar cuna", "Solicitud especial", "Recepción", "Pendiente"}, new[]{"OP-030", "305 · Cargador encontrado", "Objeto olvidado", "Custodia A-02", "En custodia"}, new[]{"OP-029", "201 · Revisión de ruido", "Queja", "Supervisión", "En seguimiento"}},
                C("Tipo", "Bitácora", "Solicitud especial", "Incidencia", "Queja", "Opinión"), F("Habitación / estancia"), F("Detalle", "notes"), F("Responsable")),
            M("Usuarios", "Usuarios y permisos", "Perfiles del personal, permisos y sesiones de muestra.", "Preparar usuario",
                new[]{"10|Perfiles de trabajo", "3|Usuarios de muestra", "0|Cambios guardados"},
                new[]{"Usuario", "Nombre", "Rol", "Último acceso", "Estado"},
                new[]{new[]{"U-001", "Adriana Pérez", "Administrador", "31 oct · 08:00", "Activo"}, new[]{"U-002", "Diego Flores", "Recepción", "31 oct · 08:15", "Activo"}, new[]{"U-003", "Elena Ríos", "Housekeeping", "31 oct · 08:30", "Activo"}},
                F("Nombre"), F("Correo", "email", "personal@hotel.com"), C("Rol", "Administrador", "Recepción", "Housekeeping", "Supervisión de pisos", "Mantenimiento", "Alimentos y bebidas", "Bienestar", "Lavandería", "Finanzas", "Transporte")),
            M("Auditoria", "Auditoría", "Consulta de ejemplos de trazabilidad. Registro real pendiente de integración.", "Preparar consulta",
                new[]{"3|Eventos ilustrativos", "2|Módulos", "0|Eventos reales"},
                new[]{"Evento", "Usuario / acción", "Módulo", "Fecha / motivo", "Estado"},
                new[]{new[]{"AUD-003", "Adriana Pérez · Ajuste", "Configuración", "31 oct 09:10 · Política", "Ejemplo"}, new[]{"AUD-002", "Diego Flores · Reasignación", "Habitaciones", "31 oct 08:45 · Preferencia", "Ejemplo"}, new[]{"AUD-001", "Elena Ríos · Cambio de estado", "Habitaciones", "31 oct 08:20 · Limpieza", "Ejemplo"}},
                Date("Desde"), Date("Hasta"), F("Usuario", "optional"), F("Módulo", "optional"))
        };

        internal static readonly WorkflowDefinition[] Workflows = {
            W("Reservas", "WalkIn", "Walk-in", "Write", "RF-02,07,08", "Prepara la llegada sin reserva; disponibilidad y tarifa deben validarse antes de asignar.", F("Huésped"), C("Tipo de habitación", "Individual", "Doble", "Matrimonial", "Triple", "Suite", "Familiar"), F("Habitación"), Date("Llegada"), Date("Salida"), Money("Tarifa aplicable")),
            W("Reservas", "Grupos", "Grupos y bloqueos corporativos", "Write", "RF-03,10", "Relaciona el bloqueo con una cuenta maestra y folios individuales.", F("Empresa / agencia"), F("Convenio"), F("Habitaciones solicitadas", "integer"), Date("Llegada"), Date("Salida"), F("Cuenta maestra"), F("Listado de huéspedes", "notes")),
            W("Reservas", "Importar", "Importar CSV / Excel", "Write", "RF-04", "Revisa la estructura y los errores por fila antes de preparar la importación. No se cargan archivos al servidor.", F("Archivo", "optional")),
            W("Reservas", "Calendario", "Calendario de ocupación", "View", "RF-05,07", "Consulta la distribución diaria, semanal o mensual por habitación y tipo."),
            W("Reservas", "Modificar", "Modificar o reasignar", "Write", "RF-06,08", "Documenta cambios de fechas y preferencias de asignación.", F("Reserva"), Date("Nueva llegada"), Date("Nueva salida"), F("Habitación preferida"), C("Asignación", "Manual", "Automática según preferencias"), F("Preferencias de piso, cama, vista y accesibilidad", "notes"), F("Motivo", "notes")),
            W("Reservas", "Cancelar", "Cancelar / no-show", "Cancel", "RF-06,80,99", "La anulación exige motivo. Los cargos y la liberación deben confirmarse en el servicio.", F("Reserva"), C("Operación", "Cancelar", "Marcar no-show"), C("Motivo", "Solicitud del huésped", "No presentación", "Duplicada", "Otro"), F("Justificación", "notes")),
            W("Reservas", "Espera", "Lista de espera", "Write", "RF-06,100", "Ordena solicitudes por prioridad; la conversión depende de disponibilidad real.", F("Huésped"), Date("Llegada"), Date("Salida"), C("Prioridad", "Normal", "Alta", "VIP"), F("Tipo y cantidad de habitaciones"), F("Contacto")),
            W("Habitaciones", "Ficha", "Clasificación y atributos", "Manage", "RF-14,15,16", "Define capacidad, calidad, salud y equipamiento.", F("Número"), F("Piso", "integer"), C("Capacidad", "Individual", "Doble", "Matrimonial", "Triple", "Suite", "Familiar"), C("Calidad", "Estándar", "Deluxe", "Premium", "Suite de lujo"), F("Camas, vista, balcón, jacuzzi y cocina", "notes"), F("Accesibilidad, cuna, purificador, libre de humo e hipoalergénica", "notes"), Money("Tarifa base")),
            W("Habitaciones", "Estados", "Estado de habitación", "Write", "RF-17,18,102", "La transición debe respetar limpieza, inspección y averías pendientes.", F("Habitación"), C("Nuevo estado", "Disponible", "Ocupada", "Sucia", "En limpieza", "Limpia", "Inspeccionada", "Fuera de servicio", "Mantenimiento", "Bloqueada"), F("Motivo", "notes")),
            W("Habitaciones", "Bloqueos", "Bloqueos con vigencia", "Manage", "RF-08,101", "Prepara un bloqueo y su motivo; no cambia el inventario real.", F("Habitación"), C("Causa", "Mantenimiento", "Convenio", "Cuarentena", "Otra"), Date("Desde"), Date("Hasta"), F("Motivo", "notes")),
            W("Housekeeping", "Checklist", "Limpieza y turndown", "Write", "RF-18,19,20,21", "Comprueba ropa de cama, baño, minibar, amenities y seguridad antes de solicitar revisión.", F("Habitación"), C("Servicio", "Limpieza diaria", "Limpieza a pedido", "Turndown"), F("Inicio", "time", "HH:mm"), F("Fin", "time", "HH:mm"), C("Checklist", "Completo", "Con pendientes"), F("Observaciones", "notes")),
            W("Housekeeping", "Asignar", "Asignación de tareas", "Manage", "RF-21", "Distribuye trabajo por responsable y horario.", F("Habitación"), F("Responsable"), Date("Fecha"), F("Hora prevista", "time", "HH:mm"), F("Checklist por tipo de habitación", "notes")),
            W("Housekeeping", "Inspeccion", "Inspección de supervisión", "Approve", "RF-20,22", "En categorías altas la inspección es obligatoria y exige firma. La validación horaria corresponde al servicio.", F("Habitación"), C("Resultado", "Aprobada", "Requiere nueva limpieza"), F("Firma / nombre del supervisor"), F("Observaciones", "notes")),
            W("Mantenimiento", "Orden", "Seguimiento y bloqueo crítico", "Write", "RF-23,101", "Registra diagnóstico, prioridad y estado de la orden.", F("Orden / habitación"), C("Prioridad", "Baja", "Media", "Alta", "Crítica"), C("Estado", "Pendiente", "En revisión", "En reparación", "Resuelta"), F("Diagnóstico y trabajo realizado", "notes"), F("Motivo del bloqueo o liberación", "notes")),
            W("CheckInOut", "CheckIn", "Check-in guiado", "Write", "RF-24,25,26,27,28,108,110,112", "Identidad, ocupantes y entrega. Los datos de historial se verificarán con el servicio.", F("Reserva / habitación"), C("Modalidad", "Estándar", "Express", "Anticipado"), F("Documento de identidad"), F("Huésped titular"), F("Acompañantes y menores", "notes"), F("Preferencias, alergias y notas del historial", "optional"), C("Adicionales", "Ninguno", "Cama extra", "Cuna"), F("Número de llave / tarjeta"), F("Hora de entrega", "time", "HH:mm")),
            W("CheckInOut", "CheckOut", "Check-out guiado", "Write", "RF-28,29,30,31,117,118", "Revisa folio, servicios, saldo, factura y devolución de llaves antes de solicitar el cierre.", F("Folio / habitación"), C("Modalidad", "Completo", "Parcial", "Express", "Late check-out"), F("Consumos pendientes revisados", "notes"), Money("Saldo de referencia"), C("Pago", "Sin pago pendiente", "Efectivo", "Tarjeta", "Transferencia", "Convenio", "Mixto"), F("Referencia de pago", "optional"), F("Correo de factura", "email"), C("Llaves devueltas", "Sí", "No"), F("Observaciones", "optional")),
            W("Folios", "Cuenta", "Cuentas maestras y folios", "Write", "RF-32,37,118", "Relaciona folios de huéspedes, grupos y convenios con trazabilidad.", F("Titular"), C("Tipo", "Individual", "Cuenta maestra de grupo", "Cuenta maestra de convenio"), F("Folios vinculados", "notes"), Money("Límite de crédito")),
            W("Folios", "Dividir", "Dividir folio", "Write", "RF-33", "Distribuye conceptos entre huéspedes o empresa y huésped.", F("Folio de origen"), F("Titular de destino"), F("Conceptos a dividir", "notes"), Money("Importe"), F("Motivo", "notes")),
            W("Folios", "Transferir", "Transferir cargos", "Approve", "RF-34,81", "Solo los perfiles autorizados pueden preparar transferencias entre folios.", F("Folio de origen"), F("Folio de destino"), F("Cargo"), Money("Importe"), F("Motivo", "notes"), F("Referencia de autorización")),
            W("Convenios", "Contrato", "Condiciones y crédito", "Manage", "RF-35,36,37,38,39", "Define datos fiscales, descuentos, vigencia, crédito y renovación.", F("Empresa / agencia / operador"), F("Identificación fiscal"), F("Contacto"), F("Correo", "email"), C("Tarifa", "Fija", "Descuento porcentual"), Money("Tarifa o porcentaje"), Date("Desde"), Date("Hasta"), Money("Límite de crédito"), F("Condiciones de renovación", "notes")),
            W("Transporte", "Seguimiento", "Asignación y seguimiento", "Write", "RF-40,41,42", "Coordina el vehículo y el conductor; las notificaciones se enviarán desde el servicio.", F("Traslado"), F("Conductor"), F("Vehículo / placa"), C("Estado", "Programado", "En ruta", "Completado", "Cancelado"), F("Observaciones", "notes")),
            W("Huespedes", "Perfil", "Historial y preferencias", "Write", "RF-09,44,45,46,112,113,114", "Ficha de atención con documentos, contactos, segmentación y notas.", F("Huésped / documento"), C("Segmento", "Estándar", "VIP", "Recurrente", "Corporativo", "Agencia", "Grupo"), F("Empresa", "optional"), F("Preferencias y alergias", "notes"), F("Notas y opinión de estancia", "optional"), C("Consentimiento de datos", "Otorgado", "Pendiente", "Retirado")),
            W("Huespedes", "Privacidad", "Consentimiento y anonimización", "Privacy", "RF-47", "Una solicitud de anonimización requiere revisión de retención legal y autorización.", F("Ficha"), C("Solicitud", "Actualizar consentimiento", "Solicitar anonimización"), F("Motivo", "notes"), F("Referencia de autorización")),
            W("Huespedes", "Fusionar", "Revisar duplicados", "Manage", "RF-48", "Compara las fichas antes de solicitar su fusión; no se elimina información local.", F("Ficha principal"), F("Ficha duplicada"), F("Datos a conservar", "notes"), F("Motivo", "notes")),
            W("FNB", "Desayunos", "Desayunos y cocina", "Write", "RF-49,50,51", "Lista de preparación y acceso por tarifa. Ocupación e insumos son ejemplos.", Date("Fecha"), C("Servicio", "Salón básico", "Salón completo", "Room service"), F("Habitaciones / tarifas incluidas", "notes"), F("Raciones", "integer"), F("Insumos de preparación", "notes"), F("Horario", "text", "Ej. 07:00–10:30")),
            W("FNB", "Menu", "Menú y disponibilidad", "Manage", "RF-52", "Organiza precios, categorías y disponibilidad del restaurante.", F("Producto"), C("Categoría", "Desayuno", "Principal", "Bebida", "Postre"), Money("Precio"), C("Disponibilidad", "Disponible", "Agotado")),
            W("FNB", "Cobro", "Consumos y minibar", "Write", "RF-53,54,55,57", "Separa consumos de alojamiento; selecciona cargo al folio o pago inmediato.", F("Comanda / habitación"), F("Detalle de consumos", "notes"), Money("Importe"), C("Destino del cobro", "Folio", "Efectivo", "Tarjeta"), F("Referencia")),
            W("Inventario", "Minimos", "Stock mínimo y mermas", "Write", "RF-55,56", "Documenta reposición, insumos y pérdidas.", F("Producto"), F("Cantidad", "integer"), F("Stock mínimo", "integer"), F("Motivo / merma", "notes")),
            W("GymSpa", "Acceso", "Acceso y aforo del gimnasio", "Write", "RF-58,59", "La muestra indica 18 de 20 plazas. El aforo se comprobará con datos reales al integrar.", F("Huésped / habitación"), C("Movimiento", "Entrada", "Salida"), C("Acceso", "Gratuito", "Por día", "Membresía", "Cargo al folio"), F("Hora", "time", "HH:mm")),
            W("GymSpa", "Catalogo", "Servicios y tarifas de bienestar", "Manage", "RF-58,60,61,62", "Configura terapeutas, duración, aforo, promociones y horarios.", F("Servicio"), F("Terapeuta"), F("Duración en minutos", "integer"), F("Aforo máximo", "integer"), Money("Precio"), F("Horario promocional"), Date("Desde"), Date("Hasta")),
            W("Lavanderia", "Entrega", "Prendas y entrega", "Write", "RF-63", "Detalla cantidades, precios, estado y prendas dañadas o perdidas.", F("Orden / habitación"), F("Prendas y cantidades", "notes"), Money("Precio del servicio"), C("Estado", "Recibido", "En proceso", "Entregado", "Dañado", "Perdido"), C("Cobro", "Cargo al folio", "Pago directo"), F("Incidencia / observaciones", "optional")),
            W("Finanzas", "Pago", "Pagos y comprobantes", "Write", "RF-64", "Prepara el desglose de métodos de pago y su comprobante.", F("Folio / documento"), C("Método", "Efectivo", "Tarjeta", "Transferencia", "Convenio", "Mixto"), Money("Importe"), F("Desglose de pago mixto", "optional"), F("Referencia del comprobante")),
            W("Finanzas", "Devolucion", "Devoluciones y anulaciones", "Approve", "RF-65,80,81", "Motivo y autorización obligatorios. No ejecuta devoluciones.", F("Documento"), Money("Importe"), F("Motivo", "notes"), F("Referencia de autorización")),
            W("Finanzas", "Factura", "Facturas y cuentas por cobrar", "Write", "RF-57,66,67", "Prepara datos fiscales y separación de alojamiento, F&B, servicios e impuestos.", F("Folio / cuenta maestra"), F("Razón social"), F("Identificación fiscal"), F("Correo", "email"), F("Conceptos e impuestos desglosados", "notes"), C("Entrega", "Correo", "Impresión A4 / carta", "Recibo térmico")),
            W("Finanzas", "NotaCredito", "Notas de crédito", "Approve", "RF-68", "Solicita corrección de factura con autorización y motivo.", F("Factura original"), Money("Importe"), F("Corrección / motivo", "notes"), F("Referencia de autorización")),
            W("Finanzas", "Caja", "Apertura y cierre de caja", "Write", "RF-70,106", "Revisa el fondo, arqueo y diferencias antes de solicitar el cierre.", C("Movimiento", "Apertura", "Cierre"), F("Turno / responsable"), Money("Fondo inicial"), Money("Efectivo contado"), Money("Tarjetas / transferencias"), F("Conciliación y diferencias", "notes")),
            W("Tarifas", "Paquetes", "Paquetes y promociones", "Manage", "RF-12,61", "Desglosa alojamiento, desayuno, spa y traslado con condiciones de vigencia.", F("Nombre"), F("Componentes y precios", "notes"), Date("Desde"), Date("Hasta"), F("Condiciones", "notes")),
            W("Tarifas", "Reglas", "Temporadas y precio dinámico", "Manage", "RF-11,13,74", "Define vigencias, canales, días y reglas desactivables.", F("Temporada / feriado"), Date("Desde"), Date("Hasta"), F("Días y canales"), F("Reglas de ocupación, anticipación y demanda", "notes"), C("Estado", "Activa", "Desactivada")),
            W("Tarifas", "Simulador", "Simulador de precio", "Write", "RF-79", "Revisa los componentes del precio. El cálculo definitivo corresponde al motor de tarifas.", F("Plan / habitación"), Date("Llegada"), Date("Salida"), F("Canal"), Money("Alojamiento de referencia"), Money("Paquetes de referencia"), Money("Impuestos de referencia")),
            W("Config", "Hotel", "Datos del hotel", "Manage", "RF-71", "Identidad, contacto y configuración regional.", F("Nombre del hotel"), F("Logo / recurso", "optional"), F("Identificación fiscal"), F("Dirección y contacto", "notes"), C("Moneda", "BOB", "USD"), C("Idioma previsto", "Español", "Inglés"), F("Zona horaria", "text", "America/La_Paz")),
            W("Config", "Plantillas", "Plantillas y comunicaciones", "Manage", "RF-73,97", "Prepara facturas, reportes y mensajes con variables.", C("Tipo", "Factura", "Reporte", "Mensaje interno", "Correo"), F("Nombre"), F("Contenido con variables", "notes", "Ej. {huesped}, {habitacion}, {fecha}")),
            W("Config", "Impuestos", "Impuestos y métodos de pago", "Manage", "RF-75,76", "Define tasas, conceptos, orden y comisiones.", F("Nombre"), C("Tipo", "Impuesto", "Tasa turística", "Servicio", "Propina", "Método de pago"), Money("Porcentaje / comisión"), F("Conceptos aplicables", "notes"), F("Orden", "integer"), C("Estado", "Activo", "Inactivo")),
            W("Config", "Politicas", "Políticas y autorizaciones", "Manage", "RF-06,19,20,50,77,80,81,109", "Prepara reglas de ocupación, limpieza, cancelación, depósitos y cargos adicionales.", C("Categoría del hotel", "3 estrellas", "4 estrellas", "5 estrellas", "Lujo"), F("Política / canal / tarifa"), F("Regla y motivos permitidos", "notes"), Money("Cargo / umbral de autorización"), C("Sobreventa", "No permitida", "Requiere autorización"), F("Horario de limpieza y cobertura nocturna")),
            W("Config", "Asistente", "Puesta en marcha", "Manage", "RF-78", "Recorre hotel, habitaciones, tarifas y personal; cada paso se revisa sin guardar.", F("Nombre y datos fiscales del hotel"), F("Habitaciones y atributos", "notes"), F("Temporadas y tarifas", "notes"), F("Personal y roles", "notes")),
            W("Usuarios", "Roles", "Matriz de permisos", "Manage", "RF-72", "Consulta permisos de presentación por módulo y acción. La asignación real pertenece al servicio."),
            W("Usuarios", "Desactivar", "Desactivar usuario", "Manage", "RF-72", "Prepara la desactivación; no cambia cuentas reales.", F("Usuario"), F("Motivo", "notes")),
            W("Usuarios", "Sesiones", "Sesiones y cierre remoto", "Approve", "RNF-12", "El cierre remoto y la expiración se aplicarán en el servicio de sesión.", F("Usuario / sesión"), F("Motivo", "notes")),
            W("Reportes", "Operativo", "Reporte operativo diario", "Export", "RF-39,43,69,83,84,85,86,87,88,89,90,91,115,116", "Selecciona el área y el período para revisar parámetros; no genera archivos.", C("Reporte", "Operativo diario", "Ocupación por tipo, calidad y piso", "Ingresos / comisiones por canal", "Housekeeping y tiempos", "F&B / stock / mermas", "Gimnasio y spa", "Transporte", "Convenios y saldos", "Financiero / cuentas por cobrar", "Registro de huéspedes / viajeros", "Exportación para auditoría"), Date("Desde"), Date("Hasta"), C("Formato", "PDF", "Excel", "CSV"), F("Tipo / calidad / piso", "optional")),
            W("Reportes", "Programar", "Envíos programados", "Manage", "RF-92", "Prepara destinatarios, periodicidad y consulta del registro de envío.", F("Reporte"), F("Destinatario", "email"), C("Periodicidad", "Diaria", "Semanal", "Mensual"), F("Hora", "time", "HH:mm")),
            W("Alertas", "Seguimiento", "Atención y prioridad", "Write", "RF-93,94,95,96", "Solo se muestran avisos destinados a módulos accesibles por el perfil.", F("Aviso"), F("Responsable"), C("Estado", "Pendiente", "En atención", "Resuelta"), F("Seguimiento", "notes")),
            W("Operacion", "Llaves", "Llaves y tarjetas", "Write", "RF-27,105", "Lleva referencia de emisión, entrega, devolución, duplicados y pérdidas.", F("Habitación"), F("Llave / tarjeta"), C("Movimiento", "Emisión", "Entrega", "Devolución", "Duplicado", "Pérdida"), F("Responsable"), F("Hora", "time", "HH:mm"), F("Motivo", "notes")),
            W("Operacion", "Objetos", "Objetos olvidados", "Write", "RF-104", "Registra custodia y devolución con responsable.", F("Habitación"), F("Objeto"), F("Ubicación de custodia"), C("Estado", "En custodia", "Contactado", "Devuelto"), F("Detalle de devolución", "optional")),
            W("Operacion", "Solicitudes", "Solicitudes, quejas y bitácora", "Write", "RF-98,103,107,111,114", "Da continuidad al turno y a las necesidades de los huéspedes.", C("Tipo", "Bitácora", "Queja", "Incidencia", "Solicitud especial", "Opinión"), F("Habitación / estancia"), F("Responsable"), C("Estado", "Pendiente", "En seguimiento", "Resuelta"), F("Detalle", "notes")),
            W("Auditoria", "Consulta", "Consulta de trazabilidad", "View", "RF-82,102", "Revisa usuario, fecha, motivo y valores anterior / nuevo de ejemplos de auditoría.", Date("Desde"), Date("Hasta"), F("Usuario", "optional"), F("Módulo", "optional"))
        };

        static OperationsCatalog()
        {
            foreach (var workflow in Workflows.Where(w => w.Key == "CheckIn" || w.Key == "CheckOut" || w.Key == "Asistente")) workflow.Guided = true;
        }
    }
}
