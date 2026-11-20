# Crown & Sovereign — sistema visual de escritorio

## Dirección y restricciones

Una herramienta de trabajo para recepción y administración hotelera: lectura rápida,
superficies tranquilas y estados inequívocos. La identidad conserva el emblema original,
el nombre Crown & Sovereign y los colores originales del emblema. El tema de marca usa
la escala Forest Green proporcionada por el usuario. La fotografía y
Georgia mantienen la voz de hospitalidad en el acceso; el panel usa Segoe UI para títulos,
controles y datos.

El menú usa iconos vectoriales por módulo, selección sólida y una ficha de sesión con avatar.
El modo Claro conserva su paleta neutral original, con acentos azules; Oscuro usa grafito.
De marca es una apariencia clara independiente, con fondos Forest Green 50 y selección 100/200.
Se sustituyen los puntos de navegación, las cápsulas grandes y las pestañas subrayadas
por iconografía consistente, paneles con encabezado y pestañas segmentadas.

La aplicación continúa en WPF, .NET Framework 4.7.2. Conserva la composición existente:
barra lateral de 236 DIPs, encabezado, filtros, acciones y paneles en sus celdas originales.
Las barras que exceden el ancho disponible permiten desplazamiento horizontal. Las tablas
mantienen desplazamiento y anchos mínimos para no cortar números o esconder columnas.
Los formularios largos desplazan sus campos y mantienen las acciones al pie.

## Tres apariencias

El botón de apariencia, en su ubicación original, recorre Claro → Oscuro → De marca.
La elección se comparte entre el acceso, el panel y los diálogos durante la ejecución;
no se guarda al cerrar la aplicación.

| Token | Claro | Oscuro | De marca |
| --- | --- | --- | --- |
| `UI.Page` | `#EDF1F6` | `#10151D` | `#EFFEF3` |
| `UI.Surface` | `#FFFFFF` | `#1B2330` | `#FFFFFF` |
| `UI.Text` | `#172E44` | `#F1F4F8` | `#013213` |
| `UI.Muted` | `#596D83` | `#AFBDCD` | `#0F6C2F` |
| `UI.Accent` | `#183C55` | `#DCC18B` | `#0F6C2F` |
| `UI.OnAccent` | `#FFFFFF` | `#172431` | `#EFFEF3` |

El tema De marca, en su apariencia clara, usa el extremo izquierdo de la escala: 50 (`#EFFEF3`), 100 (`#D9FFE5`)
y 200 (`#B6FCCC`). Los textos y las acciones usan verdes oscuros para conservar el contraste
sobre estas superficies claras.

La escala completa 50–950 está registrada como `ForestGreen.*`. El tema de marca usa
superficies claras y controles principales 800 con etiquetas 50 para conservar un contraste
superior a 4.5:1. El modo Claro original y el modo Oscuro mantienen sus paletas anteriores.
El emblema oficial conserva sus colores.

`HotelPalette.xaml` define los valores iniciales y conserva los recursos históricos de
marca. `ThemePresentation.cs` cambia los recursos `UI.*` de la aplicación en memoria.
Los controles usan `DynamicResource` para actualizarse sin recrear las vistas. Los colores
de éxito, advertencia y error tienen texto y superficie propios en cada apariencia.

## Componentes e interacción

- `ModernControls.xaml`: campos, selectores, calendario, botones, pestañas, grupos,
  listas, barras de desplazamiento y tarjetas de habitación con plantillas WPF nativas.
- `LiquidGlassStyles.xaml`: emblema, acceso, campos de credenciales, controles de
  apariencia y movimiento. Conserva la escena fotográfica y el respaldo de vídeo local.
- `OperationsStyles.xaml`: agenda semanal, tarjetas de reserva, resúmenes de estancia y pestañas internas.
- `DashboardStyles.xaml`: navegación, indicadores, tablas, tarjetas, panel emergente
  y disponibilidad. Las filas miden 46 DIPs; los controles, entre 34 y 52 DIPs según su uso.

Los controles usan radios de 5–8 DIPs; los paneles, 10–12. Los indicadores tienen títulos
en estilo oración, iconos por concepto y una escala de valores de 30 DIPs.
Los estados de foco, selección, pulsación y deshabilitado son visibles. Los controles
conservan la interacción de teclado de WPF. El hover de los botones usa 120 ms y el
detalle de habitación, 160 ms; ambos respetan `SystemParameters.ClientAreaAnimation`.

Las plantillas del calendario conservan sus partes y la selección del control nativo,
siguiendo la [referencia de plantillas de Calendar de Microsoft](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/calendar-styles-and-templates).

## Habitaciones

El inventario mantiene el selector de pisos a la izquierda, las habitaciones arriba,
el detalle debajo y el historial al pie. Las tarjetas muestran número, tipo, estado
mediante indicador y descripción emergente; la selección tiene borde y marca de verificación.
Cambiar de piso actualiza la lista y limpia el detalle para evitar mostrar otra habitación.
Seleccionar una tarjeta actualiza el número, tipo, estado y tarifa en el panel existente.
Estos cambios son de presentación y lectura de los datos de demostración disponibles.

La disponibilidad conserva sus filtros y tabla. Cambiar fechas válidas, tipo o calidad
actualiza los resultados; el botón Buscar disponibilidad sigue en su lugar. La selección
de una fila amplía sus datos, y las etiquetas de disponibilidad conservan texto además
del color. En la reserva, cada opción distingue número, tipo, calidad, capacidad y precio;
el cálculo de tarifa y total existente sigue funcionando.

## Reservas, huéspedes y estancias

La agenda de reservas avanzadas conserva las siete columnas semanales y el detalle inferior.
Muestra las fechas de la semana actual, marca Hoy y cuenta reservas por día. Cada tarjeta
muestra código, nombre abreviado, habitación, noches y estado; la descripción emergente
conserva el nombre completo. Los filtros de canal y estado actualizan la agenda en memoria.
Seleccionar una tarjeta actualiza el huésped, el canal, las fechas y las tablas de detalle.
El panel inferior adapta su altura y permite desplazar los campos cuando falta espacio.

Huéspedes usa los registros de demostración existentes, avatares con iniciales, búsqueda
por nombre/documento/correo y campos vinculados a la selección en modo de solo lectura del
modelo. Estancias muestra llegadas, huéspedes en casa y salidas a partir de esas reservas;
seleccionar una fila actualiza el detalle. Las acciones de edición, check-in/check-out,
llaves y guardado de estos módulos conservan el alcance simulado del esqueleto.

## Ventana y fondo

Ventana inicial de 1200 × 760, mínimo de 920 × 660. Se conservan `WindowChrome`, radio
de 28 DIPs, región de arrastre de 44 DIPs y los controles de minimizar, maximizar y salir.
El acceso conserva su división 52/48 y el fondo local `hospitality-lobby.png`. Su movimiento
suave y el botón de pausa siguen disponibles; no requiere descargas ni servicios externos.

## Alcance y verificación

Rediseño de presentación sobre la base existente: no cambia autenticación, persistencia,
reglas de reservas ni servicios de backend. Los módulos de esqueleto mantienen sus acciones
sin implementar; el rediseño no los convierte en operaciones reales.

Ejecutar desde la raíz:

```powershell
./scripts/Preview-Frontend.ps1
```

El script compila con MSBuild de Visual Studio, renderiza el acceso, las 17 secciones,
el formulario de reserva y tres diálogos en las tres apariencias. Comprueba las secciones
en 1172 × 702 y 892 × 602 DIPs de contenido, correspondientes a la ventana normal y mínima.
Incluye vistas amplias de 1536 × 824 para agenda, inventario y panel operativo.
También verifica selección de tarjetas de reserva, filtros de agenda y búsqueda de huéspedes,
contraste de texto, cambio de tema entre vistas, filtros de disponibilidad,
selección por piso y uso del calendario en una raíz WPF nativa oculta. No guarda reservas.

Las imágenes se generan en `.design-preview/modern/`. Son exportaciones del XAML compilado,
no capturas de una sesión interactiva. Las vistas de referencia se incluyen en `docs/images/`.
