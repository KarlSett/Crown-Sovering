# Frontend por roles e integración de backend

Aplicación **WPF con .NET Framework 4.7.2**. La entrega incorpora 22 áreas y flujos visuales derivados de `RF del proyecto grupal corregido.docx`. El login y sus estilos compartidos no se modificaron.

**Solo frontend:** datos sintéticos, filtros en memoria, lectura local de CSV/XLSX y borradores revisables. No hay autenticación, Supabase, persistencia, cobros, facturación, exportaciones ni envío de mensajes. Ningún formulario anuncia una operación real completada.

## Roles encontrados e interpretación

El Word **no enumera un catálogo completo de roles ni una matriz de permisos**. Menciona recepción/recepcionista, housekeeping, supervisor y administrador; RNF-09 exige permisos granulares. Los perfiles de servicios se deducen de sus responsabilidades. El administrador con acceso completo viene también de la instrucción del usuario.

Esta es la interpretación de trabajo implementada para la demostración; backend y el responsable funcional deben ratificarla antes de trabajar con datos reales.

| Perfil demo | Áreas visibles | Acciones además de consultar |
| --- | --- | --- |
| `Administrador` | Las 22 áreas | Todas las acciones de presentación |
| `Recepcion` | Inicio, alertas, reservas, habitaciones, check-in/out, huéspedes, folios, operación, transporte, convenios | Preparar operaciones de recepción, cargos y traslados; cancelar/no-show. Habitaciones y convenios solo lectura |
| `Housekeeping` | Inicio, alertas, housekeeping, habitaciones | Preparar limpieza/turndown; sin asignar ni inspeccionar |
| `Supervisor` | Inicio, alertas, housekeeping, habitaciones, mantenimiento | Asignar tareas, revisar inspecciones, preparar estados e incidencias |
| `Mantenimiento` | Inicio, alertas, mantenimiento, habitaciones | Preparar incidencias y seguimiento; habitaciones solo lectura |
| `AlimentosBebidas` | Inicio, alertas, F&B, inventario | Preparar comandas, desayunos, consumos y stock; sin administrar menú/tarifas |
| `Bienestar` | Inicio, alertas, gimnasio/spa | Preparar citas y entradas/salidas; sin administrar catálogo/tarifas |
| `Lavanderia` | Inicio, alertas, lavandería | Preparar órdenes, seguimiento y entrega |
| `Finanzas` | Inicio, alertas, finanzas, folios, reportes | Preparar pagos, facturas, caja y parámetros de reportes; sin autorizar devoluciones, notas de crédito ni transferencias |
| `Transporte` | Inicio, alertas, transporte | Preparar traslados y seguimiento |
| Huésped, desconocido o contexto nulo | Ninguna | Denegación por defecto, conforme a RNF-14 |

Todos los perfiles de personal preparan avisos y seguimiento en Alertas. Los avisos se filtran por áreas visibles. `Supervisor` representa supervisión de pisos, no gerencia general.

## Permisos que consume el panel

[Security/FrontendAccessContext.cs](Security/FrontendAccessContext.cs) recibe permisos explícitos como `Reservas.View` o `Folios.Approve`:

| Acción | Presentación |
| --- | --- |
| `View` | Mostrar área, consultar listados/fichas |
| `Write` | Preparar operaciones habituales |
| `Cancel` | Preparar cancelaciones/no-show |
| `Approve` | Revisar inspecciones, transferencias, devoluciones, notas de crédito y cierre remoto |
| `Manage` | Preparar configuración, asignaciones, usuarios y catálogos |
| `Export` | Preparar parámetros de reportes; no descarga archivos |
| `Privacy` | Preparar solicitudes de consentimiento/anonimización |

Cada acción necesita también `Modulo.View`. Las cadenas distinguen mayúsculas. No hay comodines ni privilegios derivados del nombre del rol: una etiqueta “Administrador” sin permisos no concede acceso. Un usuario sin identificador tampoco obtiene acceso. La colección se copia al construir el contexto y al exponerla.

Claves estables: `Inicio`, `Reservas`, `Habitaciones`, `CheckInOut`, `Huespedes`, `Convenios`, `FNB`, `GymSpa`, `Lavanderia`, `Finanzas`, `Reportes`, `Config`, `Housekeeping`, `Mantenimiento`, `Transporte`, `Tarifas`, `Folios`, `Inventario`, `Alertas`, `Operacion`, `Usuarios`, `Auditoria`.

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

El [mapa de requisitos](../docs/REQUISITOS_FRONTEND.md) relaciona RF-01–118 con pantallas. Un formulario que representa un RF **no significa que esté implementado de extremo a extremo**. Cálculo de tarifas, firma verificable, disponibilidad, notificaciones, factura y persistencia requieren backend.

**RNF-27 pendiente:** la interfaz está en español. “Idioma previsto” es un dato del borrador y no traduce la aplicación al inglés. La búsqueda ignora acentos, pero no implementa búsqueda difusa cloud ni directorio externo. No se afirma cumplimiento normativo, rendimiento cloud, cifrado ni accesibilidad certificada por esta entrega visual.

## Verificar y previsualizar perfiles

Desde la raíz del proyecto:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Preview-Role.ps1 -Role Administrador
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Preview-Role.ps1 -Role Housekeeping -Theme Dark
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Verify-Frontend.ps1
```

Las vistas por rol usan un ejecutable de revisión separado; no añaden un selector de rol al programa. La verificación compila con MSBuild y recorre módulos, flujos, tres temas/tamaños, restricciones y regresión del login. `.design-preview` contiene renders de XAML compilado, no capturas interactivas. Los nuevos C# están registrados en el `.csproj` clásico.
