# Repositorios de datos

Esta carpeta está reservada para la capa de acceso a PostgreSQL cuando se conecte la
base de datos real. El contrato previsto es simple:

- Un repositorio por agregado (`Reservas`, `Habitaciones`, `Folios`, `Ingresos`).
- Cada repositorio recibe una cadena de conexión y devuelve las entidades de
  `Backend/Models`.
- `Backend/Services/ServicioHotel` consume los repositorios; hoy consume
  `Backend/Data/DatosHotel`, que mantiene los mismos datos de demostración en memoria.

Mientras no exista conexión, no se agregan clases aquí para no dejar código muerto.
