using System;
using System.Windows;

namespace FootPedalApp
{
    public partial class App : Application
    {
        private TrayApp tray;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Don't create MainWindow at all - just start the tray app
            try
            {
                tray = new TrayApp();
                tray.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            tray?.Dispose();
            base.OnExit(e);
        }
    }
}
