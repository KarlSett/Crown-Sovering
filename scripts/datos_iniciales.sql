-- Datos iniciales para la base nueva (esquema de 72 tablas): hotel, habitaciones, roles y usuarios de prueba.
-- Se puede ejecutar mas de una vez: cada bloque solo inserta lo que todavia no existe.
--
-- Usuarios de prueba (en el login, el campo Usuario es el username):
--   admin     / Admin#2026     (Administrador)
--   recepcion / Recep#2026     (Recepcionista)
--   gerente   / Gerente#2026   (Gerente)
--   servicios / Servicios#2026 (Servicios: limpieza, mantenimiento y consumos)
-- Los datos del hotel, las tarifas y los precios son de ejemplo y se pueden cambiar.

-- Hotel
INSERT INTO public.hotel (nombre, direccion, categoria)
SELECT 'Crown & Sovereign', 'La Paz, Bolivia', 5
WHERE NOT EXISTS (SELECT 1 FROM public.hotel);

-- Roles (los mismos nombres que usa el panel)
INSERT INTO public.rol (nombre, descripcion) VALUES
    ('Administrador', 'Usuarios, auditoria, reportes y configuracion'),
    ('Gerente', 'Finanzas, convenios, tarifas y reportes'),
    ('Recepcionista', 'Reservas, check-in y check-out, huespedes y servicios')
ON CONFLICT (nombre) DO NOTHING;

-- Pisos y tipos de habitacion
INSERT INTO public.piso (hotel_id, nombre)
SELECT (SELECT MIN(hotel_id) FROM public.hotel), v.nombre
FROM (VALUES ('Piso 1'), ('Piso 2'), ('Piso 3'), ('Piso 4')) AS v(nombre)
WHERE NOT EXISTS (SELECT 1 FROM public.piso p WHERE p.nombre = v.nombre);

INSERT INTO public.tipohabitacion (nombre, capacidad, descripcion)
SELECT v.nombre, v.capacidad, v.descripcion
FROM (VALUES ('Individual', 1, 'Cama simple'),
             ('Doble', 2, 'Dos camas individuales'),
             ('Matrimonial', 2, 'Cama matrimonial'),
             ('Triple', 3, 'Tres camas'),
             ('Suite', 4, 'Suite con sala'),
             ('Suite Presidencial', 4, 'Suite presidencial con sala premium')) AS v(nombre, capacidad, descripcion)
WHERE NOT EXISTS (SELECT 1 FROM public.tipohabitacion t WHERE t.nombre = v.nombre);

-- Habitaciones (las mismas que muestra el mapa del panel)
INSERT INTO public.habitacion (piso_id, tipo_habitacion_id, numero, estado)
SELECT p.piso_id, t.tipo_habitacion_id, v.numero, 'DISPONIBLE'
FROM (VALUES ('101', 'Piso 1', 'Individual'), ('102', 'Piso 1', 'Individual'), ('103', 'Piso 1', 'Individual'),
             ('104', 'Piso 1', 'Doble'), ('105', 'Piso 1', 'Doble'), ('106', 'Piso 1', 'Matrimonial'),
             ('107', 'Piso 1', 'Individual'), ('108', 'Piso 1', 'Individual'),
             ('201', 'Piso 2', 'Doble'), ('202', 'Piso 2', 'Matrimonial'), ('203', 'Piso 2', 'Suite'),
             ('204', 'Piso 2', 'Individual'), ('205', 'Piso 2', 'Doble'), ('206', 'Piso 2', 'Triple'),
             ('207', 'Piso 2', 'Individual'), ('208', 'Piso 2', 'Suite'),
             ('301', 'Piso 3', 'Individual'), ('302', 'Piso 3', 'Matrimonial'), ('303', 'Piso 3', 'Doble'),
             ('304', 'Piso 3', 'Suite Presidencial')) AS v(numero, piso, tipo)
JOIN public.piso p ON p.nombre = v.piso
JOIN public.tipohabitacion t ON t.nombre = v.tipo
ON CONFLICT (piso_id, numero) DO NOTHING;

-- La primera version de este script creo 101, 102, 201, 202, 301 y 302 con otros tipos: se alinean con el mapa.
UPDATE public.habitacion h
SET tipo_habitacion_id = t.tipo_habitacion_id
FROM (VALUES ('101', 'Individual'), ('102', 'Individual'), ('301', 'Individual'), ('302', 'Matrimonial')) AS v(numero, tipo)
JOIN public.tipohabitacion t ON t.nombre = v.tipo
WHERE h.numero = v.numero AND h.tipo_habitacion_id <> t.tipo_habitacion_id;

-- Tarifa base por tipo de habitacion (en bolivianos)
INSERT INTO public.tarifa (tipo_habitacion_id, nombre, precio_base)
SELECT t.tipo_habitacion_id, 'Tarifa base', v.precio
FROM (VALUES ('Individual', 80.00), ('Doble', 100.00), ('Matrimonial', 120.00), ('Triple', 150.00),
             ('Suite', 200.00), ('Suite Presidencial', 350.00)) AS v(tipo, precio)
JOIN public.tipohabitacion t ON t.nombre = v.tipo
WHERE NOT EXISTS (SELECT 1 FROM public.tarifa x WHERE x.tipo_habitacion_id = t.tipo_habitacion_id);

-- Catalogos de reservas, pagos y cargos
INSERT INTO public.canalreserva (nombre) VALUES ('Mostrador'), ('Telefono'), ('Web'), ('Agencia')
ON CONFLICT (nombre) DO NOTHING;

INSERT INTO public.metodopago (hotel_id, nombre)
SELECT (SELECT MIN(hotel_id) FROM public.hotel), v.nombre
FROM (VALUES ('Efectivo'), ('Tarjeta'), ('Transferencia')) AS v(nombre)
WHERE NOT EXISTS (SELECT 1 FROM public.metodopago m WHERE m.nombre = v.nombre);

INSERT INTO public.conceptocargo (nombre, categoria)
SELECT v.nombre, v.categoria
FROM (VALUES ('Alojamiento', 'Alojamiento'), ('Restaurante', 'Comidas y bebidas'), ('Minibar', 'Comidas y bebidas'),
             ('Spa', 'Servicios'), ('Lavanderia', 'Servicios'), ('Transporte', 'Servicios'), ('Otros', 'Otros')) AS v(nombre, categoria)
WHERE NOT EXISTS (SELECT 1 FROM public.conceptocargo c WHERE c.nombre = v.nombre);

INSERT INTO public.producto (nombre, categoria, precio, stock_actual, stock_minimo)
SELECT v.nombre, v.categoria, v.precio, v.stock, v.minimo
FROM (VALUES ('Agua mineral', 'Minibar', 3.50, 48, 10), ('Cafe en grano', 'Insumo', 45.00, 12, 5)) AS v(nombre, categoria, precio, stock, minimo)
WHERE NOT EXISTS (SELECT 1 FROM public.producto p WHERE p.nombre = v.nombre);

INSERT INTO public.servicio (nombre, tipo, precio)
SELECT v.nombre, v.tipo, v.precio
FROM (VALUES ('Masaje relajante', 'Spa', 65.00), ('Traslado aeropuerto', 'Transporte', 120.00)) AS v(nombre, tipo, precio)
WHERE NOT EXISTS (SELECT 1 FROM public.servicio s WHERE s.nombre = v.nombre);

-- Empleados y usuarios de prueba (las contrasenas estan guardadas como hash bcrypt)
INSERT INTO public.empleado (nombre, apellido, email)
SELECT v.nombre, v.apellido, v.email
FROM (VALUES ('Prueba', 'Administrador', 'admin@crown.test'),
             ('Prueba', 'Recepcionista', 'recepcion@crown.test'),
             ('Prueba', 'Gerente', 'gerente@crown.test')) AS v(nombre, apellido, email)
WHERE NOT EXISTS (SELECT 1 FROM public.empleado e WHERE e.email = v.email);

INSERT INTO public.usuario (empleado_id, username, password_hash)
SELECT e.empleado_id, v.username, v.hash
FROM (VALUES ('admin@crown.test', 'admin', '$2a$11$x4xn6jmMJdWmhOzOS7.JfORkV4bTdqxbtpRAr8XlFl7tn2lsNgdpO'),
             ('recepcion@crown.test', 'recepcion', '$2a$11$MYAAcRobcrDL83/aLSG28.rFY/8JF6cAkKtpVutoVa/q2YIOmE2t2'),
             ('gerente@crown.test', 'gerente', '$2a$11$4ylYw0zaHyi4O5qXFdpwoucjddHSwkWVyvxiftH4kDd7eDaGmZ9KG')) AS v(email, username, hash)
JOIN public.empleado e ON e.email = v.email
ON CONFLICT (username) DO NOTHING;

INSERT INTO public.usuariorol (usuario_id, rol_id)
SELECT u.usuario_id, r.rol_id
FROM (VALUES ('admin', 'Administrador'), ('recepcion', 'Recepcionista'), ('gerente', 'Gerente')) AS v(username, rol)
JOIN public.usuario u ON u.username = v.username
JOIN public.rol r ON r.nombre = v.rol
WHERE NOT EXISTS (SELECT 1 FROM public.usuariorol x WHERE x.usuario_id = u.usuario_id AND x.rol_id = r.rol_id);

-- Rol y usuario de Servicios (limpieza, mantenimiento y consumos)
INSERT INTO public.rol (nombre, descripcion)
SELECT 'Servicios', 'Personal de limpieza, mantenimiento y consumos'
WHERE NOT EXISTS (SELECT 1 FROM public.rol WHERE nombre = 'Servicios');

INSERT INTO public.usuario (username, password_hash)
VALUES ('servicios', '$2a$11$sLqtayPiTGG5tlrgO6mupObJX.RtwiHgG/qNikW8OW3nXLH8IIzWq')
ON CONFLICT (username) DO NOTHING;

INSERT INTO public.usuariorol (usuario_id, rol_id)
SELECT u.usuario_id, r.rol_id FROM public.usuario u, public.rol r
WHERE u.username = 'servicios' AND r.nombre = 'Servicios'
  AND NOT EXISTS (SELECT 1 FROM public.usuariorol x WHERE x.usuario_id = u.usuario_id AND x.rol_id = r.rol_id);

-- Comprobacion: cuantas filas hay en cada tabla cargada
SELECT 'hotel' AS tabla, COUNT(*) AS filas FROM public.hotel
UNION ALL SELECT 'rol', COUNT(*) FROM public.rol
UNION ALL SELECT 'piso', COUNT(*) FROM public.piso
UNION ALL SELECT 'tipohabitacion', COUNT(*) FROM public.tipohabitacion
UNION ALL SELECT 'habitacion', COUNT(*) FROM public.habitacion
UNION ALL SELECT 'tarifa', COUNT(*) FROM public.tarifa
UNION ALL SELECT 'canalreserva', COUNT(*) FROM public.canalreserva
UNION ALL SELECT 'metodopago', COUNT(*) FROM public.metodopago
UNION ALL SELECT 'conceptocargo', COUNT(*) FROM public.conceptocargo
UNION ALL SELECT 'producto', COUNT(*) FROM public.producto
UNION ALL SELECT 'servicio', COUNT(*) FROM public.servicio
UNION ALL SELECT 'empleado', COUNT(*) FROM public.empleado
UNION ALL SELECT 'usuario', COUNT(*) FROM public.usuario
UNION ALL SELECT 'usuariorol', COUNT(*) FROM public.usuariorol;
