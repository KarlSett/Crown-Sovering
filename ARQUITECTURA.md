# Crown & Sovereign — sistema hotelero

Aplicación WPF con .NET Framework 4.7.2, organizada para dos integrantes de frontend y dos de backend.

## Frontend
- MainWindow.xaml: ventana de 1200 × 760, mínimo 920 × 660. WindowChrome con esquinas exteriores de 28 DIPs, barra de arrastre y botones minimizar, maximizar/restaurar y Salir.
- MainWindow.xaml.cs: acciones de la ventana.
- Frontend/Views/LoginView.xaml: panel hotelero y formulario lateral, tema claro/oscuro y dimensiones de previsualización para Visual Studio.
- Frontend/Views/LoginView.xaml.cs: presentación del fondo, tema, contraseña revelada y navegación de demostración sin autenticación.
- Frontend/Views/DashboardView.xaml: estructura de navegación lateral, recursos semánticos, controles y panel de edición/revisión.
- Frontend/Views/DashboardView.xaml.cs: composición de módulos, filtros locales, detalles, validación visual y previsualización de borradores.
- Frontend/Views/ModuleCatalog.cs: metadatos originales y registro de las 22 áreas de presentación. Separado de las entidades de negocio.
- Frontend/Views/OperationsCatalog.cs: áreas nuevas, flujos, campos y referencias a los RF del Word.
- Frontend/Views/DashboardWorkspaces.cs: resumen por perfil, selector de flujos, calendario, ayuda y revisión guiada.
- Frontend/Views/ReservationImportPreview.cs: lectura/validación local de CSV y XLSX, sin importar ni persistir reservas.
- Frontend/Security/FrontendAccessContext.cs: permisos explícitos e inmutables de presentación y perfiles sintéticos; no autentica usuarios ni protege servicios.
- Frontend/README.md: matriz interpretada, contrato de sesión y trabajo pendiente de backend.
- Frontend/Themes/HotelPalette.xaml: tokens de color y tipografía.
- Frontend/Styles/LiquidGlassStyles.xaml: emblema, campos, botones y estados visuales reutilizables.
- Frontend/Assets/hospitality-lobby.png: fondo local generado con imagegen; su prompt está en hospitality-lobby.prompt.txt.
- Frontend/Assets/Video/README.md: incorporación de un MP4 local opcional.

## Backend
Backend/Data, Models, Repositories y Services continúan reservados para conexión, entidades, consultas y reglas de negocio. Este trabajo no añade lógica en esas carpetas.

## Controles para una futura integración
TxtUsuario, TxtContrasena, ChkRecordarme, BtnSoporteTecnico, BtnIngresar y BtnLoginSimulado conservan sus nombres. OnIngresar abre DashboardView con el tema actual sin validar credenciales. Soporte no llama servicios. No hay autenticación, almacenamiento de credenciales ni conexión a base de datos. La validación del panel es de formato y presentación; no reemplaza las reglas de negocio de backend.

## Diseño y revisión
DESIGN.md documenta el lenguaje visual. AGENTS.md indica las guías locales añadidas y el alcance de solo frontend. .design-preview conserva el estado previo a este rediseño y los renders de comprobación de los temas claro/oscuro y tamaño mínimo.
