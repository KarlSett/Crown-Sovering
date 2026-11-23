# Integración con backend

## Punto de partida

La interfaz funciona con datos de demostración en memoria (`Backend/Data/DatosHotel`) y
no hay autenticación real: `BtnIngresar` y `BtnLoginSimulado` navegan al panel. `Entidades.cs`
es un modelo de demo, no un mapeo completo del esquema PostgreSQL; las diferencias y
decisiones pendientes están en `docs/PENDIENTES.md`.

Página de acceso: `Frontend/Views/LoginView.xaml` (clase
`Crown__.Frontend.Views.LoginView`), cargada en el Frame `LoginFrame` de
`MainWindow.xaml`. Los controles del login pertenecen al ámbito de esa página.

Panel: `Frontend/Views/DashboardView.xaml` (clase `Crown__.Frontend.Views.DashboardView`).
Contiene solo composición; cada sección vive en `Frontend/Views/Sections` y tiene su propia
lógica de presentación. Las pantallas de los trece módulos están en
`Frontend/Views/Modules` y su visibilidad por rol se documenta en
`docs/MODULOS_Y_ROLES.md`.

La selección de rol del login (`CmbRol`) guarda `Frontend/Sesion.Rol`; el panel oculta las
entradas no permitidas. **Eso es solo presentación**: el backend debe autenticar y
autorizar cada operación.

## Secciones y servicios

| Sección | Clase | Servicio |
| --- | --- | --- |
| Funcionamiento | `Sections/FuncionamientoView` | `ServicioHotel.ObtenerResumenOperativo()` |
| Reservas | `Sections/ReservasView` | `ListarReservas`, `CrearReserva`, `CheckIn`, `CheckOut`, `Cancelar` |
| Habitaciones | `Sections/HabitacionesView` | `ListarDisponibilidad`, `HabitacionesLibres` |
| Cuentas de ingreso | `Sections/IngresosView` | `ObtenerIngresos(PeriodoReporte, fecha)` |

`ReservasView` expone el evento `CambiosRealizados`; `DashboardView` lo usa para refrescar
las demás secciones. Cualquier implementación real debe conservar ese contrato o
reemplazarlo por notificaciones del servicio.

## Contrato a implementar

`ServicioHotel` devuelve modelos de `Backend/Models` (`ResumenOperativo`,
`ResumenIngresos`, `ReservaVista`, `HabitacionVista`), no entidades de EF ni `DataTable`.
Para conectar PostgreSQL:

1. Crear en `Backend/Repositories` un repositorio por agregado (reservas, habitaciones,
   folios, transacciones, cierres) con consultas parametrizadas.
2. Inyectar la cadena de conexión desde `App.config`; no incrustar secretos.
3. Mantener las firmas públicas de `ServicioHotel` para que las cuatro secciones sigan
   compilando sin cambios.
4. Ejecutar las consultas fuera del hilo de interfaz y exponer estados de carga/error.

## Controles del login

| Nombre | Tipo | Uso |
| --- | --- | --- |
| TxtUsuario | TextBox | Usuario |
| TxtContrasena | PasswordBox | Contraseña |
| ChkRecordarme | CheckBox | Preferencia visual; no persiste |
| BtnIngresar | Button | Abre el panel (sin autenticación) |
| BtnLoginSimulado | Button | Igual que BtnIngresar |
| BtnSoporteTecnico | Button | Pendiente |

## Decisiones pendientes

- Contrato real de autenticación y permisos por rol.
- Destino de las operaciones cuando falle la conexión.
- Cifrado y almacenamiento de la cadena de conexión.
- Migración del almacén de demostración a datos reales sin duplicar reglas.

## Añadir archivos al proyecto

El `Crown &.csproj` usa formato clásico de .NET Framework: registrar explícitamente los
archivos nuevos (`Compile`, `Page`). Mantener la versión objetivo 4.7.2 hasta acordar una
migración.

## Estado de implementación

Los flujos principales de operación, reservas, disponibilidad e ingresos funcionan con
datos de demo. Huéspedes, consentimientos e inventario básico ya tienen acciones en memoria.
Las demás pantallas de módulo siguen siendo maquetas y sus operaciones están pendientes.

## Validación inicial

Abrir la solución y comprobar ambos temas, el login, el panel y el tamaño mínimo. El fondo
usa una imagen local; el vídeo es opcional (`Frontend/Assets/Video/README.md`).
