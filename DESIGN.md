---
name: Crown & Sovereign
description: Sistema visual nativo WPF para el acceso de hospitalidad y el puesto operativo del hotel.
colors:
  petroleum: "#17384F"
  petroleum-selected: "#31566D"
  navy: "#102B3F"
  gold: "#D9BB79"
  ivory: "#F6F3ED"
  login-paper: "#FAF9F6"
  ink: "#192F40"
  secondary: "#5D6872"
  white: "#FFFFFF"
  login-border: "#B8BFC5"
  focus: "#8A682C"
  page: "#F3F6F8"
  border: "#CBD5DC"
  soft: "#EDF0F2"
  nav-muted: "#C1D0DB"
  good: "#276447"
  warning: "#805C1C"
  danger: "#AE3535"
  login-dark-page: "#15222D"
  dark-page: "#14232F"
  dark-field: "#1C2C39"
  dark-secondary: "#B6C1CA"
  dark-border: "#506674"
  dark-nav: "#10202C"
  dark-soft: "#2A3D4C"
  dark-good: "#9AD7B3"
  dark-warning: "#E5C780"
  dark-danger: "#FFAAAA"
  brand-primary: "#004D40"
  brand-page: "#EFF5F2"
  brand-ink: "#0F172A"
  brand-secondary: "#475569"
  brand-nav: "#203C39"
  brand-selected: "#365E58"
  brand-soft: "#E7F1EE"
typography:
  panel-title:
    fontFamily: Segoe UI
    fontSize: 27px
    fontWeight: 600
  dialog-title:
    fontFamily: Segoe UI
    fontSize: 22px
    fontWeight: 600
  section-title:
    fontFamily: Segoe UI
    fontSize: 19px
    fontWeight: 600
  subheading:
    fontFamily: Segoe UI
    fontSize: 17px
    fontWeight: 600
  metric:
    fontFamily: Segoe UI
    fontSize: 25px
    fontWeight: 600
  body:
    fontFamily: Segoe UI
    fontSize: 13px
    fontWeight: 400
  caption:
    fontFamily: Segoe UI
    fontSize: 12px
    fontWeight: 400
  metadata:
    fontFamily: Segoe UI
    fontSize: 11px
    fontWeight: 400
  brand:
    fontFamily: Georgia
    fontSize: 16px
    fontWeight: 700
rounded:
  control: 8px
  panel: 12px
  shared-field: 12px
  login-photo: 22px
  window: 28px
  scrollbar: 4px
spacing:
  compact: 4px
  small: 8px
  field-gap: 10px
  medium: 12px
  content: 16px
  panel: 20px
  shell-top: 24px
  shell-left: 26px
components:
  button-primary:
    backgroundColor: "{colors.petroleum}"
    textColor: "{colors.white}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: 9px 14px
  button-secondary:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: 9px 14px
  navigation-selected:
    backgroundColor: "{colors.petroleum-selected}"
    textColor: "{colors.white}"
    typography: "{typography.caption}"
    rounded: "{rounded.control}"
    padding: 6px 12px
  panel:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink}"
    rounded: "{rounded.panel}"
    padding: 20px
  input:
    backgroundColor: "{colors.white}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.shared-field}"
    height: 40px
    padding: 0px 16px
  calendar-stay:
    backgroundColor: "{colors.soft}"
    textColor: "{colors.ink}"
    typography: "{typography.metadata}"
    rounded: "{rounded.control}"
    padding: 6px
---

# Design System: Crown & Sovereign

## Overview

**Creative North Star: "Mesa de operaciones del hotel"**

El panel centra el trabajo en las tareas del puesto: identificar el perfil, leer pendientes, entrar en un área, elegir un flujo y revisar un borrador. La identidad se expresa mediante el emblema existente, la navegación petróleo y una jerarquía tipográfica clara. Los estados se leen por nombre y color; los datos de muestra y las revisiones conservan su carácter ilustrativo.

El proyecto es una aplicación WPF con .NET Framework 4.7.2. Este documento fusiona el panel final con las decisiones del acceso existente. Los tokens son normativos para sus roles: las unidades px representan un DIP WPF a 96 DPI para intercambio documental. Las muestras HTML del sidecar sirven solo al visor de documentación; no definen una implementación web.

**Key Characteristics:**

- Navegación lateral estable, agrupada y limitada a las áreas del perfil.
- Superficies sobrias, líneas discretas y controles de Windows accesibles con teclado.
- Segoe UI en el trabajo operativo; Georgia conserva la voz de hospitalidad del acceso y la marca.
- Formularios con etiquetas permanentes, errores locales y revisión de borrador.
- Calendario de habitaciones con barras continuas por estancia.

Las secciones siguientes conservan el contrato del login. La autorización efectiva, la disponibilidad, las transacciones y las reglas reales corresponden a backend. Los perfiles derivados de responsabilidades son una propuesta de frontend, no una matriz de roles confirmada por el documento funcional.

### Dirección
Un acceso tranquilo a un hotel de lujo: escena arquitectónica cálida a la izquierda y un formulario claro a la derecha. Minimalismo inspirado en la claridad de las interfaces Apple, con tipografía y comportamiento propios de Windows. Mantener el emblema oficial existente.

### Ventana y composición
Ventana inicial 1200 × 760, mínimo 920 × 660. WindowChrome nativo sin marco rectangular tradicional, esquinas de 28 DIPs, zona de arrastre de 44 DIPs, redimensionamiento de 6 DIPs y botones minimizar, maximizar/restaurar y Salir. El panel fotográfico tiene radio 22; campos y botones, radio 12. División de contenido 52% imagen y 48% formulario; el formulario mide hasta 344 DIPs. La vista tiene dimensiones de diseño explícitas para Visual Studio.

### Tipografía y color
Segoe UI para interacción y Georgia para la voz de hospitalidad. Fondo marfil #FAF9F6, tinta #192F40, primario azul #17384F, dorado #D9BB79. Modo oscuro #15222D con texto marfil y botón primario dorado. Los tokens están en HotelPalette.xaml; los estados de foco, hover, pulsado y deshabilitado, en LiquidGlassStyles.xaml.

### Fondo
hospitality-lobby.png fue generado con la herramienta integrada imagegen. Imagen embebida y movimiento de acercamiento muy suave, reversible en 24 segundos, con pausa. Se respeta la preferencia de animación del sistema. La imagen permanece disponible sin Internet. Un MP4 H.264 local opcional reemplaza la fotografía; reproducción silenciada, bucle, pausa y respaldo al fallar. No se incluyó un MP4: la descarga estaba bloqueada en este entorno. No se simula una fotografía como si fuera vídeo.

### Alcance
Solo frontend. Ingresar y el acceso simulado abren el panel de demostración sin validar credenciales. Soporte no llama servicios y Recordarme no guarda datos. No hay autenticación, persistencia ni cambios de backend.

## Colors

La navegación petróleo ancla el puesto; los fondos fríos y las superficies claras priorizan la lectura. Oscuro conserva esa estructura y Marca introduce el verde institucional.

Las rampas OKLCH del sidecar son muestras sintetizadas para el visor documental; no son tokens de WPF ni añaden colores aprobados al producto. Los colores normativos son los del frontmatter y su asignación temática siguiente.

### Primary

- `petroleum`: navegación y acción primaria de Claro.
- `petroleum-selected`: selección de navegación en Claro y Oscuro, siempre con texto blanco.
- `gold`: acción primaria y foco de Oscuro, además de la identidad existente.
- `brand-primary`: acción primaria y foco de Marca.

### Neutral

Los recursos locales `Ui.*` de DashboardView son la interfaz de consumo. `ApplyTheme()` toma HotelPalette y sobrescribe los roles propios del panel. Los valores iniciales del XAML no sustituyen la paleta efectiva de esta tabla. Cada nombre en una celda refiere al token de color del frontmatter.

| Recurso | Claro | Oscuro | Marca |
|---|---|---|---|
| `Ui.Page` | `page` | `dark-page` | `brand-page` |
| `Ui.Ink` | `ink` | `ivory` | `brand-ink` |
| `Ui.Secondary` | `secondary` | `dark-secondary` | `brand-secondary` |
| `Ui.Field` | `white` | `dark-field` | `white` |
| `Ui.Border` | `border` | `dark-border` | `border` |
| `Ui.Soft` | `soft` | `dark-soft` | `brand-soft` |
| `Ui.Primary` | `petroleum` | `gold` | `brand-primary` |
| `Ui.OnPrimary` | `white` | `navy` | `white` |
| `Ui.Focus` | `focus` | `gold` | `brand-primary` |
| `Ui.Nav` | `petroleum` | `dark-nav` | `brand-nav` |
| `Ui.NavSelected` | `petroleum-selected` | `petroleum-selected` | `brand-selected` |
| `Ui.NavInk` | `white` | `white` | `white` |
| `Ui.NavMuted` | `nav-muted` | `nav-muted` | `nav-muted` |
| `Ui.Good` | `good` | `dark-good` | `good` |
| `Ui.Warning` | `warning` | `dark-warning` | `warning` |
| `Ui.Danger` | `danger` | `dark-danger` | `danger` |

`Ui.Good` comunica disponibilidad o confirmación en los ejemplos; `Ui.Warning` señala preparación o revisión pendiente; `Ui.Danger` marca bloqueo, incidencia o prioridad alta. Los demás estados usan `Ui.Secondary`. Siempre conservar el nombre del estado además del color.

**The Local Theme Rule.** Aplicar los cambios del panel mediante sus recursos `Ui.*`; preservar la paleta y las plantillas compartidas que sostienen el login.

## Typography

**Display Font:** Segoe UI en los títulos operativos.
**Body Font:** Segoe UI en datos, navegación, controles y ayudas.
**Brand Font:** Georgia en el nombre Crown & Sovereign; el acceso conserva su combinación existente.

La escala principal del panel es 27 / 22 / 19 / 17 / 13 / 12 / 11 DIPs: página, diálogo, sección, subtítulo, cuerpo, texto auxiliar y metadatos. Títulos SemiBold y cuerpo Regular. Las métricas usan 25 DIPs SemiBold y las fichas de habitación 26; valores de detalle 15 y avisos o pasos guiados 14. Son usos locales existentes, no nuevos encabezados generales.

La marca del rail usa Georgia Bold de 16 DIPs. Los rótulos de navegación usan 12 y sus grupos 11 SemiBold. Pie y etiquetas compactas del calendario usan 11. No se fija interlineado ni tracking: se conservan las métricas nativas.

**The Work Type Rule.** Usar Segoe UI para los títulos del panel; reservar Georgia para la marca y el login existente.

## Layout

El rail mide 208 DIPs y el espacio de trabajo ocupa el resto. Padding del rail: 12 / 20 / 12 / 12; margen principal: 26 / 24 / 22 / 12. Marca arriba y perfil, tema y regreso al acceso abajo. Solo el listado central del menú se desplaza. Encabezado con título y ayuda, y pie de demostración, quedan fuera del scroll del contenido.

El catálogo contiene 22 áreas y muestra únicamente las permitidas al perfil, distribuidas en Mi puesto (Inicio, Alertas), Recepción (Reservas, Habitaciones, Check-in / check-out, Huéspedes, Folios, Operación), Servicios (Housekeeping, Mantenimiento, Alimentos y bebidas, Inventario, Gimnasio y spa, Lavandería, Transporte), Gestión (Convenios, Tarifas, Finanzas, Reportes) y Administración (Usuarios y permisos, Auditoría, Configuración). Los nombres completos proceden del catálogo; las claves internas no sustituyen las etiquetas visibles.

La primera vista presenta métricas autorizadas en una franja de tres columnas, pendientes y accesos del puesto. Los flujos se eligen mediante Área de trabajo. Herramientas y fichas usan WrapPanel. Las fichas de habitación miden 184 DIPs con separación 10. Las tablas conservan ancho mínimo 760 y scroll horizontal. En el mínimo 920 × 660 se mantiene el rail y se desplazan las áreas necesarias; no existe breakpoint web ni menú móvil.

El diálogo centrado mide 540 DIPs, altura máxima 560 y margen 20. El cuerpo se desplaza y las acciones permanecen visibles. El recorrido de Tab queda contenido y el puesto detrás queda deshabilitado.

## Elevation & Depth

El panel usa superficies, separación y bordes de 1 DIP. No hay sombras decorativas ni desenfoque de vidrio. Avisos y encabezados de tabla usan `Ui.Soft`; el diálogo mantiene la superficie de panel sobre un scrim negro al 50%. El emblema conserva sus gradientes originales. El panel no introduce animación; el movimiento del login mantiene el contrato conservado en Overview.

## Shapes

Botones, navegación, ComboBox y popup usan radio 8 DIPs. Paneles y diálogo usan 12. Las barras de desplazamiento tienen ancho 12 y pulgar de radio 4. Los iconos de navegación son vectores de 18 × 18, trazo 1.5 y color enlazado al botón.

El TextBox operativo hereda `HotelTextBox` con radio 12 del estilo compartido del login: excepción existente, no radio general de controles. El login mantiene los radios originales. El radio 6 del brief preliminar no corresponde a las plantillas finales y no debe propagarse.

## Components

### Buttons

Acciones compactas: superficie `Ui.Field`, texto `Ui.Ink`, borde 1 DIP, padding 14 / 9 y altura mínima 38. Primario con `Ui.Primary`, `Ui.OnPrimary` y SemiBold. Hover cambia el borde a `Ui.Focus`; foco de teclado usa borde 2; pulsado reduce opacidad de superficie a 0.72 y deshabilitado a 0.45. Los cambios son inmediatos.

### Navigation

Rail persistente con texto blanco y grupos `Ui.NavMuted`. Entradas de altura mínima 36, padding 12 / 6, margen vertical 1 y alineación izquierda. Selección con `Ui.NavSelected` y SemiBold. Foco y nombres accesibles acompañan los iconos. La visibilidad y los manejadores verifican el contexto de permisos local antes de mostrar áreas o acciones; esto no reemplaza la autorización real.

### Cards / Containers

Superficie `Ui.Field`, borde `Ui.Border`, radio 12 y padding habitual 20. Fichas compactas y avisos usan 16; tablas y listas usan 0 para que cada fila defina su espacio. Las filas tienen separador inferior. Reportes y el inicio autorizado incluyen barras semanales con porcentajes visibles.

### Inputs / Fields

Etiquetas permanentes asociadas al control y hints cuando está vacío. TextBox normal de altura 40, texto 13, padding horizontal 16 y radio compartido 12. Notas usa altura 82, varias líneas y scroll. ComboBox de altura mínima 40 y radio 8, con popup de altura máxima 260; elementos con padding 12 / 9 y resaltado o selección `Ui.Soft`.

El foco tiene borde visible de 2 DIPs. Los errores aparecen junto al campo en `Ui.Danger`, acompañados de resumen anunciado de forma cortés; la revisión enfoca el primer error. Los formularios guiados muestran paso y título. Volver del resumen al editor conserva los datos y solo produce un borrador local.

### Status

Texto de 12 DIPs SemiBold con color semántico. No son chips rellenos: conservar el nombre explícito y su representación tipográfica.

### Calendar

Día, Semana y Mes comparten filas de habitaciones y columnas de fechas. Columna de habitación de 150 DIPs, encabezado 42 y filas 64. Ancho mínimo de 100 por día o 42 por día en Mes, con scroll horizontal.

Cada estancia es un botón continuo que abarca sus noches mediante ColumnSpan; no se repite como botones diarios. Usa `Ui.Soft`, texto 11, tooltip y nombre accesible con huésped, referencia y fechas. Las celdas libres indican Libre. Al activar una estancia o celda se abre primero una ficha contextual. Según permisos, desde ella se prepara reserva, cambio o cancelación / no-show. La reserva o el cambio abre formulario con contexto precargado. La ocupación sigue siendo ilustrativa.

### Dialogs and keyboard

Escape cierra el panel y devuelve foco al activador. Tab recorre el diálogo. F1 abre ayuda sin sustituir un editor abierto; Ctrl+F enfoca la búsqueda. Buscar y filtrar operan sobre las filas ilustrativas visibles, con contador anunciado y estado vacío recuperable. Cerrar descarta la edición del diálogo; volver del resumen al editor conserva los valores. Las acciones no anuncian reservas, cobros o registros reales completados.

## Do's and Don'ts

### Do:

- **Do** conservar el login, su composición 52/48, emblema, imagen y estilos originales.
- **Do** usar recursos locales `Ui.*` y comprobar Claro, Oscuro y Marca al ampliar el panel.
- **Do** mantener el rail de 208 DIPs y las 22 áreas del catálogo con visibilidad por perfil.
- **Do** usar Segoe UI en títulos y datos operativos, con estados expresados mediante nombre y color.
- **Do** abrir contexto antes de editar desde el calendario y conservar la continuidad de las estancias.
- **Do** describir acciones como preparación o revisión de borradores y verificar el mínimo 920 × 660.

### Don't:

- **Don't** convertir este proyecto WPF en una web ni agregar frameworks web.
- **Don't** restaurar doce secciones, rail de 190 DIPs o Georgia en los títulos operativos.
- **Don't** propagar al login cambios de paleta, radios o plantillas propios del panel.
- **Don't** introducir sombras decorativas o animaciones en el panel que no existen en la implementación.
- **Don't** anunciar reservas, cobros, permisos o registros reales como completados desde este frontend.
- **Don't** tratar los perfiles derivados o los colores de estado como autorización efectiva de backend.
