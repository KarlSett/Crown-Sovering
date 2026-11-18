# Crown & Sovereign — interfaz de escritorio

## Dirección
Un acceso tranquilo a un hotel de lujo: escena arquitectónica cálida a la izquierda y un
formulario claro a la derecha. El panel mantiene la misma voz: superficies marfil, acentos
dorados y una barra lateral azul profundo. Minimalismo inspirado en la claridad de las
interfaces Apple, con tipografía y comportamiento propios de Windows. Mantener el emblema
oficial existente.

## Ventana y composición
Ventana inicial 1200 × 760, mínimo 920 × 660. WindowChrome nativo sin marco rectangular
tradicional, esquinas de 28 DIPs, zona de arrastre de 44 DIPs, redimensionamiento de 6 DIPs
y botones minimizar, maximizar/restaurar y Salir. El panel fotográfico tiene radio 22;
campos y botones, radio 12. División de contenido 52% imagen y 48% formulario; el
formulario mide hasta 344 DIPs. Las vistas tienen dimensiones de diseño explícitas para
Visual Studio.

## Panel
Barra lateral fija de 236 DIPs sobre `Brand.Navy`, con el emblema, cuatro accesos
(Funcionamiento, Reservas, Habitaciones, Cuentas de ingreso) y el bloque de sesión. El
contenido usa tarjetas de radio 16 con borde tenue; los indicadores (`KpiCard`) muestran
etiqueta en mayúsculas, valor grande y una pista. `SectionCard` agrupa contenido con
título y nota. `StatusBadge` resume estados con un punto de color y su conteo. Las tablas
son `DataGrid` sin líneas, con encabezado en versalitas y filas de 40 DIPs.

## Tipografía y color
Segoe UI para interacción y Georgia para la voz de hospitalidad. Fondo marfil #FAF9F6,
tinta #192F40, primario azul #17384F, dorado #D9BB79. Modo oscuro #15222D con texto marfil
y botón primario dorado. Los tokens están en `Frontend/Themes/HotelPalette.xaml`; los
estilos del login, en `LiquidGlassStyles.xaml`; los del panel, en `DashboardStyles.xaml`
(fusionado en `App.xaml`).

## Fondo
`hospitality-lobby.png` fue generado con la herramienta integrada imagegen. Imagen embebida
y movimiento de acercamiento muy suave, reversible en 24 segundos, con pausa. Se respeta la
preferencia de animación del sistema. La imagen permanece disponible sin Internet. Un MP4
H.264 local opcional reemplaza la fotografía; reproducción silenciada, bucle, pausa y
respaldo al fallar. No se incluyó un MP4: la descarga estaba bloqueada en este entorno. No
se simula una fotografía como si fuera vídeo.

## Alcance
Interfaz completa con datos de demostración. No hay autenticación real ni conexión a base
de datos; `BtnIngresar` y `BtnLoginSimulado` abren el panel. Recordarme cambia únicamente su
aspecto. El tema claro/oscuro se mantiene en login y panel.
