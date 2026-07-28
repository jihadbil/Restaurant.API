using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Core
{
    public class SessionManager
    {
        private static SessionManager? _instance;
        public static SessionManager Instance => _instance ??= new SessionManager();

        public string? Token { get; private set; }
        public UserDto? CurrentUser { get; private set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

        private SessionManager() { }

        public void SetSession(string token, UserDto user)
        {
            Token = token;
            CurrentUser = user;
        }

        public void ClearSession()
        {
            Token = null;
            CurrentUser = null;
        }
    }
}
