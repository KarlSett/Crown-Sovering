# Crown & Sovereign — sistema hotelero

Aplicación WPF con .NET Framework 4.7.2, organizada para dos integrantes de frontend y dos de backend.

## Frontend
- MainWindow.xaml: ventana de 1200 × 760, mínimo 920 × 660. WindowChrome con esquinas exteriores de 28 DIPs, barra de arrastre y botones minimizar, maximizar/restaurar y Salir.
- MainWindow.xaml.cs: acciones de la ventana.
- Frontend/Views/LoginView.xaml: panel hotelero y formulario lateral, tema claro/oscuro y dimensiones de previsualización para Visual Studio.
- Frontend/Views/LoginView.xaml.cs: únicamente presentación del fondo, pausa, reproducción del MP4 opcional y recorte de las esquinas del panel.
- Frontend/Themes/HotelPalette.xaml: tokens de color y tipografía.
- Frontend/Styles/LiquidGlassStyles.xaml: emblema, campos, botones y estados visuales reutilizables.
- Frontend/Assets/hospitality-lobby.png: fondo local generado con imagegen; su prompt está en hospitality-lobby.prompt.txt.
- Frontend/Assets/Video/README.md: incorporación de un MP4 local opcional.

## Backend
Backend/Data, Models, Repositories y Services continúan reservados para conexión, entidades, consultas y reglas de negocio. Este trabajo no añade lógica en esas carpetas.

## Controles para una futura integración
TxtUsuario, TxtContrasena, ChkRecordarme, BtnSoporteTecnico, BtnIngresar y BtnLoginSimulado conservan sus nombres. Los botones de ingreso, acceso simulado y soporte no tienen acciones. No hay validación, autenticación, navegación, almacenamiento de credenciales ni conexión a base de datos.

## Diseño y revisión
DESIGN.md documenta el lenguaje visual. AGENTS.md indica las guías locales añadidas y el alcance de solo frontend. .design-preview conserva el estado previo a este rediseño y los renders de comprobación de los temas claro/oscuro y tamaño mínimo.