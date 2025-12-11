using EventManagementSystem.Models;

namespace EventManagementSystem.Services
{
    public interface ISessionManager
    {
        void SetUserSession(User user);
        User? GetUserSession();
        void ClearSession();
        bool IsAuthenticated();
        bool IsAdmin();
        bool IsOrganizer();
        bool IsCustomer();

        string GetUserName();
    }

    public class SessionManager : ISessionManager
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionManager(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetUserSession(User user)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                session.SetInt32("UserId", user.Id);
                session.SetString("UserName", user.Name);
                session.SetString("UserEmail", user.Email);
                session.SetString("UserRole", user.Role);
            }
        }

        public string GetUserName()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            return session?.GetString("UserName") ?? "Customer";
        }

        public User? GetUserSession()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return null;

            var userId = session.GetInt32("UserId");
            if (!userId.HasValue) return null;

            return new User
            {
                Id = userId.Value,
                Name = session.GetString("UserName") ?? string.Empty,
                Email = session.GetString("UserEmail") ?? string.Empty,
                Role = session.GetString("UserRole") ?? "customer"
            };
        }

        public void ClearSession()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            session?.Clear();
        }

        public bool IsAuthenticated()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            return session?.GetInt32("UserId") != null;
        }

        public bool IsAdmin()
        {
            var user = GetUserSession();
            return user?.Role == "admin";
        }

        public bool IsOrganizer()
        {
            var user = GetUserSession();
            return user?.Role == "organizer";
        }

        public bool IsCustomer()
        {
            var user = GetUserSession();
            return user?.Role == "customer";
        }
    }
}