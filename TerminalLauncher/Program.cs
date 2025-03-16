using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminalRedirect
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Path to your compiled Hook DLL
                string injectionLibrary = args[0];

                // For instance, run Windows Terminal:
                // If you have a direct path:
                string targetExe = args[1];

                // The "redirect root" folder
                string redirectFromRoot = args[2];
                string redirectToRoot = args[3];
                string[] redirectArgs = { redirectFromRoot, redirectToRoot };

                int processId;
                // CreateAndInject starts the process suspended, injects the DLL,
                // and resumes the process
                EasyHook.RemoteHooking.CreateAndInject(
                    targetExe,
                    "",           // Command line arguments to wt.exe
                    0,            // process creation flags
                    EasyHook.InjectionOptions.NoService | EasyHook.InjectionOptions.DoNotRequireStrongName | EasyHook.InjectionOptions.NoWOW64Bypass,
                    injectionLibrary,
                    injectionLibrary,
                    out processId,
                    // arguments to pass to the Hook constructor
                    redirectArgs
                );

                Console.WriteLine($"Injected into process id {processId}. Press ENTER to exit.");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to inject: " + ex);
            }
        }
    }
}
