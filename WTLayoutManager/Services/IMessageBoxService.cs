using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WTLayoutManager.Services
{
    public enum DialogType
    {
        Information,
        Warning,
        Error,
        Confirmation
    }

    public interface IMessageBoxService
    {
        bool Confirm(string message, string title = "Confirm");
        void ShowMessage(string message, string title = "Information", DialogType dialogType = DialogType.Information);
    }
}