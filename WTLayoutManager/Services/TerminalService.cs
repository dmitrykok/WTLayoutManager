using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;

/// <summary>
/// Provides services for discovering and managing terminal packages on the system.
/// </summary>
/// <remarks>
/// This service uses memory-mapped files and an administrative process to retrieve
/// terminal package information securely and efficiently.
/// </remarks>
namespace WTLayoutManager.Services
{
    internal class TerminalService : ITerminalService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalService"/> class.
        /// </summary>
        /// <param name="messageBoxService">The message box service used for displaying dialogs and messages.</param>
        public TerminalService(IMessageBoxService messageBoxService)
        {
            _messageBoxService = messageBoxService;
        }

        /// <summary>
        /// Provides a service for displaying message box dialogs within the terminal service.
        /// </summary>
        private readonly IMessageBoxService _messageBoxService;

        /// <summary>
        /// Gets a dictionary of terminal packages by using memory-mapped files to communicate with an admin process.
        /// </summary>
        /// <returns>A dictionary of terminal information, or null if no data is received.</returns>
        /// <remarks>
        /// Creates a unique memory-mapped file, starts an admin process to populate the file,
        /// and then deserializes the received JSON data into terminal package information.
        /// </remarks>
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

        /// <summary>
        /// Finds and returns all known terminal packages installed on the system.
        /// </summary>
        /// <returns>
        /// A dictionary of terminal information, keyed by common name.
        /// May return null if the data is not available.
        /// </returns>
        public Dictionary<string, TerminalInfo>? FindAllTerminals()
        {
            return Packages;
        }

        /// <summary>
        /// Starts the WTerminalPackages.exe as an administrator process
        /// with the given <paramref name="mapName"/> as an argument.
        /// Waits for the process to exit.
        /// </summary>
        /// <param name="mapName">The name of the memory-mapped file to be used
        /// for communication with the admin process.</param>
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
