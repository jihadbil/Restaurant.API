using System;

namespace Restaurant.Desktop.Services
{
    public interface IToastService
    {
        event Action<string, string>? OnShow;
        void ShowSuccess(string message);
        void ShowError(string message);
        void ShowWarning(string message);
        void ShowInfo(string message);
    }
}
