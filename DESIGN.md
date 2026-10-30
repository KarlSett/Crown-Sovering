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
Solo frontend. Los botones de ingreso, prueba y soporte permanecen sin acciones. Recordarme cambia únicamente su aspecto; no guarda datos. No hay autenticación ni cambios de backend.