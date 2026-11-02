# Crown & Sovereign — interfaz de escritorio

## Dirección
Un acceso tranquilo a un hotel de lujo: escena arquitectónica cálida a la izquierda y un formulario claro a la derecha. Minimalismo inspirado en la claridad de las interfaces Apple, con tipografía y comportamiento propios de Windows. Mantener el emblema oficial existente.

## Ventana y composición
Ventana inicial 1200 × 760, mínimo 920 × 660. WindowChrome nativo sin marco rectangular tradicional, esquinas de 28 DIPs, zona de arrastre de 44 DIPs, redimensionamiento de 6 DIPs y botones minimizar, maximizar/restaurar y Salir. El panel fotográfico tiene radio 22; campos y botones, radio 12. División de contenido 52% imagen y 48% formulario; el formulario mide hasta 344 DIPs. La vista tiene dimensiones de diseño explícitas para Visual Studio.

## Tipografía y color
Segoe UI para interacción y Georgia para la voz de hospitalidad. Fondo marfil #FAF9F6, tinta #192F40, primario azul #17384F, dorado #D9BB79. Modo oscuro #15222D con texto marfil y botón primario dorado. Los tokens están en HotelPalette.xaml; los estados de foco, hover, pulsado y deshabilitado, en LiquidGlassStyles.xaml.

## Fondo
hospitality-lobby.png fue generado con la herramienta integrada imagegen. Imagen embebida y movimiento de acercamiento muy suave, reversible en 24 segundos, con pausa. Se respeta la preferencia de animación del sistema. La imagen permanece disponible sin Internet. Un MP4 H.264 local opcional reemplaza la fotografía; reproducción silenciada, bucle, pausa y respaldo al fallar. No se incluyó un MP4: la descarga estaba bloqueada en este entorno. No se simula una fotografía como si fuera vídeo.

## Alcance
Solo frontend. Ingresar y el acceso simulado abren el panel de demostración sin validar credenciales. Soporte no llama servicios y Recordarme no guarda datos. No hay autenticación, persistencia ni cambios de backend.

## Panel operativo
Navegación lateral estable de 190 DIPs para las doce secciones. Iconos vectoriales de trazo 1.5 enlazados al color del botón; todas las superficies, textos, estados, campos, desplegables y barras de desplazamiento usan recursos semánticos `Ui.*`, actualizados en los temas Claro, Oscuro y Marca. Georgia queda en los títulos principales; Segoe UI en datos y controles.

El encabezado y el pie permanecen fuera del área de desplazamiento, evitando la superposición que tenía la barra superior. En el tamaño mínimo, el menú permite desplazamiento y las tablas conservan su ancho legible mediante desplazamiento horizontal. Habitaciones utiliza una cuadrícula adaptable de fichas; reportes incluye una gráfica semanal con valores visibles.

Los formularios usan etiquetas permanentes, hints visibles solo en campos vacíos, errores junto al campo y un resumen de revisión. Escape cierra el panel y los valores permanecen al volver del resumen al editor. Buscar y filtrar solo operan sobre datos ilustrativos. Las acciones muestran borradores; no anuncian reservas, cobros o registros completados.
