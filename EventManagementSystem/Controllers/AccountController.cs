using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Models;
using EventManagementSystem.Services;

namespace EventManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ISessionManager _sessionManager;

        public AccountController(IAuthService authService, ISessionManager sessionManager)
        {
            _authService = authService;
            _sessionManager = sessionManager;
        }

        // GET: /Account/Register
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Register()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId != null)
            {
                return userRole switch
                {
                    "admin" => RedirectToAction("Dashboard", "Admin"),
                    "organizer" => RedirectToAction("Dashboard", "Organizer"),
                    _ => RedirectToAction("Dashboard", "Customer")
                };
            }

            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _authService.RegisterAsync(model);
            if (user == null)
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            _sessionManager.SetUserSession(user);
            return RedirectToAction("Dashboard", user.Role);
        }

        // GET: /Account/Login
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Login()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId != null)
            {
                return userRole switch
                {
                    "admin" => RedirectToAction("Dashboard", "Admin"),
                    "organizer" => RedirectToAction("Dashboard", "Organizer"),
                    _ => RedirectToAction("Dashboard", "Customer")
                };
            }

            return View();
        }


        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _authService.LoginAsync(model.Email, model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                return View(model);
            }

            _sessionManager.SetUserSession(user);

            if (model.RememberMe)
            {
                Response.Cookies.Append("UserEmail", user.Email, new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(30),
                    HttpOnly = true
                });
            }

            return RedirectToAction("Dashboard", user.Role);
        }

        // POST: /Account/Logout
        [HttpPost]
        public IActionResult Logout()
        {
            _sessionManager.ClearSession();
            Response.Cookies.Delete("UserEmail");
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}