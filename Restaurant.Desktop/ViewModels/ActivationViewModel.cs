using System;
using System.Threading.Tasks;
using System.Windows;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.ViewModels.Base;
using Restaurant.Desktop.Views;

namespace Restaurant.Desktop.ViewModels
{
    public class ActivationViewModel : BaseViewModel
    {
        private readonly ILicenseService _licenseService;
        private readonly IServiceProvider _serviceProvider;

        private string _licenseKey = string.Empty;
        public string LicenseKey
        {
            get => _licenseKey;
            set
            {
                if (SetProperty(ref _licenseKey, value))
                {
                    ActivateCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _machineFingerprint = string.Empty;
        public string MachineFingerprint
        {
            get => _machineFingerprint;
            private set => SetProperty(ref _machineFingerprint, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isSuccess;
        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public AsyncRelayCommand<object> ActivateCommand { get; }

        public ActivationViewModel(ILicenseService licenseService, IServiceProvider serviceProvider)
        {
            _licenseService = licenseService;
            _serviceProvider = serviceProvider;

            MachineFingerprint = _licenseService.GetMachineFingerprint();
            ActivateCommand = new AsyncRelayCommand<object>(ExecuteActivateAsync, CanActivate);
        }

        private bool CanActivate(object? parameter)
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(LicenseKey);
        }

        private async Task ExecuteActivateAsync(object? parameter)
        {
            IsBusy = true;
            StatusMessage = "جاري التفعيل...";
            IsSuccess = true; // For default color (neutral)

            try
            {
                var result = await _licenseService.ActivateLicenseAsync(LicenseKey);
                
                if (result.Success)
                {
                    IsSuccess = true;
                    StatusMessage = "تم تفعيل الترخيص بنجاح!";
                    
                    // Show login window
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var loginWindow = _serviceProvider.GetService(typeof(LoginWindow)) as LoginWindow;
                        if (loginWindow != null)
                        {
                            loginWindow.Show();
                            
                            // Close activation window
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window is ActivationWindow)
                                {
                                    window.Close();
                                    break;
                                }
                            }
                        }
                    });
                }
                else
                {
                    IsSuccess = false;
                    StatusMessage = result.Error ?? "فشل التفعيل لسبب غير معروف.";
                }
            }
            catch (Exception ex)
            {
                IsSuccess = false;
                StatusMessage = $"حدث خطأ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
