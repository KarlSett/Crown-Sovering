# Pendientes para completar la entrega

La aplicación ofrece flujos de demostración en memoria para operación, reservas, inventario, huéspedes, consentimiento, estancias, cargos, cobros, devoluciones, facturas, convenios, limpieza, consumos, configuración y reportes CSV. Los datos desaparecen al cerrar la aplicación.

## Datos que debe proporcionar o confirmar el equipo

- Acceso a PostgreSQL y un script de esquema ejecutable, o autorización para preparar una migración con el equipo de backend. El archivo de esquema adjunto es solo contexto y no incluye una instancia o credenciales.
- Política de identidad: proveedor de usuarios, hash de contraseñas, restablecimiento, bloqueo, sesiones y autorización por operación. El selector de rol no autentica.
- Imágenes finales de habitaciones, productos y perfiles, con procedencia/licencia. El fondo y el emblema actual ya funcionan.
- Moneda y zona horaria del hotel, impuestos aplicables y reglas de caja para calcular los totales legales.
- Credenciales SMTP si se requiere entregar notificaciones y reportes por correo.

## Trabajo de integración para producción

- Reemplazar el almacén volátil por repositorios PostgreSQL con transacciones, concurrencia y mapeo de las claves del esquema. El modelo de demostración usa `Guid` para algunos agregados, mientras que el SQL usa claves enteras.
- Aplicar impuestos, temporadas, tarifas por canal, paquetes y descuentos al cálculo de reservas, folios y facturas. El demo permite mantener catálogos, pero no determina reglas fiscales ni recalcula reservas con ellos.
- Completar las tablas específicas de reservas avanzadas (grupos, lista de espera, recursos y solicitudes especiales) y el flujo detallado de inspecciones, mantenimiento y objetos olvidados.
- Persistir auditoría, historial de habitaciones, turnos y cierres, devoluciones y consentimientos con permisos de servidor.
- Activar entregas reales de correo y ejecución automática de reportes. La generación manual disponible produce archivos CSV locales; no envía correos ni exporta PDF/XLSX.
- Definir y aplicar las reglas de caja para cierres, anulaciones, reaperturas y conciliación por método de pago.

## Validación del entorno

El proyecto es WPF clásico para .NET Framework 4.7.2. En este entorno no se pudo completar la compilación de WPF porque el SDK instalado no generó los archivos de `InitializeComponent`; ábrase y compílese en Visual Studio con el Developer Pack de .NET Framework 4.7.2.
