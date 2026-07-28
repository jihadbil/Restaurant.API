using System.Collections.Generic;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IWindowsPrintService
    {
        void Print(string printerName, string content);
        List<string> GetInstalledPrinters();
    }
}
