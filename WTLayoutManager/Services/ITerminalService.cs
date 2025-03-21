namespace WTLayoutManager.Services
{
    public interface ITerminalService
    {
        Dictionary<string, TerminalInfo>? FindAllTerminals();
        //void SaveCurrentLayout(string terminalFolderPath, string savePath);
        //void LoadLayout(string savePath, string terminalFolderPath);
    }
}
