using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Management.Deployment;

namespace WTLayoutManager.Services
{
    internal class TerminalService : ITerminalService
    {
        private Dictionary<string, TerminalInfo>? _packages;
        private Dictionary<string, TerminalInfo> Packages
        {
            get
            {
                _packages ??= TerminalPackages.FindInstalledTerminals();
                var packages = _packages.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
                return packages;
            }
        }

        public Dictionary<string, TerminalInfo> FindAllTerminals()
        {
            return Packages;
        }
    }
}
