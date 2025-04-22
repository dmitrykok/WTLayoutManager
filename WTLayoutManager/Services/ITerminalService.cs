/// <summary>
/// Defines the contract for a terminal service that manages terminal-related operations.
/// </summary>
/// <remarks>
/// Provides methods for discovering and managing terminal instances within the application.
/// </remarks>
namespace WTLayoutManager.Services
{
    /// <summary>
    /// Defines a service for managing and discovering terminal instances within the application.
    /// </summary>
    /// <remarks>
    /// Provides methods to interact with terminal resources and retrieve terminal information.
    /// </remarks>
    public interface ITerminalService
    {
        Dictionary<string, TerminalInfo>? FindAllTerminals();
        //void SaveCurrentLayout(string terminalFolderPath, string savePath);
        //void LoadLayout(string savePath, string terminalFolderPath);
    }
}
