# Módulos, pantallas y roles

Mapa de las pantallas del panel y de los roles que pueden verlas. Sirve de guía para
conectar el backend.

> **Alcance actual:** la visibilidad por rol es **solo de presentación**. Se aplica en el
> cliente para que el menú refleje los casos de uso, pero **no autoriza nada**. El backend
> debe validar permisos en cada operación cuando exista autenticación real.

## Roles

| Rol | Uso previsto |
| --- | --- |
| Administrador | Usuarios, roles, auditoría, configuración del hotel y habitaciones. |
| Gerente | Tarifas, convenios, dashboard y reportes, autorizaciones (transferencias y anulaciones), fusión de duplicados, anonimización y revisión del cierre de caja. |
| Recepcionista | Reservas, check-in/out, folios, huéspedes, pagos, anulaciones, facturación y cierre de caja antes del cajero. |
| Personal de servicios | Housekeeping, inspección y mantenimiento, alimentos y bebidas, gimnasio, spa, lavandería y traslados. |

En el login se elige el rol en el desplegable **Rol de la sesión**; el botón
**Entrar como administrador** es un atajo que selecciona Administrador.

## Pantallas y tablas del esquema

### Operación

| Pantalla | Tablas del esquema | Roles |
| --- | --- | --- |
| Funcionamiento | `reserva`, `habitacion`, `folio`, `cargo`, `pago` (resumen) | Admin · Gerente · Recepcionista |
| Reservas | `reserva`, `reservahabitacion`, `huesped` | Admin · Gerente · Recepcionista |
| Habitaciones disponibles | `habitacion`, `tipohabitacion`, `reservahabitacion` | Admin · Gerente · Recepcionista |
| Cuentas de ingreso | `cargo`, `pago`, `cierrecaja` | Admin · Gerente |

### Administración

| Pantalla | Tablas del esquema | Roles |
| --- | --- | --- |
| Huéspedes | `huesped`, `consentimiento` | Admin · Gerente · Recepcionista |
| Inventario | `piso`, `tipohabitacion`, `habitacion`, `habitacionestadohistorial`, `bloqueohabitacion` | Admin · Gerente |
| Estancias y llaves | `estancia`, `llave` | Admin · Gerente · Recepcionista |
| Facturación | `folio`, `conceptocargo`, `cargo`, `transferenciafolio`, `factura`, `notacredito` | Admin · Gerente · Recepcionista |
| Pagos y caja | `metodopago`, `pago`, `devolucion`, `anulacion`, `turnocaja`, `movimientocaja`, `cierrecaja` | Admin · Gerente · Recepcionista |
| Reservas avanzadas | `canalreserva`, `gruporeserva`, `reserva`, `reservahuesped`, `reservahabitacion`, `listaespera`, `solicitudespecial`, `recursoadicional`, `reservarecurso` | Admin · Gerente · Recepcionista |
| Notificaciones | `notificacion`, `notificacionrol`, `notificacionrecepcion`, `reporteprogramado`, `reporteejecucion` | Admin · Gerente |

### Producto

| Pantalla | Tablas del esquema | Roles |
| --- | --- | --- |
| Tarifas | `tarifa`, `temporada`, `tarifaconfiguracion`, `paquete`, `promocion`, `reservapaquete`, `reservapromocion` | Admin · Gerente |
| Convenios | `convenio`, `convenioreserva`, `conveniohistorial` | Admin · Gerente |

### Servicios

| Pantalla | Tablas del esquema | Roles |
| --- | --- | --- |
| Limpieza y mantenimiento | `checklist`, `checklistitem`, `tarealimpieza`, `tareachecklist`, `inspeccion`, `incidencia`, `ordenmantenimiento`, `objetoolvidado` | Admin · Servicios |
| Consumos y servicios | `producto`, `movimientoinventario`, `consumo`, `servicio`, `reservaservicio`, `citaservicio`, `traslado` | Admin · Recepcionista · Servicios |

### Configuración

| Pantalla | Tablas del esquema | Roles |
| --- | --- | --- |
| Configuración del hotel | `hotel`, `configuracion`, `politica`, `impuesto`, `modulo`, `hotelmodulo`, `configuracionaudit` | Admin |
| Seguridad y personal | `empleado`, `usuario`, `rol`, `usuariorol` | Admin |

## Diseño de pantallas (maquetas)

Cada pantalla usa una disposición distinta según su tarea, **sin estilos**: solo controles
por defecto, paneles de disposición, etiquetas en español y datos de ejemplo.

| Pantalla | Patrón de disposición |
| --- | --- |
| Huéspedes | Lista a la izquierda; ficha y consentimientos a la derecha. |
| Inventario | Pisos a la izquierda; habitaciones como mosaico de botones; detalle y pestañas debajo. |
| Reservas avanzadas | Tablero semanal de siete columnas; detalle y catálogos en pestañas. |
| Estancias y llaves | Tablero de tres columnas (llegadas, en casa, salidas); detalle y llaves debajo. |
| Facturación | Folios a la izquierda; resumen y pestañas de cargos/facturas a la derecha. |
| Pagos y caja | Consola de turno arriba; pagos a la izquierda; movimientos y cierre a la derecha. |
| Notificaciones | Bandeja con vista de lectura y destinatarios; reportes en pestañas. |
| Configuración del hotel | Pestañas verticales (franja lateral) para cada sección. |
| Seguridad y personal | Secciones plegables (Expander) para usuarios, empleados, roles y asignación. |
| Tarifas | Matriz tipo de habitación × temporada; catálogos en pestañas. |
| Convenios | Galería de tarjetas; reservas e historial en pestañas. |
| Limpieza y mantenimiento | Tablero kanban (pendientes, en curso, completadas); catálogos en pestañas. |
| Consumos y servicios | Punto de venta: catálogo a la izquierda, consumo y totales a la derecha. |

Las altas principales (huésped, habitación, reserva) usan **ventanas emergentes**; los
catálogos usan un formulario compacto en la parte superior, nunca un panel de creación
lateral.

## Cómo se aplica hoy

- `Frontend/Sesion.cs` guarda el rol elegido (`Rol`).
- `Frontend/Views/DashboardView.xaml.cs` contiene el diccionario `Permisos`
  (pantalla → roles) y oculta las entradas y los grupos vacíos en la barra lateral.
- Al iniciar sesión con Personal de servicios se abre directamente *Limpieza y
  mantenimiento*, porque esa sesión no tiene acceso a Funcionamiento.
- Cada pantalla se carga al entrar en su entrada del menú; no hay navegación profunda.

## Pendiente (backend)

1. Autenticación real y rol efectivo del usuario.
2. Autorización por operación (no solo por pantalla): p. ej. anulaciones requieren
   Gerente/Administrador y deben auditarse.
3. Unificar la matriz de permisos en el servidor; el cliente solo debe dibujar lo que el
   servidor autorice.
4. Sesiones remotas y cierre forzado (caso de uso de Administrador).
5. Fusión de duplicados y anonimización (caso de uso de Gerente).
