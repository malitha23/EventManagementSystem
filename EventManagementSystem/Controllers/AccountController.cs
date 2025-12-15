using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Models;
using EventManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Data;
using EventManagementSystem.Models.ViewModels;

namespace EventManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ISessionManager _sessionManager;
        private readonly ApplicationDbContext _dbContext;

        public AccountController(IAuthService authService, ISessionManager sessionManager, ApplicationDbContext dbContext)
        {
            _authService = authService;
            _sessionManager = sessionManager;
            _dbContext = dbContext;
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


        // GET: /Account/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null)
                return RedirectToAction("Login");

            if (userRole == "customer")
            {
                return await LoadCustomerProfileView(userId.Value);
            }
            else if (userRole == "organizer")
            {
                return await LoadOrganizerProfileView(userId.Value);
            }
            else if (userRole == "admin")
            {
                return await LoadAdminProfileView(userId.Value);
            }

            return RedirectToAction("Login");
        }

        // Helper method for customer profile
        private async Task<IActionResult> LoadCustomerProfileView(int userId, ProfileViewModel? existingModel = null)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            // Load loyalty points
            var loyaltyPoints = await _dbContext.Set<LoyaltyPoint>()
                .AsNoTracking()
                .FirstOrDefaultAsync(lp => lp.CustomerId == userId);

            var loyaltyHistory = await _dbContext.Set<LoyaltyHistory>()
                .AsNoTracking()
                .Where(lh => lh.CustomerId == userId)
                .OrderByDescending(lh => lh.CreatedAt)
                .ToListAsync();

            // Load bookings
            var bookings = await _dbContext.Set<Booking>()
                .AsNoTracking()
                .Include(b => b.Event)
                    .ThenInclude(e => e.EventImages)
                .Include(b => b.Tickets)
                .Where(b => b.CustomerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            // Load payments
            var payments = await _dbContext.Set<Payment>()
                .AsNoTracking()
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Event)
                .Where(p => p.Booking.CustomerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var vm = existingModel ?? new ProfileViewModel();
            vm.User = user;
            vm.LoyaltyPoints = loyaltyPoints?.Points ?? 0;
            vm.LoyaltyHistory = loyaltyHistory;
            vm.Bookings = bookings;
            vm.Payments = payments;
            vm.UserRole = "customer"; // Set role for view

            // Clear password field when reloading
            vm.User.Password = null;

            return View("Profile", vm);
        }

        // Helper method for organizer profile
        private async Task<IActionResult> LoadOrganizerProfileView(int userId)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            // Load organizer events
            var events = await _dbContext.Events
                .AsNoTracking()
                .Include(e => e.EventImages)
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Where(e => e.OrganizerId == userId)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            // Load organizer statistics
            var totalEvents = events.Count;
            var activeEvents = events.Count(e => e.Status == "upcoming" && e.EventDate >= DateTime.Today);
            var pastEvents = events.Count(e => e.EventDate < DateTime.Today);

            // Load bookings for revenue calculation
            var eventIds = events.Select(e => e.Id).ToList();
            var bookings = await _dbContext.Bookings
                .AsNoTracking()
                .Where(b => eventIds.Contains(b.EventId))
                .ToListAsync();

            var totalRevenue = bookings.Where(b => b.PaymentStatus == "paid").Sum(b => b.FinalAmount);
            var totalTicketsSold = bookings.Sum(b => b.NumberOfTickets);
            var totalBookings = bookings.Count;

            var vm = new ProfileViewModel
            {
                User = user,
                UserRole = "organizer",
                OrganizerEvents = events,
                OrganizerStats = new OrganizerProfileStats
                {
                    TotalEvents = totalEvents,
                    ActiveEvents = activeEvents,
                    PastEvents = pastEvents,
                    TotalRevenue = totalRevenue,
                    TotalTicketsSold = totalTicketsSold,
                    TotalBookings = totalBookings
                }
            };

            return View("Profile", vm);
        }

        // Helper method for admin profile (if needed)
        private async Task<IActionResult> LoadAdminProfileView(int userId)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            var vm = new ProfileViewModel
            {
                User = user,
                UserRole = "admin"
            };

            return View("Profile", vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model, IFormFile? profileImageFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            

            try
            {
                // Update only the specific fields - don't assign the entire model.User object
                user.Name = model.User.Name;
                user.Email = model.User.Email;
                user.Phone = model.User.Phone;

                // Handle password update
                if (!string.IsNullOrEmpty(model.User.Password))
                {
                    // Don't save plain text - hash the password
                   // user.Password = BCrypt.Net.BCrypt.HashPassword(model.User.Password);
                }

                // Handle profile image upload
                if (profileImageFile != null && profileImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(user.ProfileImage))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfileImage.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try { System.IO.File.Delete(oldFilePath); } catch { }
                        }
                    }

                    // Generate unique filename
                    var uniqueFileName = $"{user.Id}_{DateTime.Now.Ticks}{Path.GetExtension(profileImageFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImageFile.CopyToAsync(stream);
                    }

                    user.ProfileImage = $"/images/profiles/{uniqueFileName}";
                }

                // Save changes
                await _dbContext.SaveChangesAsync();

                // Update session
                if (_sessionManager != null)
                {
                    _sessionManager.SetUserSession(user);
                }

                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                ModelState.AddModelError("", "The record you attempted to edit was modified by another user. Please try again.");
                return await LoadProfileView(userId.Value, model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating profile: {ex.Message}");
                return await LoadProfileView(userId.Value, model);
            }
        }


        // Helper method to load profile data
        private async Task<IActionResult> LoadProfileView(int userId, ProfileViewModel? existingModel = null)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            // Load loyalty points
            var loyaltyPoints = await _dbContext.Set<LoyaltyPoint>()
                .AsNoTracking()
                .FirstOrDefaultAsync(lp => lp.CustomerId == userId);

            var loyaltyHistory = await _dbContext.Set<LoyaltyHistory>()
                .AsNoTracking()
                .Where(lh => lh.CustomerId == userId)
                .OrderByDescending(lh => lh.CreatedAt)
                .ToListAsync();

            // Load bookings
            var bookings = await _dbContext.Set<Booking>()
                .AsNoTracking()
                .Include(b => b.Event)
                    .ThenInclude(e => e.EventImages)
                .Include(b => b.Tickets)
                .Where(b => b.CustomerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            // Load payments
            var payments = await _dbContext.Set<Payment>()
                .AsNoTracking()
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Event)
                .Where(p => p.Booking.CustomerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var vm = existingModel ?? new ProfileViewModel();
            vm.User = user;
            vm.LoyaltyPoints = loyaltyPoints?.Points ?? 0;
            vm.LoyaltyHistory = loyaltyHistory;
            vm.Bookings = bookings;
            vm.Payments = payments;

            // Clear password field when reloading
            vm.User.Password = null;

            return View(vm);
        }



    }
}