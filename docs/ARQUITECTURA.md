# Crown & Sovereign — sistema hotelero

Aplicación WPF con .NET Framework 4.7.2, organizada para dos integrantes de frontend y
dos de backend. La interfaz funciona con datos de demostración en memoria; la capa de
datos está aislada para sustituirla por PostgreSQL sin tocar las vistas.

## Estructura

```
Crown &
├─ App.xaml                      Recursos globales y arranque
├─ MainWindow.xaml               Ventana con WindowChrome y navegación por Frame
├─ Backend/
│  ├─ Data/DatosHotel.cs         Almacén de demostración (datos semilla)
│  ├─ Models/Entidades.cs        Entidades espejo del esquema PostgreSQL
│  ├─ Models/Formato.cs          Formatos de moneda, fecha y porcentaje
│  ├─ Models/Reportes.cs         Modelos de reportes e indicadores
│  ├─ Repositories/              Reservado para ADO.NET; ver README.md
│  └─ Services/ServicioHotel.cs  Reglas de negocio y consultas
├─ Frontend/
│  ├─ Assets/                    Fotografía, emblema y video opcional
│  ├─ Controls/                  Controles reutilizables
│  │  ├─ KpiCard.xaml            Tarjeta de indicador
│  │  ├─ SectionCard.cs          Contenedor con título (plantilla en Styles)
│  │  └─ StatusBadge.xaml        Insignia de estado con color y conteo
│  ├─ Styles/
│  │  ├─ LiquidGlassStyles.xaml  Emblema, campos, botones y estados del login
│  │  └─ DashboardStyles.xaml    Tokens del panel: tarjetas, tabla, navegación
│  ├─ Themes/HotelPalette.xaml   Colores y tipografías
│  └─ Views/
│     ├─ LoginView.xaml          Acceso y panel de hospitalidad
│     ├─ DashboardView.xaml      Shell: barra lateral, encabezado y secciones
│     └─ Sections/
│        ├─ FuncionamientoView   Ocupación, llegadas, salidas y movimientos
│        ├─ ReservasView         Alta, filtros y operación de reservas
│        ├─ HabitacionesView     Disponibilidad por fechas, tipo y calidad
│        └─ IngresosView         Resúmenes diario, semanal y trimestral
├─ docs/                         Arquitectura, diseño e integración
└─ scripts/                      Publicación del repositorio
```

## Frontend

- `MainWindow.xaml`: ventana de 1200 × 760, mínimo 920 × 660. WindowChrome con esquinas
  de 28 DIPs, barra de arrastre y botones minimizar, maximizar/restaurar y Salir.
- `Frontend/Views/DashboardView.xaml`: composición (barra lateral agrupada por rol,
  encabezado, tema claro/oscuro) y host de las 17 vistas. No contiene lógica de negocio.
- `Frontend/Views/Sections/*`: secciones operativas (Funcionamiento, Reservas,
  Habitaciones, Ingresos) con su lógica de presentación y carga.
- `Frontend/Views/Modules/*`: trece pantallas de módulo en español, cada una con una
  disposición adecuada a su tarea (lista+ficha, mosaico, tablero semanal, kanban, matriz,
  consola de caja, bandeja, pestañas laterales, acordeón, tarjetas). Solo controles por
  defecto, sin estilos.
- `Frontend/Views/Dialogs/*`: ventanas emergentes de alta (huésped, habitación, reserva)
  para no ocupar un panel lateral con formularios. Sin persistencia todavía.
- `Frontend/Sesion.cs`: rol de la demostración (`Rol`). El login permite elegir entre
  Administrador, Gerente, Recepcionista y Personal de servicios. La visibilidad del menú
  es solo de presentación; ver `docs/MODULOS_Y_ROLES.md`.
- `Frontend/Controls/*`: piezas reutilizables. `KpiCard` y `StatusBadge` son
  `UserControl`; `SectionCard` es un `ContentControl` con plantilla, para que el
  contenido declarado en la vista conserve su namescope (`x:Name`).
- `Frontend/Themes/HotelPalette.xaml`: tokens de color y tipografía.
- `Frontend/Styles/LiquidGlassStyles.xaml`: emblema y controles del login.
- `Frontend/Styles/DashboardStyles.xaml`: estilos del panel (se fusiona en `App.xaml`
  para que los controles de `Frontend/Controls` los encuentren).
- `Frontend/Assets/hospitality-lobby.png`: fondo local generado con imagegen; su prompt
  está en `hospitality-lobby.prompt.txt`.

## Backend

`Backend/Data/DatosHotel`, `Backend/Models` y `Backend/Services` implementan hoy el flujo
completo con datos de demostración. `Backend/Repositories` queda reservado para la
conexión PostgreSQL descrita en `docs/INTEGRACION_BACKEND.md`.

## Controles del login

`TxtUsuario`, `TxtContrasena`, `ChkRecordarme`, `BtnSoporteTecnico`, `BtnIngresar`,
`BtnLoginSimulado`, `BtnAccesoAdmin` y `CmbRol` conservan sus nombres. No hay validación
ni autenticación real: `BtnIngresar` y `BtnLoginSimulado` abren el panel con el rol
elegido en `CmbRol`, y `BtnAccesoAdmin` fuerza el rol Administrador.

## Roles

`docs/MODULOS_Y_ROLES.md` describe la matriz pantalla → tablas → roles y aclara que el
cliente solo oculta entradas del menú; la autorización real corresponde al backend.

## Diseño y revisión

`docs/DESIGN.md` documenta el lenguaje visual. Los renders de comprobación están en
`docs/images`.
