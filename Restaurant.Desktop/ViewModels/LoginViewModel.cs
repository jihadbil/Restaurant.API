using System;
using System.Threading.Tasks;
using System.Windows;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;
using Restaurant.Desktop.ViewModels.Base;
using Restaurant.Desktop.Views;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthApiService _authApiService;
        private readonly IServiceProvider _serviceProvider;

        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set
            {
                if (SetProperty(ref _userName, value))
                {
                    LoginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public AsyncRelayCommand<object> LoginCommand { get; }

        public LoginViewModel(IAuthApiService authApiService, IServiceProvider serviceProvider)
        {
            _authApiService = authApiService;
            _serviceProvider = serviceProvider;

            LoginCommand = new AsyncRelayCommand<object>(ExecuteLoginAsync, CanLogin);
        }

        private bool CanLogin(object? parameter)
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(UserName);
        }

        private async Task ExecuteLoginAsync(object? parameter)
        {
            ClearErrors();

            string password = string.Empty;
            if (parameter is System.Windows.Controls.PasswordBox passwordBox)
            {
                password = passwordBox.Password;
            }
            else if (parameter is string passStr)
            {
                password = passStr;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "يرجى إدخال كلمة المرور.";
                return;
            }

            IsBusy = true;
            try
            {
                var loginDto = new LoginRequestDto
                {
                    UserName = UserName,
                    Password = password
                };

                var result = await _authApiService.LoginAsync(loginDto);
                if (result.IsSuccess && result.Data != null)
                {
                    var response = result.Data;
                    if (response.IsSuccess && response.User != null)
                    {
                        SessionManager.Instance.SetSession(response.Token, response.User);

                        // Navigate to MainWindow
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var mainWindow = _serviceProvider.GetService(typeof(MainWindow)) as MainWindow;
                            if (mainWindow != null)
                            {
                                mainWindow.Show();
                                
                                // Close current login window
                                foreach (Window window in Application.Current.Windows)
                                {
                                    if (window is LoginWindow)
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
                        ErrorMessage = response.Message ?? "فشل تسجيل الدخول.";
                    }
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "فشل تسجيل الدخول. يرجى التحقق من البيانات.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"حدث خطأ غير متوقع: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
