// [LEGACY] معطّل — تم الانتقال إلى WpfPrintingService
/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.Services.Printing;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.Services
{
    public class PrintingService : IPrintingService
    {
        private readonly IWindowsPrintService _windowsPrintService;
        private readonly IPrinterApiService _printerApiService;
        private readonly IPrintStationApiService _printStationApiService;

        public PrintingService(
            IWindowsPrintService windowsPrintService,
            IPrinterApiService printerApiService,
            IPrintStationApiService printStationApiService)
        {
            _windowsPrintService = windowsPrintService;
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
                System.Diagnostics.Debug.WriteLine($"Failed to print customer receipt: {ex.Message}");
            }

            try
            {
                await PrintKitchenTicketsAsync(order);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to print kitchen tickets: {ex.Message}");
            }
        }

        public async Task PrintReceiptAsync(OrderDto order)
        {
            var printersResult = await _printerApiService.GetAllAsync();
            if (!printersResult.IsSuccess || printersResult.Data == null)
            {
                throw new Exception("تعذر جلب قائمة الطابعات من الخادم.");
            }

            var receiptPrinter = printersResult.Data.FirstOrDefault(p => p.PrinterType == PrinterType.Receipt);
            if (receiptPrinter == null)
            {
                System.Diagnostics.Debug.WriteLine("لم يتم العثور على طابعة فواتير (Receipt Printer) في النظام.");
                return;
            }

            string content = ReceiptBuilder.Build(order);
            _windowsPrintService.Print(receiptPrinter.PrinterName, content);
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
            if (!kitchenPrinters.Any())
            {
                System.Diagnostics.Debug.WriteLine("لم يتم العثور على طابعات مطبخ (Kitchen Printers) في النظام.");
                return;
            }

            // Group items by PrintStationId
            var stationItemsMap = new Dictionary<int, List<OrderItemDto>>();
            var stationNameMap = new Dictionary<int, string>();

            foreach (var item in order.OrderItems)
            {
                if (item.CategoryId <= 0)
                {
                    continue;
                }

                var stationsResult = await _printStationApiService.GetStationsByCategoryIdAsync(item.CategoryId);
                if (stationsResult.IsSuccess && stationsResult.Data != null)
                {
                    foreach (var station in stationsResult.Data)
                    {
                        if (!stationItemsMap.ContainsKey(station.Id))
                        {
                            stationItemsMap[station.Id] = new List<OrderItemDto>();
                            stationNameMap[station.Id] = station.Name;
                        }
                        stationItemsMap[station.Id].Add(item);
                    }
                }
            }

            foreach (var kvp in stationItemsMap)
            {
                int stationId = kvp.Key;
                string stationName = stationNameMap[stationId];
                var itemsForStation = kvp.Value;

                // Find kitchen printer linked to this print station
                var printer = kitchenPrinters.FirstOrDefault(p => p.PrintStationId == stationId);
                if (printer == null)
                {
                    System.Diagnostics.Debug.WriteLine($"لم يتم العثور على طابعة مطبخ مرتبطة بمحطة الطباعة: {stationName}");
                    continue;
                }

                string content = KitchenTicketBuilder.Build(order, stationName, itemsForStation);
                _windowsPrintService.Print(printer.PrinterName, content);
            }
        }
    }
}
*/
