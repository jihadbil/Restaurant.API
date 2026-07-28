using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using Restaurant.Desktop.Services.IServices;

namespace Restaurant.Desktop.Services
{
    // [LEGACY - معطّل] تم الانتقال إلى WpfPrintingService
    public class WindowsPrintService : IWindowsPrintService
    {
        public void Print(string printerName, string content)
        {
            // [LEGACY] معطّل — الطباعة تتم الآن عبر WpfPrintingService و WPF Controls
            throw new NotSupportedException("الطباعة النصية القديمة معطلة. يرجى استخدام WpfPrintingService.");
        }

        public List<string> GetInstalledPrinters()
        {
            var list = new List<string>();
            try
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    list.Add(printer);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving installed printers: {ex.Message}");
            }
            return list;
        }
    }
}
