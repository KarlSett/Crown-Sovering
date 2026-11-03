# Integración con backend

La guía vigente de la entrega por roles está en [Frontend/README.md](../Frontend/README.md). Incluye los roles encontrados e inferidos del Word, matriz de accesos, permisos por módulo/acción, contrato propuesto de sesión, archivos y métodos a conectar, pendientes de seguridad y verificación.

El [mapa de requisitos](REQUISITOS_FRONTEND.md) relaciona RF-01–118 con las pantallas y distingue los borradores de las operaciones reales pendientes.

El login se conserva en Frontend/Views/LoginView.xaml y LoginView.xaml.cs. Los controles TxtUsuario, TxtContrasena, ChkRecordarme, BtnSoporteTecnico, BtnIngresar y BtnLoginSimulado pertenecen a LoginView. Ingresar y el acceso simulado siguen sin autenticar y abren una demostración de Recepción. Backend debe reemplazar ese paso por una sesión validada y construir DashboardView(theme, accessContext).

No insertar consultas SQL en las vistas. El proyecto continúa siendo WPF con .NET Framework 4.7.2, sin servicios ni persistencia implementados.