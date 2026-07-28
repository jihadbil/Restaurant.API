using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Restaurant.Desktop.Controls.Printing;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.Services
{
    public class WpfPrintingService : IWpfPrintingService
    {
        private readonly IPrinterApiService _printerApiService;
        private readonly IPrintStationApiService _printStationApiService;

        public WpfPrintingService(
            IPrinterApiService printerApiService,
            IPrintStationApiService printStationApiService)
        {
            _printerApiService = printerApiService;
            _printStationApiService = printStationApiService;
        }

        public async Task PrintOrderAsync(OrderDto order)
        {
            try
            {
                await PrintReceiptAsync(order);
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        $"تعذر طباعة فاتورة العميل:\n{ex.Message}",
                        "تنبيه طباعة الفواتير",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
            }

            try
            {
                await PrintKitchenTicketsAsync(order);
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        $"تعذر طباعة طلبات تحضير المطبخ:\n{ex.Message}",
                        "تنبيه طباعة المطبخ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
            }
        }

        public async Task PrintReceiptAsync(OrderDto order)
        {
            var printersResult = await _printerApiService.GetAllAsync();
            if (!printersResult.IsSuccess || printersResult.Data == null)
            {
                throw new Exception("تعذر جلب قائمة الطابعات من الخادم.");
            }

            var receiptPrinters = printersResult.Data.Where(p => p.PrinterType == PrinterType.Receipt).ToList();
            if (!receiptPrinters.Any())
            {
                throw new Exception("لم يتم العثور على طابعة فواتير (Receipt Printer) في إعدادات النظام.");
            }

            // Determine paper width based on AppSettings
            int paperWidthMm = AppSettings.Instance.ReceiptPaperWidth;
            double widthInPixels = paperWidthMm == 58 ? 220 : 302;

            foreach (var receiptPrinter in receiptPrinters)
            {
                Exception? printException = null;

                // Must print on UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var control = new ReceiptPrintControl();
                        control.SetOrder(order, "مطعم الوجبة اللذيذة"); // Default restaurant name as specified by user

                        // Force layout measurement and arrangement
                        control.Width = widthInPixels;
                        control.Measure(new Size(widthInPixels, double.PositiveInfinity));
                        control.Arrange(new Rect(new Point(0, 0), new Size(widthInPixels, control.DesiredSize.Height)));
                        control.UpdateLayout();

                        // Print setup
                        var printDialog = new PrintDialog();
                        PrintQueue queue = null;
                        try
                        {
                            using (var server = new LocalPrintServer())
                            {
                                queue = server.GetPrintQueue(receiptPrinter.PrinterName);
                                printDialog.PrintQueue = queue;
                            }
                        }
                        catch
                        {
                            using (var server = new LocalPrintServer())
                            {
                                var queues = server.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections });
                                queue = queues.FirstOrDefault(q => q.Name.Equals(receiptPrinter.PrinterName, StringComparison.OrdinalIgnoreCase) || 
                                                                   q.FullName.Equals(receiptPrinter.PrinterName, StringComparison.OrdinalIgnoreCase));
                                if (queue != null)
                                {
                                    printDialog.PrintQueue = queue;
                                }
                                else
                                {
                                    throw new Exception($"الطابعة بالاسم '{receiptPrinter.PrinterName}' غير مثبتة أو غير متصلة بالجهاز.");
                                }
                            }
                        }

                        // Print the visual control
                        printDialog.PrintVisual(control, $"Order #{order.OrderNumber} Receipt");
                        System.Diagnostics.Debug.WriteLine($"Successfully printed receipt for order {order.OrderNumber} to printer {receiptPrinter.PrinterName}");
                    }
                    catch (Exception ex)
                    {
                        printException = ex;
                    }
                });

                if (printException != null)
                {
                    throw new Exception($"خطأ أثناء الطباعة على طابعة '{receiptPrinter.Name}': {printException.Message}", printException);
                }
            }
        }

        public async Task PrintKitchenTicketsAsync(OrderDto order)
        {
            if (order.OrderItems == null || !order.OrderItems.Any())
            {
                return;
            }

            var printersResult = await _printerApiService.GetAllAsync();
            if (!printersResult.IsSuccess || printersResult.Data == null)
            {
                throw new Exception("تعذر جلب قائمة الطابعات من الخادم.");
            }

            var kitchenPrinters = printersResult.Data.Where(p => p.PrinterType == PrinterType.Kitchen).ToList();

            // diagnostic log builder
            var debugInfo = new System.Text.StringBuilder();
            debugInfo.AppendLine($"عدد طابعات المطبخ في النظام: {kitchenPrinters.Count}");
            foreach (var kp in kitchenPrinters)
            {
                debugInfo.AppendLine($"- الطابعة: {kp.Name}, اسم النظام: {kp.PrinterName}, محطة: {kp.PrintStationId} ({kp.PrintStationName})");
            }

            // Group items by PrintStationId
            var stationItemsMap = new Dictionary<int, List<OrderItemDto>>();
            var stationNameMap = new Dictionary<int, string>();

            foreach (var item in order.OrderItems)
            {
                debugInfo.AppendLine($"الصنف: {item.ProductName}, CategoryId: {item.CategoryId}");
                if (item.CategoryId <= 0)
                {
                    debugInfo.AppendLine("  -> تم التخطي لأن CategoryId <= 0");
                    continue;
                }

                var stationsResult = await _printStationApiService.GetStationsByCategoryIdAsync(item.CategoryId);
                if (stationsResult.IsSuccess && stationsResult.Data != null)
                {
                    debugInfo.AppendLine($"  -> عدد المحطات المرتبطة بالتصنيف: {stationsResult.Data.Count}");
                    foreach (var station in stationsResult.Data)
                    {
                        debugInfo.AppendLine($"     - محطة: {station.Id} ({station.Name})");
                        if (!stationItemsMap.ContainsKey(station.Id))
                        {
                            stationItemsMap[station.Id] = new List<OrderItemDto>();
                            stationNameMap[station.Id] = station.Name;
                        }
                        stationItemsMap[station.Id].Add(item);
                    }
                }
                else
                {
                    debugInfo.AppendLine($"  -> فشل جلب المحطة للتصنيف أو النتيجة فارغة. الخطأ: {stationsResult.ErrorMessage}");
                }
            }

            debugInfo.AppendLine($"عدد المحطات التي تجمع فيها أصناف: {stationItemsMap.Count}");

            if (!kitchenPrinters.Any())
            {
                throw new Exception($"لم يتم العثور على طابعات مطبخ (Kitchen) في النظام.\n\nتفاصيل التشخيص:\n{debugInfo}");
            }

            if (!stationItemsMap.Any())
            {
                throw new Exception($"لم يتم تجميع أي أصناف لإرسالها للمطبخ. يرجى التأكد من ربط تصنيف المنتجات بمحطة طباعة.\n\nتفاصيل التشخيص:\n{debugInfo}");
            }

            // Determine kitchen paper width based on AppSettings
            int paperWidthMm = AppSettings.Instance.KitchenPaperWidth;
            double widthInPixels = paperWidthMm == 58 ? 220 : 302;

            foreach (var kvp in stationItemsMap)
            {
                int stationId = kvp.Key;
                string stationName = stationNameMap[stationId];
                var itemsForStation = kvp.Value;

                // Find kitchen printers linked to this print station
                var printers = kitchenPrinters.Where(p => p.PrintStationId == stationId).ToList();
                if (!printers.Any())
                {
                    continue;
                }

                foreach (var printer in printers)
                {
                    Exception? printException = null;

                    // Must print on UI thread
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            var control = new KitchenTicketPrintControl();
                            control.SetOrder(order, stationName, itemsForStation);

                            // Force layout measurement and arrangement
                            control.Width = widthInPixels;
                            control.Measure(new Size(widthInPixels, double.PositiveInfinity));
                            control.Arrange(new Rect(new Point(0, 0), new Size(widthInPixels, control.DesiredSize.Height)));
                            control.UpdateLayout();

                            // Print setup
                            var printDialog = new PrintDialog();
                            PrintQueue queue = null;
                            try
                            {
                                using (var server = new LocalPrintServer())
                                {
                                    queue = server.GetPrintQueue(printer.PrinterName);
                                    printDialog.PrintQueue = queue;
                                }
                            }
                            catch
                            {
                                using (var server = new LocalPrintServer())
                                {
                                    var queues = server.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections });
                                    queue = queues.FirstOrDefault(q => q.Name.Equals(printer.PrinterName, StringComparison.OrdinalIgnoreCase) || 
                                                                       q.FullName.Equals(printer.PrinterName, StringComparison.OrdinalIgnoreCase));
                                    if (queue != null)
                                    {
                                        printDialog.PrintQueue = queue;
                                    }
                                    else
                                    {
                                        throw new Exception($"الطابعة بالاسم '{printer.PrinterName}' غير مثبتة أو غير متصلة بالجهاز.");
                                    }
                                }
                            }

                            // Print the visual control
                            printDialog.PrintVisual(control, $"Order #{order.OrderNumber} Kitchen Ticket - {stationName}");
                            System.Diagnostics.Debug.WriteLine($"Successfully printed kitchen ticket for order {order.OrderNumber} station {stationName} to printer {printer.PrinterName}");
                        }
                        catch (Exception ex)
                        {
                            printException = ex;
                        }
                    });

                    if (printException != null)
                    {
                        throw new Exception($"خطأ أثناء الطباعة في محطة '{stationName}' على طابعة '{printer.Name}': {printException.Message}", printException);
                    }
                }
            }
        }
    }
}
