using System.Text.Json;

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
