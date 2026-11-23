# Pendientes para completar la entrega

La integración PostgreSQL, los repositorios y servicios de operación están en el proyecto y las pantallas principales usan esos servicios. Para ejecutar el sistema conectado se necesita una instancia PostgreSQL con el esquema compatible y una configuración local válida. No se versionan credenciales.

## Requiere información o recursos del equipo

- Configurar `Conexion.config` local usando [Conexion.config.example](../Conexion.config.example), y proporcionar acceso a una instancia PostgreSQL donde se haya aplicado el esquema del equipo.
- Imágenes finales de habitaciones, productos y perfiles, con procedencia/licencia. El fondo y el emblema actuales son locales y funcionan.
- Confirmar moneda, zona horaria, impuestos y reglas contables de caja antes de usar importes como valores legales.
- Proporcionar SMTP y destinatarios si se requiere el envío real de notificaciones por correo.

## Trabajo pendiente para producción

- Revisar contra la versión definitiva del esquema los nombres y tipos de columnas y ejecutar las operaciones contra una base de integración. El archivo de esquema adjunto al encargo es solo contexto.
- Completar la cobertura de datos para reservas avanzadas, solicitudes especiales, inspecciones, mantenimiento y objetos olvidados donde el esquema lo permita.
- Definir reglas fiscales, tarifas por canal, paquetes, descuentos, conciliación y reapertura de caja; el código no puede inferir políticas del hotel.
- Verificar el alcance de permisos por operación y revisar los datos semilla antes de ejecutarlos en cualquier entorno compartido.
- Configurar SMTP para envíos automáticos. La exportación local CSV no equivale al envío de reportes.

## Imágenes

Las imágenes finales de habitaciones, productos y perfiles quedan pendientes hasta que el equipo indique sus archivos y cómo desea administrarlos.

## Validación

El proyecto es WPF clásico para .NET Framework 4.7.2. Para una compilación reproducible se necesita Visual Studio con el Developer Pack de .NET Framework 4.7.2 y restaurar los paquetes declarados (Npgsql y BCrypt.Net-Next).
