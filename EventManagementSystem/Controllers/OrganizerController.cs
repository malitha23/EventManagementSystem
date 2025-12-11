using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Services;

namespace EventManagementSystem.Controllers
{
    public class OrganizerController : Controller
    {
        private readonly ISessionManager _sessionManager;

        public OrganizerController(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public IActionResult Dashboard()
        {
            if (!_sessionManager.IsAuthenticated() || !_sessionManager.IsOrganizer())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }
    }
}