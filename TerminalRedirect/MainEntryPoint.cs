using System;
using System.IO;
using System.Runtime.InteropServices;
using EasyHook;

namespace TerminalRedirect
{
    // We'll define a delegate that matches CreateFileW's signature
    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    delegate IntPtr CreateFileW_Delegate(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    public class MainEntryPoint : IEntryPoint
    {
        // We'll store the hook itself here
        LocalHook _createFileWHook = null;

        // The "redirection" path, e.g. "C:\MyCustomFolder"
        private string _redirectFromRoot;
        private string _redirectToRoot;

        // This constructor is called once inside the target process
        public MainEntryPoint(RemoteHooking.IContext context, string[] redirectArgs)
        {
            _redirectFromRoot = redirectArgs[0];
            _redirectToRoot = redirectArgs[1];
        }

        // This is called once the injection is complete
        public void Run(RemoteHooking.IContext context, string redirectRoot)
        {
            try
            {
                // Install the hook: 
                // 1) get the address of CreateFileW in kernel32
                IntPtr procAddress = LocalHook.GetProcAddress("kernel32.dll", "CreateFileW");

                // 2) create the hook, specifying which function to call instead
                _createFileWHook = LocalHook.Create(
                    procAddress,
                    new CreateFileW_Delegate(CreateFileW_Hooked),
                    this);

                // 3) Activate the hook on all threads in the process
                _createFileWHook.ThreadACL.SetInclusiveACL(new int[] { 0 });

                // Keep the thread alive; RemoteHooking.WakeUpProcess might be used
                RemoteHooking.WakeUpProcess();
                while (true)
                {
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                // Logging or debugging
            }
        }

        // This is the actual replacement function
        // Original signature: https://learn.microsoft.com/windows/win32/api/fileapi/nf-fileapi-createfilew
        static IntPtr CreateFileW_Hooked(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            // 1) Grab 'this' instance
            var thisObj = (MainEntryPoint)HookRuntimeInfo.Callback;

            // 2) Check if the path is the Terminal's LocalState
            if (IsLocalStatePath(lpFileName, thisObj._redirectFromRoot))
            {
                // 3) Rewrite it to the custom folder
                string newPath = lpFileName.Replace(
                    thisObj._redirectFromRoot,
                    thisObj._redirectToRoot);

                // 4) Call the original CreateFileW with the modified path
                return Original_CreateFileW(
                    newPath,
                    dwDesiredAccess,
                    dwShareMode,
                    lpSecurityAttributes,
                    dwCreationDisposition,
                    dwFlagsAndAttributes,
                    hTemplateFile);
            }
            else
            {
                // 5) Not matching localState path => pass through
                return Original_CreateFileW(
                    lpFileName,
                    dwDesiredAccess,
                    dwShareMode,
                    lpSecurityAttributes,
                    dwCreationDisposition,
                    dwFlagsAndAttributes,
                    hTemplateFile);
            }
        }

        // We'll store the pointer to the original function here
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateFileW")]
        static extern IntPtr Original_CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        private static bool IsLocalStatePath(string path, string searchPath)
        {
            // e.g. check if it starts with "C:\Users\Me\AppData\Local\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState"
            // or a substring check if you want partial matches
            return path.IndexOf(searchPath, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
