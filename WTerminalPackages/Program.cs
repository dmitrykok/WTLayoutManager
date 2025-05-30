﻿// See https://aka.ms/new-console-template for more information
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace WTLayoutManager.Services
{
    internal class Program
    {
        /// <summary>
        /// The entry point for the application.
        /// </summary>
        /// <param name="args">An array containing the memory-mapped file name as its first element.</param>
        /// <remarks>
        /// The method attempts to open an existing memory-mapped file specified by <paramref name="args"/>.
        /// If successful, it gathers information about installed terminal packages, serializes this data to JSON,
        /// and writes it to the memory-mapped file. Any exceptions encountered during this process are logged to the error output.
        /// The memory-mapped file must have sufficient size to accommodate the serialized data.
        /// </remarks>
        private static void Main(string[] args)
        {
            //Dictionary<string, TerminalInfo> __packages = TerminalPackages.FindInstalledTerminals();

            if (args.Length < 1)
            {
                Console.Error.WriteLine("No memory-mapped file name passed in.");
                return;
            }

            string mapName = args[0];

            try
            {
                using (var mmf = MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.ReadWrite))
                {
                    // Gather data with PackageManager
                    Dictionary<string, TerminalInfo> _packages = TerminalPackages.FindInstalledTerminals();
                    var packages = _packages.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
                    string jsonString = JsonSerializer.Serialize(packages, TerminalPackages.SerializerOptions);

                    // Write to the memory-mapped file
                    // MUST be <= the map size (1 MB in example).
                    using (var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Write))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(jsonString);
                        accessor.WriteArray(0, data, 0, data.Length);
                        // Optionally, write a 0 byte at the end to mark termination
                        accessor.Write(data.Length, (byte)0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to open memory-mapped file: {ex.Message}");
            }

            //Dictionary<string, TerminalInfo> _packages = TerminalPackages.FindInstalledTerminals();
            //var packages = _packages.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
            //string jsonString = JsonSerializer.Serialize(packages);
            //Console.WriteLine(jsonString);
        }
    }
}