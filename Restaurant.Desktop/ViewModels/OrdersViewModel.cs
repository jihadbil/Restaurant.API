using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.ViewModels.Base;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.ViewModels
{
    public class OrdersViewModel : BaseViewModel
    {
        private readonly IOrderApiService _orderApiService;

        private ObservableCollection<OrderDto> _orders = new();
        public ObservableCollection<OrderDto> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        private ObservableCollection<OrderDto> _filteredOrders = new();
        public ObservableCollection<OrderDto> FilteredOrders
        {
            get => _filteredOrders;
            set
            {
                if (SetProperty(ref _filteredOrders, value))
                {
                    OnPropertyChanged(nameof(IsOrdersListEmpty));
                }
            }
        }

        public bool IsOrdersListEmpty => FilteredOrders == null || !FilteredOrders.Any();

        private OrderDto? _selectedOrder;
        public OrderDto? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (SetProperty(ref _selectedOrder, value))
                {
                    IsDetailsVisible = value != null;
                }
            }
        }

        private bool _isDetailsVisible;
        public bool IsDetailsVisible
        {
            get => _isDetailsVisible;
            set => SetProperty(ref _isDetailsVisible, value);
        }

        private string _viewMode = "Table";
        public string ViewMode
        {
            get => _viewMode;
            set => SetProperty(ref _viewMode, value);
        }

        private bool _filterByDate = true;
        public bool FilterByDate
        {
            get => _filterByDate;
            set
            {
                if (SetProperty(ref _filterByDate, value))
                {
                    ApplyFilter();
                }
            }
        }

        private DateTime? _selectedDateFrom = DateTime.Today;
        public DateTime? SelectedDateFrom
        {
            get => _selectedDateFrom;
            set
            {
                if (SetProperty(ref _selectedDateFrom, value))
                {
                    ApplyFilter();
                }
            }
        }

        private DateTime? _selectedDateTo = DateTime.Today;
        public DateTime? SelectedDateTo
        {
            get => _selectedDateTo;
            set
            {
                if (SetProperty(ref _selectedDateTo, value))
                {
                    ApplyFilter();
                }
            }
        }

        public bool CanVoidOrder => HasPermission("Permission.Orders.VoidOrder");

        private bool HasPermission(string permissionName)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null) return false;
            if (user.Roles != null && user.Roles.Contains("Admin")) return true;
            return user.Permissions != null && user.Permissions.Contains(permissionName);
        }

        private string _statusFilter = "All";
        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (SetProperty(ref _statusFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public AsyncRelayCommand LoadOrdersCommand { get; }
        public AsyncRelayCommand<object> ChangeStatusCommand { get; }
        public AsyncRelayCommand<int> DeleteOrderCommand { get; }
        public RelayCommand CloseDetailsCommand { get; }

        public OrdersViewModel(IOrderApiService orderApiService)
        {
            _orderApiService = orderApiService;

            LoadOrdersCommand = new AsyncRelayCommand(LoadOrdersAsync);
            ChangeStatusCommand = new AsyncRelayCommand<object>(ExecuteChangeStatusAsync);
            DeleteOrderCommand = new AsyncRelayCommand<int>(DeleteOrderAsync);
            CloseDetailsCommand = new RelayCommand(() => SelectedOrder = null);

            _ = LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            ClearErrors();
            IsBusy = true;
            try
            {
                var result = await _orderApiService.GetAllAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    Orders = new ObservableCollection<OrderDto>(result.Data.OrderByDescending(o => o.Date));
                    ApplyFilter();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل تحميل قائمة الطلبات.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            var query = Orders.AsEnumerable();

            // 1. Status Filter
            if (StatusFilter != "All" && Enum.TryParse<OrderStatus>(StatusFilter, out var statusVal))
            {
                query = query.Where(o => o.OrderStatus == statusVal);
            }

            // 2. Date Filter
            if (FilterByDate)
            {
                if (SelectedDateFrom.HasValue)
                {
                    query = query.Where(o => o.Date.Date >= SelectedDateFrom.Value.Date);
                }
                if (SelectedDateTo.HasValue)
                {
                    query = query.Where(o => o.Date.Date <= SelectedDateTo.Value.Date);
                }
            }

            FilteredOrders = new ObservableCollection<OrderDto>(query.ToList());
        }

        private async Task ExecuteChangeStatusAsync(object? parameter)
        {
            // parameter is expected to be a string like "Id,NewStatus"
            if (parameter == null) return;
            
            var parts = parameter.ToString()?.Split(',');
            if (parts == null || parts.Length != 2) return;

            if (int.TryParse(parts[0], out int id) && Enum.TryParse<OrderStatus>(parts[1], out var newStatus))
            {
                var order = Orders.FirstOrDefault(o => o.Id == id);
                if (order == null) return;

                ClearErrors();
                IsBusy = true;
                try
                {
                    var dto = new OrderUpdateDto
                    {
                        Id = order.Id,
                        OrderStatus = newStatus,
                        OrderType = order.OrderType,
                        Notes = order.Notes,
                        Discount = order.Discount
                    };

                    var result = await _orderApiService.UpdateAsync(order.Id, dto);
                    if (result.IsSuccess)
                    {
                        order.OrderStatus = newStatus;
                        // update locally in current collection
                        var idx = Orders.IndexOf(order);
                        if (idx >= 0)
                        {
                            Orders[idx] = null!; // Trigger reset or property notifications
                            Orders[idx] = order;
                        }
                        ApplyFilter();
                        if (SelectedOrder?.Id == id)
                        {
                            // refresh detail view
                            var temp = SelectedOrder;
                            SelectedOrder = null;
                            SelectedOrder = temp;
                        }
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage ?? "فشل تعديل حالة الطلب.";
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"خطأ: {ex.Message}";
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private async Task DeleteOrderAsync(int id)
        {
            var owner = System.Windows.Application.Current.MainWindow;
            var confirmed = Restaurant.Desktop.Controls.ConfirmWindow.Show(
                owner,
                "إلغاء الفاتورة",
                "هل أنت متأكد من رغبتك في إلغاء/حذف هذا الطلب بالكامل؟ لا يمكن التراجع عن هذا الإجراء لاحقاً.",
                "نعم، احذف الطلب",
                "danger");

            if (!confirmed) return;

            ClearErrors();
            IsBusy = true;
            try
            {
                var result = await _orderApiService.DeleteAsync(id);
                if (result.IsSuccess)
                {
                    var order = Orders.FirstOrDefault(o => o.Id == id);
                    if (order != null)
                    {
                        Orders.Remove(order);
                    }
                    SelectedOrder = null;
                    ApplyFilter();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل حذف الطلب.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
