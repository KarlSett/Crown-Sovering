# Frontend por roles e integración de backend

Aplicación **WPF con .NET Framework 4.7.2**. La entrega incorpora 23 áreas unificadas y flujos visuales derivados de `RF del proyecto grupal corregido.docx`, incluyendo selector de rol simulado en Login, menú de `Preferencias` con `ComboBox` de temas, calendario de `Eventos y descuentos` y limitación de servicios del hotel.

**Solo frontend:** datos sintéticos, filtros en memoria, lectura local de CSV/XLSX y borradores revisables. No hay autenticación, Supabase, persistencia, cobros, facturación, exportaciones ni envío de mensajes. Ningún formulario anuncia una operación real completada.

## Roles de demostración

El selector de acceso simulado y el sistema utilizan únicamente **3 roles**:

| Rol | Áreas visibles | Acciones principales |
| --- | --- | --- |
| `Administrador` | Inicio, alertas, preferencias, **eventos y descuentos**, tarifas, convenios, finanzas, reportes, comidas y bebidas, spa y gimnasio, lavandería, transporte, inventario, usuarios, auditoría y ajustes (**sin Reservas ni mostrador**) | Gestión administrativa completa, calendario de eventos/descuentos, **limitación de servicios** y configuración del sistema |
| `Gerente` | Inicio, alertas, preferencias, **eventos y descuentos**, tarifas, convenios, finanzas, reportes, comidas y bebidas, spa y gimnasio, lavandería, transporte e inventario (**sin Reservas ni mostrador, ni usuarios/sistema**) | Gestión comercial y administrativa, calendario de eventos/descuentos y **limitación de servicios** |
| `Recepcionista` | Inicio, alertas, preferencias, **reservas**, habitaciones, **check-in y huéspedes (unificado)**, folios, operación del turno, comidas y bebidas, spa y gimnasio, housekeeping, mantenimiento, lavandería y transporte | Atención de reservas, llegadas/salidas, directorio de huéspedes y servicios al huésped (sin eventos/descuentos ni limitación de servicios) |
| Huésped, desconocido o contexto nulo | Ninguna | Denegación por defecto, conforme a RNF-14 |

Todos los perfiles de personal ven `Preferencias` y preparan avisos en `Alertas`. `Eventos` y la acción `Limit` son exclusivos de `Administrador` y `Gerente`; `Reservas` y `CheckInOut` son propios de `Recepcionista`.

## Permisos que consume el panel

[Security/FrontendAccessContext.cs](Security/FrontendAccessContext.cs) recibe permisos explícitos como `Reservas.View` o `GymSpa.Limit`:

| Acción | Presentación |
| --- | --- |
| `View` | Mostrar área, consultar listados/fichas |
| `Write` | Preparar operaciones habituales |
| `Cancel` | Preparar cancelaciones/no-show |
| `Approve` | Revisar inspecciones, transferencias, devoluciones, notas de crédito y cierre remoto |
| `Manage` | Preparar configuración, asignaciones, usuarios y catálogos |
| `Limit` | Limitar horarios, cupo/aforo o estado operativo de servicios del hotel (`Administrador` y `Gerente`) |
| `Export` | Preparar parámetros de reportes; no descarga archivos |
| `Privacy` | Preparar solicitudes de consentimiento/anonimización |

Cada acción necesita también `Modulo.View`. Las cadenas distinguen mayúsculas. No hay comodines ni privilegios derivados del nombre del rol: una etiqueta “Administrador” sin permisos no concede acceso. Un usuario sin identificador tampoco obtiene acceso. La colección se copia al construir el contexto y al exponerla.

Claves estables: `Inicio`, `Alertas`, `Preferencias`, `Reservas`, `Habitaciones`, `CheckInOut` (integra movimientos y directorio de huéspedes), `Convenios`, `Eventos`, `FNB`, `GymSpa`, `Lavanderia`, `Finanzas`, `Reportes`, `Config`, `Housekeeping`, `Mantenimiento`, `Transporte`, `Tarifas`, `Folios`, `Inventario`, `Operacion`, `Usuarios`, `Auditoria`.

## Atajos de teclado de interfaz

- `Alt + T`: alterna el tema visual (`Claro` -> `Oscuro` -> `Marca`) tanto en `LoginView` como en `DashboardView`, sincronizado con el `ComboBox` de temas.
- `Ctrl + Shift + ↑` / `Ctrl + Shift + ↓`: cambia al menú anterior o siguiente visible para el rol activo en `DashboardView`.
- `F1` / `Esc`: abre ayuda contextual y cierra paneles modales de borrador.

Menú, flujos y acciones ocultan lo no permitido. `ShowModule`, `SelectWorkflow`, `ShowDetail`, `OpenForm` y `OnReviewDraft` comprueban el acceso nuevamente. Mi puesto solo muestra accesos y cifras autorizados. La consulta de la matriz en Usuarios **no cambia el perfil de la sesión**.

## Contrato propuesto de sesión

El login conservado usa `new DashboardView(theme)`: abre una **demostración de Recepción**, sin validar credenciales. Los constructores sin contexto se conservan únicamente para esa demostración. Backend debe reemplazar ese ingreso cuando implemente la autenticación.

Después de validar la sesión, entregar permisos efectivos del servicio:

```csharp
using Crown__.Frontend.Security;
using Crown__.Frontend.Views;

var context = new FrontendAccessContext(
    session.UserId, session.DisplayName,
    session.RoleLabel, session.Permissions);
NavigationService.Navigate(new DashboardView(theme, context));
```

`session` es un contrato **propuesto**, no una clase existente. Campos mínimos: identificador, nombre, etiqueta del rol y colección de permisos. Token, expiración, revocación y conexión pertenecen al servicio. Al cambiar permisos o revocar la sesión, desechar el panel y reconstruirlo con el contexto actualizado, cerrando formularios anteriores.

`CreateDemonstration(...)` contiene fixtures y **no debe obtener permisos de producción**. El cliente es modificable: ocultar botones y comprobar permisos en C# no protege la base de datos. Cada consulta/operación debe comprobar usuario, hotel, permiso, ámbito de registros y autorización en servicios confiables y políticas de Supabase. No incorporar claves privilegiadas ni secretos al ejecutable.

## Trabajo pendiente de backend

1. **Autenticación y sesiones:** personal autorizado, contraseñas seguras, bloqueo de intentos, expiración por inactividad, revocación y permisos efectivos (RNF-07–14). Aplicar límites por hotel, responsable, equipo, turno y datos sensibles, además del módulo/acción.
2. **Datos cloud:** consultas asíncronas y modelos tipados sustituyendo fixtures. Disponibilidad, bloqueos, calendario y aforo necesitan sincronización y validación autoritativa; la muestra no trabaja en tiempo real.
3. **Operaciones:** tarifas, impuestos, ocupación, cargos, folios, caja y facturación. Transacciones e idempotencia en operaciones críticas; comprobar de nuevo permisos y autorizaciones al confirmar. Los borradores solo validan formato/presencia.
4. **Auditoría:** log inmutable con usuario autenticado, fecha del servidor, entidad, valores anteriores/nuevos, motivo y autorización. El nombre o referencia escrito en un borrador no prueba autorización.
5. **Integraciones:** facturación local, PDF/Excel, impresión, correo, directorio externo, lectura de documentos y notificaciones por rol. No hay endpoints implementados en esta entrega.
6. **Estados al integrar:** carga, desconexión/reintento, conflicto de disponibilidad, permiso revocado, sesión expirada y confirmación real. Conservar valores al fallar; no anunciar éxito sin respuesta del servicio.

## Archivos y puntos de integración

| Archivo | Estado actual / integración |
| --- | --- |
| `Views/ModuleCatalog.cs` | Metadatos y ejemplos originales; sustituir por modelos tipados y consultas paginadas |
| `Views/OperationsCatalog.cs` | Diez áreas nuevas, campos, flujos y referencias RF; conservar claves al conectar operaciones |
| `Views/DashboardView.xaml` | Shell, navegación, temas y panel de revisión |
| `Views/DashboardView.xaml.cs` | Filtros, fichas, validación y resumen; conectar comandos, evitar SQL en código visual |
| `Views/DashboardWorkspaces.cs` | Resumen por rol, flujos, calendario, ayuda, wizard y consulta de permisos |
| `Views/ReservationImportPreview.cs` | Lectura local y errores por fila; revalidar en servidor antes de importar |
| `Security/FrontendAccessContext.cs` | Contexto inmutable de presentación; construir desde la sesión validada |

La navegación conserva `AutomationId = Nav` + clave y el selector de flujos usa `WorkspacePicker`. Los pasos guiados no ejecutan movimientos. Los formularios comprueban obligatorios, correo, teléfono, fechas/rangos, horas, cantidades e importes; volver desde el resumen conserva valores.

Importación: CSV UTF-8 con coma/punto y coma, campos entre comillas y `.xlsx` sin fórmulas. Primera hoja con encabezados `huesped,llegada,salida,tipo,tarifa,canal`; fechas **como texto** `dd/MM/aaaa`, sin seriales Excel ni `.xls`. Límites: 2 MB, 2.000 filas, 200 columnas XLSX y 8 MB por entrada XML descomprimida. No modifica ni sube archivos. Muestra hasta 30 errores y ocho filas válidas.

## Cobertura y límites de esta entrega

El [mapa de requisitos](../docs/REQUISITOS_FRONTEND.md) relaciona `RF-01`–`RF-66` y `RNF-01`–`RNF-33` con las pantallas y flujos del frontend. Un formulario que representa un RF **no significa que esté implementado de extremo a extremo**. Cálculo de tarifas en servidor, firma verificable, disponibilidad concurrente, notificaciones, factura electrónica real y persistencia requieren backend.

**Alcance visual vs. integración real:** la interfaz incluye selector de plantillas ES/EN, estado de conexión cloud simulado (`RNF-02`), tolerancia tipográfica local y vista de directorio externo de muestra (`RF-27`), pero la traducción integral de interfaz, consultas difusas en Supabase y conectores HTTP reales corresponden a la integración posterior.

## Verificar y previsualizar perfiles

Desde la raíz del proyecto:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Preview-Role.ps1 -Role Administrador
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Preview-Role.ps1 -Role Gerente -Theme Brand
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Verify-Frontend.ps1
```

Las vistas por rol usan un ejecutable de revisión separado; no añaden un selector de rol al programa. La verificación compila con MSBuild y recorre módulos, flujos, tres temas/tamaños, restricciones y regresión del login. `.design-preview` contiene renders de XAML compilado, no capturas interactivas. Los nuevos C# están registrados en el `.csproj` clásico.
