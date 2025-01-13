using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WTLayoutManager.Services
{
    public interface ITerminalService
    {
        Dictionary<string, TerminalInfo> FindAllTerminals();
        //void SaveCurrentLayout(string terminalFolderPath, string savePath);
        //void LoadLayout(string savePath, string terminalFolderPath);
    }
}
