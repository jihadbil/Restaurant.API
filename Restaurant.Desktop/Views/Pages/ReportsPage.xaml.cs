using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Restaurant.Desktop.ViewModels;

namespace Restaurant.Desktop.Views.Pages
{
    public partial class ReportsPage : UserControl
    {
        public ReportsPage()
        {
            InitializeComponent();
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ReportsViewModel;
            if (vm == null) return;

            string reportTitle = "";
            switch (vm.ActiveTab)
            {
                case "Sales": reportTitle = "تقرير المبيعات اليومية"; break;
                case "Products": reportTitle = "تقرير الأصناف الأكثر مبيعاً"; break;
                case "Categories": reportTitle = "تقرير التصنيفات الأكثر مبيعاً"; break;
                case "PaymentMethods": reportTitle = "تقرير وسائل الدفع"; break;
                case "Cancelled": reportTitle = "تقرير الطلبات الملغاة"; break;
            }

            FlowDocument doc = new FlowDocument();
            doc.FlowDirection = FlowDirection.RightToLeft;
            doc.PagePadding = new Thickness(40);
            doc.FontFamily = new FontFamily("Segoe UI");

            // Header Section
            var pHeader = new Paragraph(new Run("مطعم الوجبة اللذيذة"))
            {
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            doc.Blocks.Add(pHeader);

            var pTitle = new Paragraph(new Run(reportTitle))
            {
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            doc.Blocks.Add(pTitle);

            var pPeriod = new Paragraph(new Run($"الفترة من: {vm.StartDate:yyyy/MM/dd}  إلى: {vm.EndDate:yyyy/MM/dd}"))
            {
                FontSize = 11,
                Foreground = Brushes.DimGray,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(pPeriod);

            // Table Creation
            Table table = new Table
            {
                CellSpacing = 0,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // Set column widths and headers based on active tab
            TableRowGroup headerGroup = new TableRowGroup();
            TableRow headerRow = new TableRow();

            List<string> headers = new List<string>();
            List<GridLength> columnWidths = new List<GridLength>();

            if (vm.ActiveTab == "Sales")
            {
                headers = new List<string> { "التاريخ", "عدد الطلبات", "إجمالي المبيعات", "التكلفة الإجمالية", "الأرباح المحققة" };
                columnWidths = new List<GridLength> {
                    new GridLength(1.5, GridUnitType.Star),
                    new GridLength(1.0, GridUnitType.Star),
                    new GridLength(1.5, GridUnitType.Star),
                    new GridLength(1.5, GridUnitType.Star),
                    new GridLength(1.5, GridUnitType.Star)
                };
            }
            else if (vm.ActiveTab == "Products")
            {
                headers = new List<string> { "المنتج", "الباركود", "الكمية المباعة", "إجمالي الإيرادات", "التكلفة الإجمالية", "صافي الأرباح" };
                columnWidths = new List<GridLength> {
                    new GridLength(2.0, GridUnitType.Star),
                    new GridLength(1.2, GridUnitType.Star),
                    new GridLength(1.0, GridUnitType.Star),
                    new GridLength(1.5, GridUnitType.Star),
                    new GridLength(1.5, GridUnitType.Star),
                    new GridLength(1.5, GridUnitType.Star)
                };
            }
            else if (vm.ActiveTab == "Categories")
            {
                headers = new List<string> { "اسم التصنيف", "الكمية المباعة", "إجمالي المبيعات", "التكلفة الإجمالية", "صافي الأرباح" };
                columnWidths = new List<GridLength> {
                    new GridLength(2.0, GridUnitType.Star),
                    new GridLength(1.0, GridUnitType.Star),
                    new GridLength(1.5, GridUnitType.Star),
                    new GridLength(1.5, GridUnitType.Star),
                    new GridLength(1.5, GridUnitType.Star)
                };
            }
            else if (vm.ActiveTab == "PaymentMethods")
            {
                headers = new List<string> { "وسيلة الدفع", "عدد العمليات", "إجمالي القيمة المحصلة" };
                columnWidths = new List<GridLength> {
                    new GridLength(2.0, GridUnitType.Star),
                    new GridLength(1.0, GridUnitType.Star),
                    new GridLength(2.0, GridUnitType.Star)
                };
            }
            else if (vm.ActiveTab == "Cancelled")
            {
                headers = new List<string> { "رقم الطلب", "تاريخ الطلب", "قيمة الطلب الملغى", "الكاشير المسؤول", "سبب الإلغاء / ملاحظات" };
                columnWidths = new List<GridLength> {
                    new GridLength(1.0, GridUnitType.Star),
                    new GridLength(1.5, GridUnitType.Star),
                    new GridLength(1.2, GridUnitType.Star),
                    new GridLength(1.2, GridUnitType.Star),
                    new GridLength(2.0, GridUnitType.Star)
                };
            }

            foreach (var w in columnWidths)
            {
                table.Columns.Add(new TableColumn { Width = w });
            }

            foreach (var h in headers)
            {
                headerRow.Cells.Add(CreateCell(h, isHeader: true));
            }
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            // Populate data rows
            TableRowGroup bodyGroup = new TableRowGroup();
            int rowIndex = 0;

            if (vm.ActiveTab == "Sales")
            {
                foreach (var item in vm.DailySales)
                {
                    var row = new TableRow();
                    row.Background = (rowIndex % 2 == 0) ? Brushes.White : new SolidColorBrush(Color.FromRgb(250, 250, 250));
                    row.Cells.Add(CreateCell(item.Date.ToString("yyyy/MM/dd")));
                    row.Cells.Add(CreateCell(item.TotalOrders.ToString()));
                    row.Cells.Add(CreateCell(item.TotalSales.ToString("C1")));
                    row.Cells.Add(CreateCell(item.TotalCost.ToString("C1")));
                    row.Cells.Add(CreateCell(item.TotalProfit.ToString("C1")));
                    bodyGroup.Rows.Add(row);
                    rowIndex++;
                }

                // Add footer totals
                var footerRow = new TableRow();
                footerRow.Cells.Add(CreateCell("المجموع الإجمالي", isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.SalesTotalOrders.ToString(), isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.SalesTotalSales.ToString("C1"), isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.SalesTotalCost.ToString("C1"), isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.SalesTotalProfit.ToString("C1"), isFooter: true));
                bodyGroup.Rows.Add(footerRow);
            }
            else if (vm.ActiveTab == "Products")
            {
                foreach (var item in vm.BestProducts)
                {
                    var row = new TableRow();
                    row.Background = (rowIndex % 2 == 0) ? Brushes.White : new SolidColorBrush(Color.FromRgb(250, 250, 250));
                    row.Cells.Add(CreateCell(item.Name));
                    row.Cells.Add(CreateCell(item.BarCode ?? "-"));
                    row.Cells.Add(CreateCell(item.QuantitySold.ToString()));
                    row.Cells.Add(CreateCell(item.TotalRevenue.ToString("C1")));
                    row.Cells.Add(CreateCell(item.TotalCost.ToString("C1")));
                    row.Cells.Add(CreateCell(item.TotalProfit.ToString("C1")));
                    bodyGroup.Rows.Add(row);
                    rowIndex++;
                }

                var footerRow = new TableRow();
                footerRow.Cells.Add(CreateCell("المجموع الإجمالي", isFooter: true));
                footerRow.Cells.Add(CreateCell("", isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.ProductsTotalQuantity.ToString(), isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.ProductsTotalRevenue.ToString("C1"), isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.ProductsTotalCost.ToString("C1"), isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.ProductsTotalProfit.ToString("C1"), isFooter: true));
                bodyGroup.Rows.Add(footerRow);
            }
            else if (vm.ActiveTab == "Categories")
            {
                foreach (var item in vm.BestCategories)
                {
                    var row = new TableRow();
                    row.Background = (rowIndex % 2 == 0) ? Brushes.White : new SolidColorBrush(Color.FromRgb(250, 250, 250));
                    row.Cells.Add(CreateCell(item.Name));
                    row.Cells.Add(CreateCell(item.QuantitySold.ToString()));
                    row.Cells.Add(CreateCell(item.TotalRevenue.ToString("C1")));
                    row.Cells.Add(CreateCell(item.TotalCost.ToString("C1")));
                    row.Cells.Add(CreateCell(item.TotalProfit.ToString("C1")));
                    bodyGroup.Rows.Add(row);
                    rowIndex++;
                }

                var footerRow = new TableRow();
                footerRow.Cells.Add(CreateCell("المجموع الإجمالي", isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.CategoriesTotalQuantity.ToString(), isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.CategoriesTotalRevenue.ToString("C1"), isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.CategoriesTotalCost.ToString("C1"), isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.CategoriesTotalProfit.ToString("C1"), isFooter: true));
                bodyGroup.Rows.Add(footerRow);
            }
            else if (vm.ActiveTab == "PaymentMethods")
            {
                foreach (var item in vm.PaymentMethods)
                {
                    var row = new TableRow();
                    row.Background = (rowIndex % 2 == 0) ? Brushes.White : new SolidColorBrush(Color.FromRgb(250, 250, 250));
                    row.Cells.Add(CreateCell(item.PaymentMethodName));
                    row.Cells.Add(CreateCell(item.TotalOrders.ToString()));
                    row.Cells.Add(CreateCell(item.TotalSales.ToString("C1")));
                    bodyGroup.Rows.Add(row);
                    rowIndex++;
                }

                var footerRow = new TableRow();
                footerRow.Cells.Add(CreateCell("المجموع الإجمالي", isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.PaymentMethodsTotalOrders.ToString(), isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.PaymentMethodsTotalSales.ToString("C1"), isFooter: true));
                bodyGroup.Rows.Add(footerRow);
            }
            else if (vm.ActiveTab == "Cancelled")
            {
                foreach (var item in vm.CancelledOrders)
                {
                    var row = new TableRow();
                    row.Background = (rowIndex % 2 == 0) ? Brushes.White : new SolidColorBrush(Color.FromRgb(250, 250, 250));
                    row.Cells.Add(CreateCell(item.OrderNumber.ToString()));
                    row.Cells.Add(CreateCell(item.Date.ToString("yyyy/MM/dd HH:mm")));
                    row.Cells.Add(CreateCell(item.Total.ToString("C1")));
                    row.Cells.Add(CreateCell(item.UserName));
                    row.Cells.Add(CreateCell(item.Notes ?? ""));
                    bodyGroup.Rows.Add(row);
                    rowIndex++;
                }

                var footerRow = new TableRow();
                footerRow.Cells.Add(CreateCell("المجموع الإجمالي", isFooter: true));
                footerRow.Cells.Add(CreateCell("", isFooter: true));
                footerRow.Cells.Add(CreateCell(vm.CancelledTotal.ToString("C1"), isFooter: true));
                footerRow.Cells.Add(CreateCell("", isFooter: true));
                footerRow.Cells.Add(CreateCell("", isFooter: true));
                bodyGroup.Rows.Add(footerRow);
            }

            table.RowGroups.Add(bodyGroup);
            doc.Blocks.Add(table);

            // Print document
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                doc.PageWidth = printDialog.PrintableAreaWidth;
                doc.PageHeight = printDialog.PrintableAreaHeight;
                printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "طباعة تقارير مطعم الوجبة اللذيذة");
            }
        }

        private TableCell CreateCell(string text, bool isHeader = false, bool isFooter = false)
        {
            var p = new Paragraph(new Run(text))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0),
                Padding = new Thickness(0)
            };

            var cell = new TableCell(p)
            {
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = Brushes.LightGray,
                Padding = new Thickness(6, 8, 6, 8)
            };

            if (isHeader)
            {
                cell.Background = new SolidColorBrush(Color.FromRgb(242, 242, 242));
                p.FontWeight = FontWeights.Bold;
                cell.BorderThickness = new Thickness(0, 0, 0, 2);
                cell.BorderBrush = Brushes.DarkGray;
            }
            else if (isFooter)
            {
                cell.Background = new SolidColorBrush(Color.FromRgb(248, 248, 248));
                p.FontWeight = FontWeights.Bold;
                cell.BorderThickness = new Thickness(0, 2, 0, 0);
                cell.BorderBrush = Brushes.DarkGray;
            }

            return cell;
        }
    }
}
