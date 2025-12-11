using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Models;
using Microsoft.AspNetCore.Http;

namespace EventManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId != null)
            {
                // Redirect to appropriate dashboard based on role
                return userRole switch
                {
                    "admin" => RedirectToAction("Dashboard", "Admin"),
                    "organizer" => RedirectToAction("Dashboard", "Organizer"),
                    _ => RedirectToAction("Dashboard", "Customer")
                };
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel());
        }
    }
}