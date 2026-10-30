# Integración con backend

## Punto de partida

La entrega actual es exclusivamente de frontend. No hay usuarios de prueba, autenticación simulada, conexión a base de datos ni credenciales guardadas. Backend/Data, Models, Repositories y Services contienen marcadores para conservar las carpetas en Git.

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
| BtnIngresar | Button | Ingreso real; sin evento ni comando |
| BtnLoginSimulado | Button | Acceso de demostración pendiente; sin evento ni comando |
| BtnSoporteTecnico | Button | Soporte pendiente; sin evento ni comando |

ThemeToggle y MotionToggle pertenecen a presentación. Los eventos existentes administran únicamente el fondo, el tema y los controles de ventana.

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

La primera entrega fue compilada correctamente en Debug. El login no envía datos ni cambia de pantalla al pulsar sus botones.