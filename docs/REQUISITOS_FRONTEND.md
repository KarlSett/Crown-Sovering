# Requisitos y pantallas de frontend

Fuente: `Frontend/RF del proyecto grupal corregido.docx`. Este mapa distingue representación visual de ejecución real. Los listados usan datos sintéticos; las operaciones producen borradores revisables. Los roles y su matriz son una interpretación de responsabilidades, documentada en [Frontend/README.md](../Frontend/README.md).

| Requisitos | Pantallas y flujos | Integración pendiente |
| --- | --- | --- |
| RF-01–04 | Reservas: nueva reserva, walk-in, grupos/bloqueos e importación CSV/XLSX con errores por fila | Registro, asignación, disponibilidad y confirmación de filas |
| RF-05–08 | Reservas: calendario día/semana/mes, modificar/reasignar, cancelar/no-show y espera; Habitaciones: bloqueos | Calendario sincronizado, sobreventa, asignación y transiciones |
| RF-09–10 | Huéspedes: historial/preferencias; Reservas: grupos; Folios: cuentas maestras | Historial, relaciones y persistencia |
| RF-11–13 | Tarifas: planes, temporadas, reglas dinámicas y paquetes | Motor de cálculo y vigencias |
| RF-14–18 | Habitaciones: fichas con filtro por piso, clasificación/atributos, estado y detalle | Inventario completo, historial y mapa sincronizado |
| RF-19–23 | Housekeeping: limpieza/turndown, asignación e inspección; Mantenimiento: órdenes | Checklist configurable, alertas horarias, firma y bloqueo crítico |
| RF-24–31 | Check-in/out: tres pasos, modalidades express/anticipado/parcial/late, acompañantes, llaves, pago y factura | Identidad, historial, disponibilidad, cobro y confirmación transaccional |
| RF-32–34 | Folios: cuentas maestras, detalle, división y transferencia autorizada | Trazabilidad, saldo y autorización real |
| RF-35–39 | Convenios: vigencia, condiciones/crédito; Reportes: convenios y saldos | Contratos, renovación, alertas y facturación corporativa |
| RF-40–43 | Transporte: programación y seguimiento; Reportes: operativo diario | Conductores, cargos/comisiones, notificaciones y exportación |
| RF-44–48 | Huéspedes: búsqueda local, perfil, consentimiento/anonimización y duplicados | Búsqueda difusa cloud, directorio externo, privacidad y fusión auditada |
| RF-49–57 | F&B: desayunos, menú y consumos; Inventario: mínimos/mermas; Finanzas: factura desglosada | Ocupación diaria, insumos exactos, stock, acceso por tarifa y cargos |
| RF-58–62 | Gimnasio/spa: agenda, acceso/aforo y catálogo; Tarifas: paquetes | Aforo concurrente, citas, terapeutas, promociones y cobros |
| RF-63 | Lavandería: órdenes, prendas, entrega e incidencias | Seguimiento y cargos reales |
| RF-64–70 | Finanzas: pagos, devoluciones, facturas, notas de crédito y caja; Reportes | Autorización, emisión normativa, conciliación y agregaciones |
| RF-71–78 | Configuración: hotel, plantillas, impuestos/pagos, políticas y asistente; Tarifas; Usuarios | Configuración persistida/auditada y permisos efectivos |
| RF-79–82 | Tarifas: simulador de componentes; acciones sensibles: motivo/autorización; Auditoría | Precio final calculado, umbrales y log inmutable |
| RF-83–92 | Mi puesto y Reportes: ocupación, canales, housekeeping, F&B, bienestar, transporte, convenios, finanzas y envíos | Métricas cloud, PDF/Excel y correo programado |
| RF-93–98 | Alertas segmentadas/seguimiento; Configuración: plantillas; Mi puesto y check-in/out: pendientes | Eventos, alertas automáticas y notificaciones por rol |
| RF-99–102 | Reservas: no-show/espera; Habitaciones: bloqueo/estado; Auditoría | Liberación, conversión automática e historial |
| RF-103–107 | Operación: quejas, objetos, llaves y bitácora; Finanzas: caja | Custodia, responsables, trazabilidad y cambios de turno |
| RF-108–114 | Check-in: acompañantes/cunas; Configuración: cargos; Huéspedes y Operación: segmentación, solicitudes y opiniones | Ocupación, cargos, disponibilidad e historial |
| RF-115–118 | Reportes: viajeros/auditoría; Check-out y Folios: revisión de saldo/integridad | Plantillas normativas, exportación, integridad y saldo autoritativo |

Los flujos no crean servicios de ejemplo ni anuncian cobros, reservas o emisiones completados. Configuración de idioma es un borrador: la traducción completa al inglés (RNF-27) sigue pendiente de frontend. La importación local no sustituye la validación del servidor.

RNF-01 se conserva con WPF. RNF-09 se representa mediante permisos explícitos y denegación por defecto; RNF-14 mediante ausencia de acceso para huésped/desconocido. RNF-23–26 y RNF-28 tienen flujos guiados, etiquetas, foco, estados, temas, atajos y ayuda contextual; no es una certificación de accesibilidad. RNF-36 tiene un asistente de revisión en cuatro pasos. Los RNF de autenticación, cloud, rendimiento, disponibilidad, cifrado, retención y facturación requieren implementación y verificación posteriores.
