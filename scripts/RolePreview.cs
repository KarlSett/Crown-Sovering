using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

// Independent development harness; never adds a role switch to the application/login.
internal static class RolePreview
{
    [STAThread]
    private static int Main(string[] args)
    {
        string path = Environment.GetEnvironmentVariable("CROWN_FRONTEND_ASSEMBLY");
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => e.Name.StartsWith("Crown &,") ? Assembly.LoadFrom(path) : null;
        try {
            var assembly = Assembly.LoadFrom(path);
            object access = assembly.GetType("Crown__.Frontend.Security.FrontendAccessContext").GetMethod("CreateDemonstration").Invoke(null, new object[]{args.Length > 0 ? args[0] : "Recepcion"});
            var app = (Application)Activator.CreateInstance(assembly.GetType("Crown__.App"));
            app.GetType().GetMethod("InitializeComponent").Invoke(app, null); app.StartupUri = null;
            var window = (Window)Activator.CreateInstance(assembly.GetType("Crown__.MainWindow"));
            var frame = (Frame)window.FindName("LoginFrame"); frame.Source = null;
            frame.Navigate(Activator.CreateInstance(assembly.GetType("Crown__.Frontend.Views.DashboardView"), new object[]{args.Length > 1 ? args[1] : "Light", access}));
            app.Run(window); return 0;
        } catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
