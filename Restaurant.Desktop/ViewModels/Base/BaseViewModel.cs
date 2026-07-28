using CommunityToolkit.Mvvm.ComponentModel;

namespace Restaurant.Desktop.ViewModels.Base
{
    public class BaseViewModel : ObservableObject
    {
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public void ClearErrors()
        {
            ErrorMessage = null;
        }
    }
}
