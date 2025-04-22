using MaterialDesignThemes.Wpf;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using WTLayoutManager.Services;
using WTLayoutManager.ViewModels;

namespace WTLayoutManager
{

    /// <summary>
    /// Represents the main application class for the WTLayoutManager, extending the base WPF Application class.
    /// </summary>
    /// <remarks>
    /// This partial class provides the core application logic and initialization for the Windows Terminal Layout Manager application.
    /// </remarks>
    public partial class App : Application
    {
        /// <summary>
        /// Checks whether the current process is running with administrator privileges.
        /// </summary>
        /// <returns>true if the current process is running with administrator privileges, false otherwise.</returns>
        /// <remarks>
        /// This method uses the WindowsPrincipal class to check the current user's role membership.
        /// The WindowsPrincipal class is populated by the current thread's WindowsIdentity, which is
        /// obtained using WindowsIdentity.GetCurrent(). The method checks whether the current user is
        /// a member of the WindowsBuiltInRole.Administrator role to determine whether the process is
        /// running with administrator privileges.
        /// </remarks>
        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            if (identity == null)
            {
                return false;
            }
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Restarts the application with administrator privileges using the "runas" shell verb.
        /// </summary>
        /// <remarks>
        /// If the user refuses the elevation request or an error occurs, a message box is displayed explaining
        /// why the application requires administrator privileges and how to restart the application with the
        /// required privileges.
        /// </remarks>
        private static void RestartAsAdministrator()
        {
            try
            {
                // Get the path to the executable file
                string? exeName = Environment.ProcessPath;

                if (exeName == null)
                {
                    MessageBox.Show("The application requires administrator privileges to run.\n" +
                                "Please restart the application and grant the required privileges.",
                                "Elevation Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var startInfo = new ProcessStartInfo(fileName: exeName)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo);
            }
            catch (Exception)
            {
                // The user refused the elevation request or an error occurred
                MessageBox.Show("The application requires administrator privileges to run.\n" +
                                "Please restart the application and grant the required privileges.",
                                "Elevation Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);

                // Optionally log the exception or handle it as needed
                // For example: LogException(ex);
            }
        }

        /// <summary>
        /// Handles the application startup event.
        /// </summary>
        /// <remarks>
        /// If the current process is not running with administrator privileges, the application will attempt to
        /// restart itself with elevated privileges using the "runas" shell verb. If the user refuses the elevation
        /// request or an error occurs, the application will display a message box explaining why the application
        /// requires administrator privileges and how to restart the application with the required privileges.
        /// </remarks>
        /// <param name="e">The event arguments for the application startup event.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            //if (!IsAdministrator())
            //{
            //    // Attempt to restart the application with elevated privileges
            //    RestartAsAdministrator();
            //    // Shutdown the current instance
            //    Shutdown();
            //    return;
            //}

            base.OnStartup(e);
            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainViewModel(new MessageBoxService());
            mainWindow.Show();
        }
    }

}
