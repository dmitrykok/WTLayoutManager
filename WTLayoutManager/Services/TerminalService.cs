using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;

namespace WTLayoutManager.Services
{
    internal class TerminalService : ITerminalService
    {
        public TerminalService(IMessageBoxService messageBoxService) 
        {
            _messageBoxService = messageBoxService;
        }

        private readonly IMessageBoxService _messageBoxService;
        private Dictionary<string, TerminalInfo>? Packages
        {
            get
            {
                string mapName = "WTMmf_" + Guid.NewGuid().ToString();
                const long mapSize = 1024 * 1024; // e.g. 1 MB

                using (var mmf = MemoryMappedFile.CreateNew(mapName, mapSize))
                {
                    StartAdminProcess(mapName);

                    using (var accessor = mmf.CreateViewAccessor(0, mapSize, MemoryMappedFileAccess.Read))
                    {
                        // We'll read from offset 0 until we hit a zero byte or the end.
                        byte[] buffer = new byte[mapSize];
                        accessor.ReadArray(0, buffer, 0, buffer.Length);

                        // Convert to string
                        int stringLength = 0;
                        while (stringLength < buffer.Length && buffer[stringLength] != 0)
                        {
                            stringLength++;
                        }

                        string jsonString = Encoding.UTF8.GetString(buffer, 0, stringLength);

                        // De-serialize the JSON
                        if (!string.IsNullOrEmpty(jsonString))
                        {
                            var _packages = JsonSerializer.Deserialize<Dictionary<string, TerminalInfo>>(jsonString, TerminalPackages.SerializerOptions);
                            // Console.WriteLine("Received {0} TerminalInfo items.", _packages?.Count ?? 0);
                            return _packages;
                            // Do something with the data
                            // ...
                        }
                        else
                        {
                            //MessageBox.Show(
                            //    "No Terminal Packages data received.",
                            //    "WTLayout Manager",
                            //    MessageBoxButton.OK, MessageBoxImage.Warning);
                            _messageBoxService.ShowMessage("No Terminal Packages data received.", "Warning", DialogType.Warning);
                        }
                    }
                    return null;
                }
            }
        }

        public Dictionary<string, TerminalInfo>? FindAllTerminals()
        {
            return Packages;
        }

        private void StartAdminProcess(string mapName)
        {
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "WTerminalPackages.exe",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
                Arguments = mapName,
                WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                //WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WTLayoutManager", "bin"),
            };

            try
            {
                Process? proc = Process.Start(startInfo);
                if (proc == null)
                {
                    //MessageBox.Show(
                    //    "Failed to start the admin process.",
                    //    "WTLayout Manager",
                    //    MessageBoxButton.OK, MessageBoxImage.Warning);
                    _messageBoxService.ShowMessage("Failed to start the admin process.", "Warning", DialogType.Warning);
                }
                else
                {
                    // Optionally store 'proc' if you want to do 'WaitForExit()' or check exit codes
                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(
                //    $"Error starting admin process: {ex.Message}",
                //    "WTLayout Manager",
                //    MessageBoxButton.OK, MessageBoxImage.Warning);
                _messageBoxService.ShowMessage($"Error starting admin process: {ex.Message}", "Error", DialogType.Error);
            }
        }
    }
}
