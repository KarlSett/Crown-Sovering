# Integración con backend

## Punto de partida

La entrega actual es exclusivamente de frontend. Ingresar y el acceso simulado abren un panel de demostración sin comprobar credenciales. No hay sesión autenticada, conexión a base de datos ni credenciales guardadas. Backend/Data, Models, Repositories y Services siguen reservados para la integración del equipo.

La página del login es:

- Vista: `Frontend/Views/LoginView.xaml`.
- Código asociado: `Frontend/Views/LoginView.xaml.cs`.
- Clase: `Crown__.Frontend.Views.LoginView`.
- Contenedor: el Frame `LoginFrame` de `MainWindow.xaml`, que carga esta página.

Los controles pertenecen al ámbito de nombres de **LoginView**, no al de MainWindow. Integrar los eventos o bindings en la página; no buscarlos directamente con MainWindow.FindName.

## Controles disponibles

| Nombre | Tipo WPF | Uso previsto |
| --- | --- | --- |
| TxtUsuario | TextBox | Usuario; propiedad Text |
| TxtContrasena | PasswordBox | Contraseña; propiedad Password |
| ChkRecordarme | CheckBox | Preferencia visual; IsChecked; no persiste datos |
| BtnIngresar | Button | OnIngresar abre la demostración; reemplazar este paso por autenticación real |
| BtnLoginSimulado | Button | Comparte OnIngresar para explorar la demostración |
| BtnSoporteTecnico | Button | Soporte pendiente; sin evento ni comando |

ThemeButton, EyeToggle y los eventos del fondo pertenecen a presentación. El tema se transmite entre LoginView y DashboardView mediante el argumento Light, Dark o Brand.

## Panel hotelero

`DashboardView.xaml` conserva el shell, los estilos, ModuleContent y el panel Overlay. `DashboardView.xaml.cs` genera el contenido de cada sección. `ModuleCatalog.cs` contiene exclusivamente fixtures y metadatos visuales; no usar esas clases como entidades persistentes.

| Clave estable | Contenido | Punto de integración pendiente |
| --- | --- | --- |
| Inicio | Indicadores, movimientos y alertas | Resumen operativo por turno |
| Reservas | Listado, filtro y borrador | Disponibilidad, tarifas y creación de reservas |
| Habitaciones | Fichas por piso y estado | Inventario, asignación, limpieza y mantenimiento |
| CheckInOut | Llegadas, salidas y pendientes | Confirmación transaccional del movimiento |
| Huespedes | Directorio y preferencias | Fichas de huéspedes e historial |
| Convenios | Empresas, tarifas y vigencias | Contratos y reglas comerciales |
| FNB | Comandas, minibar y consumos | Pedidos, cargos y stock |
| GymSpa | Agenda y aforo | Disponibilidad, cupos y citas |
| Lavanderia | Órdenes, prendas y entrega | Seguimiento de órdenes y cargos |
| Finanzas | Movimientos y saldos | Caja, cobros, conciliación y facturación |
| Reportes | Gráfica y catálogo de reportes | Agregaciones y exportación PDF/Excel |
| Config | Apariencia y preferencias | Configuración, usuarios y permisos |

Los botones de navegación exponen AutomationId `Nav` seguido de la clave del módulo. ShowModule selecciona la vista; RefreshResults combina búsqueda y filtros locales; ShowDetail abre la ficha; OpenForm crea los campos; OnReviewDraft comprueba el formato y muestra un resumen **sin guardar ni enviar**. Para conectar operaciones, sustituir los fixtures por modelos de vista tipados y enlazar los controles a comandos/servicios. Mantener las claves para conservar la navegación y las comprobaciones.

FieldDefinition describe etiqueta, tipo, hint y opciones. Los campos de fecha usan dd/MM/aaaa; las horas, HH:mm. Los importes aceptan punto o coma decimal, hasta dos decimales, sin separadores de miles. La salida de una reserva debe ser posterior a la llegada; el intervalo de reportes permite el mismo día. Backend debe volver a validar todo, incluyendo capacidad, disponibilidad, conflictos de horarios, permisos, moneda e importes reales.

El flujo actual es editor → errores o resumen → volver a editar/cerrar. No se anuncian reservas, cobros o registros completados. Al integrar servicios, coordinar estados de carga, error recuperable y confirmación real; preservar los valores del formulario al fallar. Buscar, filtros, tema y preferencias solo afectan la vista en memoria; navegar a otro módulo restablece los filtros de ese módulo.

Los colores `Ui.*` son recursos dinámicos de la página. No fijar Stroke/Foreground a negro ni usar pinceles Light.* directamente dentro del panel; usar los recursos semánticos para que campos, iconos y ventanas emergentes respondan al cambio de tema.

## Reparto de responsabilidades sugerido

| Carpeta | Responsabilidad pendiente |
| --- | --- |
| Backend/Models | Entidades y resultados de operaciones |
| Backend/Data | Conexión y configuración de datos |
| Backend/Repositories | Acceso a datos |
| Backend/Services | Autenticación y reglas de negocio |

La vista debería consumir un servicio; evitar consultas SQL o configuración de conexión dentro del XAML y su código asociado.

## Decisiones que debe acordar el equipo

- Proveedor y esquema de base de datos.
- Contrato del servicio de autenticación y representación de sus resultados.
- Pantalla de destino y permisos por rol.
- Comportamiento de Recordarme, soporte y acceso de demostración.
- Presentación de carga y errores de ingreso.

Estas decisiones no están implementadas ni se asumen en el frontend entregado. Las operaciones de datos deberán ejecutarse sin bloquear el hilo de interfaz; coordinar con frontend los estados de espera y error. No guardar contraseñas en texto plano ni incorporar secretos al repositorio.

## Añadir archivos al proyecto

El `Crown &.csproj` usa formato clásico de .NET Framework: añadir una clase desde Visual Studio actualiza su entrada Compile. Si se crea desde otro editor, añadir explícitamente dicha entrada. Mantener la versión objetivo 4.7.2 hasta acordar una migración.

## Validación inicial

Antes de integrar, abrir la solución y comprobar ambos temas, el formulario, los controles de ventana y el tamaño mínimo. El fondo usa una imagen local y no necesita Internet. Para vídeo opcional, seguir `Frontend/Assets/Video/README.md`.

Ejecutar `scripts/Verify-Frontend.ps1` desde PowerShell. Compila con MSBuild y verifica módulos, temas, tamaños, filtros, validación de formularios, hints del login y sincronización de contraseña. Guarda renders del XAML compilado en `.design-preview`; no sustituye la revisión interactiva ni las pruebas de integración con backend.
