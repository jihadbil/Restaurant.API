using System.Windows;
using System.Windows.Controls;
using Restaurant.Desktop.ViewModels;

namespace Restaurant.Desktop.Views
{
    public partial class LoginWindow : Window
    {
        private Control? _activeInputControl;
        private bool _isShiftActive = false;

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // Hook GotFocus events
            TxtUserName.GotFocus += InputControl_GotFocus;
            TxtPassword.GotFocus += InputControl_GotFocus;
            TxtPasswordReveal.GotFocus += InputControl_GotFocus;

            // Two-way synchronization between PasswordBox and Reveal TextBox
            TxtPassword.PasswordChanged += (s, e) => {
                TxtPasswordWatermark.Visibility = string.IsNullOrEmpty(TxtPassword.Password) ? Visibility.Visible : Visibility.Collapsed;
                if (TxtPassword.Visibility == Visibility.Visible)
                {
                    TxtPasswordReveal.Text = TxtPassword.Password;
                }
            };
            TxtPasswordReveal.TextChanged += (s, e) => {
                TxtPasswordWatermark.Visibility = string.IsNullOrEmpty(TxtPasswordReveal.Text) ? Visibility.Visible : Visibility.Collapsed;
                if (TxtPasswordReveal.Visibility == Visibility.Visible)
                {
                    TxtPassword.Password = TxtPasswordReveal.Text;
                }
            };

            // Monitor ViewModel property changes for error shaking
            viewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(LoginViewModel.HasError) && viewModel.HasError)
                {
                    var storyboard = this.Resources["ShakeAnimation"] as System.Windows.Media.Animation.Storyboard;
                    storyboard?.Begin();
                }
            };
        }

        private void InputControl_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            _activeInputControl = sender as Control;
        }

        private void EnsureActiveControl()
        {
            if (_activeInputControl == null)
            {
                _activeInputControl = TxtUserName;
                TxtUserName.Focus();
            }
        }

        private void ShowKeyboard_Click(object sender, RoutedEventArgs e)
        {
            KeyboardPanel.Visibility = KeyboardPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void HideKeyboard_Click(object sender, RoutedEventArgs e)
        {
            KeyboardPanel.Visibility = Visibility.Collapsed;
        }

        private void Key_Click(object sender, RoutedEventArgs e)
        {
            EnsureActiveControl();
            if (sender is Button btn)
            {
                string insertText = btn.Content.ToString() ?? "";
                
                if (_activeInputControl is TextBox textBox)
                {
                    int selectionStart = textBox.SelectionStart;
                    textBox.Text = textBox.Text.Insert(selectionStart, insertText);
                    textBox.SelectionStart = selectionStart + insertText.Length;
                    textBox.Focus();
                }
                else if (_activeInputControl is PasswordBox passwordBox)
                {
                    passwordBox.Password += insertText;
                    passwordBox.Focus();
                }
            }
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            EnsureActiveControl();
            if (_activeInputControl is TextBox textBox && textBox.Text.Length > 0)
            {
                int selectionStart = textBox.SelectionStart;
                if (selectionStart > 0)
                {
                    textBox.Text = textBox.Text.Remove(selectionStart - 1, 1);
                    textBox.SelectionStart = selectionStart - 1;
                }
                textBox.Focus();
            }
            else if (_activeInputControl is PasswordBox passwordBox && passwordBox.Password.Length > 0)
            {
                passwordBox.Password = passwordBox.Password.Substring(0, passwordBox.Password.Length - 1);
                passwordBox.Focus();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            EnsureActiveControl();
            if (_activeInputControl is TextBox textBox)
            {
                textBox.Text = string.Empty;
                textBox.Focus();
            }
            else if (_activeInputControl is PasswordBox passwordBox)
            {
                passwordBox.Password = string.Empty;
                passwordBox.Focus();
            }
        }

        private void Space_Click(object sender, RoutedEventArgs e)
        {
            EnsureActiveControl();
            if (_activeInputControl is TextBox textBox)
            {
                int selectionStart = textBox.SelectionStart;
                textBox.Text = textBox.Text.Insert(selectionStart, " ");
                textBox.SelectionStart = selectionStart + 1;
                textBox.Focus();
            }
            else if (_activeInputControl is PasswordBox passwordBox)
            {
                passwordBox.Password += " ";
                passwordBox.Focus();
            }
        }

        private void Shift_Click(object sender, RoutedEventArgs e)
        {
            _isShiftActive = !_isShiftActive;
            
            // Highlight Shift button if active
            if (sender is Button btnShift)
            {
                btnShift.Background = _isShiftActive 
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(56, 224, 123)) 
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            }
            
            // Update keyboard letter buttons case
            UpdateKeyboardKeyCase();
        }

        private void UpdateKeyboardKeyCase()
        {
            if (KeyboardPanel.Child is StackPanel mainStack)
            {
                foreach (var child in mainStack.Children)
                {
                    if (child is StackPanel row)
                    {
                        foreach (var element in row.Children)
                        {
                            if (element is Button btn && btn.Content is string content && content.Length == 1 && char.IsLetter(content[0]))
                            {
                                btn.Content = _isShiftActive ? content.ToUpper() : content.ToLower();
                            }
                        }
                    }
                }
            }
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (TxtPassword.Visibility == Visibility.Visible)
            {
                // Sync and reveal password
                TxtPasswordReveal.Text = TxtPassword.Password;
                TxtPassword.Visibility = Visibility.Collapsed;
                TxtPasswordReveal.Visibility = Visibility.Visible;
                PasswordToggleIcon.Data = (System.Windows.Media.Geometry)FindResource("IconEyeOff");
                TxtPasswordReveal.Focus();
                _activeInputControl = TxtPasswordReveal;
            }
            else
            {
                // Sync and hide password
                TxtPassword.Password = TxtPasswordReveal.Text;
                TxtPasswordReveal.Visibility = Visibility.Collapsed;
                TxtPassword.Visibility = Visibility.Visible;
                PasswordToggleIcon.Data = (System.Windows.Media.Geometry)FindResource("IconEye");
                TxtPassword.Focus();
                _activeInputControl = TxtPassword;
            }
        }
    }
}
