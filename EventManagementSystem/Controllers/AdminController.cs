using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Services;

namespace EventManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ISessionManager _sessionManager;

        public AdminController(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public IActionResult Dashboard()
        {
            if (!_sessionManager.IsAuthenticated() || !_sessionManager.IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }
    }
}