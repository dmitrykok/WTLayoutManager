using System.Text.Json;

/// <summary>
/// Provides default JSON serialization options for terminal-related JSON processing.
/// </summary>
/// <remarks>
/// Configures JSON serialization to skip comments and allow trailing commas,
/// which is useful for more flexible JSON parsing in terminal configuration scenarios.
/// </remarks>
namespace WTLayoutManager.Services
{
    internal class TerminalJsonOptions
    {
        public static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }
}
