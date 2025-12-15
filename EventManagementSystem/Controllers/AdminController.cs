using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Services;
using EventManagementSystem.Data;
using EventManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Utilities;

namespace EventManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ISessionManager _sessionManager;
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IEventService _eventService;
        private readonly IVenueService _venueService;
        private readonly IReportService _reportService;
        private readonly IEventCategoryService _categoryService;
        private readonly IPromotionService _promotionService;

        public AdminController(
    ISessionManager sessionManager,
    IDashboardService dashboardService,
    ILogger<AdminController> logger,
    IUserService userService,
    ApplicationDbContext context,
    IEventService eventService,
    IVenueService venueService,
    IReportService reportService,
    IEventCategoryService categoryService,
    IPromotionService promotionService)
        {
            _sessionManager = sessionManager;
            _dashboardService = dashboardService;
            _logger = logger;
            _context = context;
            _userService = userService;
            _eventService = eventService;
            _venueService = venueService;
            _reportService = reportService;
            _categoryService = categoryService;
            _promotionService = promotionService;
        }


        public async Task<IActionResult> Dashboard()
        {
            if (!_sessionManager.IsAuthenticated() || !_sessionManager.IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            try
            {
                var user = _sessionManager.GetUserSession();
                Console.WriteLine($"Admin ID: {user?.Id}");

                var viewModel = await _dashboardService.GetAdminDashboardData()
                ?? new AdminDashboardViewModel();

                return View(viewModel);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");

                // Return empty admin dashboard view model
                return View(new AdminDashboardViewModel());
            }
        }

        // Additional admin actions you might want
        public async Task<IActionResult> Users()
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> Organizers()
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var organizers = await _context.Users
                .Where(u => u.Role == "organizer")
                .ToListAsync();

            return View(organizers);
        }

       


        [HttpGet]
        public async Task<IActionResult> Users(int page = 1, int pageSize = 10,
            string search = "", string role = "", string sortBy = "CreatedAt",
            string sortOrder = "desc", bool? isActive = null)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var filter = new UserFilterModel
            {
                SearchTerm = search,
                Role = role,
                SortBy = sortBy,
                SortOrder = sortOrder,
                IsActive = isActive
            };

            var viewModel = await _userService.GetUsers(filter, page, pageSize);

            // Add roles for filter dropdown
            ViewBag.Roles = await _userService.GetAllRoles();

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            ViewBag.Roles = new List<string> { "admin", "organizer", "customer" };
            return View(new UserCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(UserCreateViewModel model)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                var success = await _userService.CreateUser(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "User created successfully!";
                    return RedirectToAction("Users");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create user. Email may already exist.";
                }
            }

            ViewBag.Roles = new List<string> { "admin", "organizer", "customer" };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var user = await _userService.GetUserById(id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Users");
            }

            var viewModel = new UserEditViewModel
            {
                Id = user.Id.ToString(),
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive
            };

            ViewBag.Roles = new List<string> { "admin", "organizer", "customer" };
            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(UserEditViewModel model)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ModelState.Remove(nameof(model.NewPassword));
                ModelState.Remove(nameof(model.ConfirmPassword));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new List<string> { "admin", "organizer", "customer" };
                return View(model);
            }

            Console.WriteLine($"Id: {model.Id}");
            Console.WriteLine($"Name: {model.Name}");
            Console.WriteLine($"Email: {model.Email}");
            Console.WriteLine($"Phone: {model.Phone}");
            Console.WriteLine($"Role: {model.Role}");
            Console.WriteLine($"IsActive: {model.IsActive}");
            Console.WriteLine($"NewPassword: {model.NewPassword}");
            Console.WriteLine($"ConfirmPassword: {model.ConfirmPassword}");

            _logger.LogInformation("Editing user {@UserModel}", model);


            var success = await _userService.UpdateUser(model);
            if (success)
            {
                TempData["SuccessMessage"] = "User updated successfully!";
                return RedirectToAction("Users");
            }

            TempData["ErrorMessage"] = "Failed to update user.";
            ViewBag.Roles = new List<string> { "admin", "organizer", "customer" };
            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (!_sessionManager.IsAdmin())
            {
                TempData["ErrorMessage"] = "Unauthorized";
                return RedirectToAction("Users", "Admin");
            }

            var success = await _userService.DeleteUser(id);
            if (success)
            {
                TempData["SuccessMessage"] = "User deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user.";
            }

            // Redirect to Users view
            return RedirectToAction("Users", "Admin");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            if (!_sessionManager.IsAdmin())
                return Json(new { success = false, message = "Unauthorized" });

            var success = await _userService.ToggleUserStatus(id);
            if (success)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "Failed to update user status." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> UserDetails(string id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var user = await _userService.GetUserViewModel(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Users");
            }

            // Get user's recent bookings
            var recentBookings = await _context.Bookings
                .Include(b => b.Event)
                .Where(b => b.CustomerId.ToString() == id)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new BookingSummary
                {
                    Id = b.Id,
                    EventTitle = b.Event.Title,
                    CustomerName = user.Name,
                    NumberOfTickets = b.NumberOfTickets,
                    FinalAmount = b.FinalAmount,
                    PaymentStatus = b.PaymentStatus,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            // Get organizer's events if applicable
            List<EventSummary> organizerEvents = new();
            if (user.Role == "organizer")
            {
                organizerEvents = await _context.Events
                    .Where(e => e.OrganizerId.ToString() == id)
                    .Select(e => new EventSummary
                    {
                        Id = e.Id,
                        Title = e.Title,
                        EventDate = e.EventDate,
                        TotalCapacity = e.TotalCapacity,
                        TicketsSold = _context.Bookings
                            .Where(b => b.EventId == e.Id)
                            .Sum(b => (int?)b.NumberOfTickets) ?? 0,
                        Revenue = _context.Bookings
                            .Where(b => b.EventId == e.Id && b.PaymentStatus == "paid")
                            .Sum(b => (decimal?)b.FinalAmount) ?? 0
                    })
                    .Take(10)
                    .ToListAsync();
            }

            ViewBag.RecentBookings = recentBookings;
            ViewBag.OrganizerEvents = organizerEvents;

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> ExportUsers(string format = "csv")
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var users = await _context.Users
                .Select(u => new UserViewModel
                {
                    Name = u.Name,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive,
                    TotalBookings = _context.Bookings.Count(b => b.CustomerId == u.Id),
                    TotalSpent = _context.Bookings
                        .Where(b => b.CustomerId == u.Id && b.PaymentStatus == "paid")
                        .Sum(b => (decimal?)b.FinalAmount) ?? 0
                })
                .ToListAsync();

            if (format.ToLower() == "csv")
            {
                var csv = "Name,Email,Phone,Role,CreatedAt,IsActive,TotalBookings,TotalSpent\n";
                csv += string.Join("\n", users.Select(u =>
                    $"\"{u.Name}\",\"{u.Email}\",\"{u.Phone}\",{u.Role},{u.CreatedAt:yyyy-MM-dd},{u.IsActive},{u.TotalBookings},{u.TotalSpent}"));

                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"users_{DateTime.Now:yyyyMMddHHmmss}.csv");
            }

            TempData["ErrorMessage"] = "Invalid export format.";
            return RedirectToAction("Users");
        }





        // Add these methods to AdminController.cs
        [HttpGet]
        public async Task<IActionResult> Events(int page = 1, int pageSize = 10,
            string search = "", string status = "", string category = "",
            string organizer = "", DateTime? fromDate = null, DateTime? toDate = null,
            decimal? minPrice = null, decimal? maxPrice = null,
            string sortBy = "EventDate", string sortOrder = "asc")
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var filter = new EventFilterModel
            {
                SearchTerm = search,
                Status = status,
                Category = category,
                Organizer = organizer,
                FromDate = fromDate,
                ToDate = toDate,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            var viewModel = await _eventService.GetAdminEvents(filter, page, pageSize);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (!_sessionManager.IsAdmin())
                return Json(new { success = false, message = "Unauthorized" });

            var success = await _eventService.DeleteEvent(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Event deleted successfully!";
                return Json(new { success = true });
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete event.";
                return Json(new { success = false, message = "Failed to delete event." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEventStatus(int id)
        {
            if (!_sessionManager.IsAdmin())
                return Json(new { success = false, message = "Unauthorized" });

            var success = await _eventService.ToggleEventStatus(id);
            if (success)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "Failed to update event status." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EventDetails(int id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var eventItem = await _eventService.GetEventById(id);
            if (eventItem == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("Events");
            }

            // Get event statistics
            var bookings = await _context.Bookings
                .Where(b => b.EventId == id)
                .ToListAsync();

            var ticketsSold = bookings.Sum(b => b.NumberOfTickets);
            var revenue = bookings.Where(b => b.PaymentStatus == "paid").Sum(b => b.FinalAmount);
            var occupancyRate = eventItem.TotalCapacity > 0 ?
                (ticketsSold * 100m) / eventItem.TotalCapacity : 0;

            // Get recent bookings for this event
            var recentBookings = await _context.Bookings
                .Include(b => b.Customer)
                .Where(b => b.EventId == id)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new BookingSummary
                {
                    Id = b.Id,
                    CustomerName = b.Customer.Name,
                    NumberOfTickets = b.NumberOfTickets,
                    FinalAmount = b.FinalAmount,
                    PaymentStatus = b.PaymentStatus,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            ViewBag.TicketsSold = ticketsSold;
            ViewBag.Revenue = revenue;
            ViewBag.OccupancyRate = occupancyRate;
            ViewBag.RecentBookings = recentBookings;
            ViewBag.TotalBookings = bookings.Count;

            return View(eventItem);
        }


        // Add these methods to AdminController.cs
        [HttpGet]
        public async Task<IActionResult> Venues(
            int page = 1,
            int pageSize = 10,
            string search = "",
            string location = "",
            int? minCapacity = null,
            int? maxCapacity = null,
            bool? isActive = null,
            string sortBy = "Name",
            string sortOrder = "asc")
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            try
            {
                var filter = new VenueFilterModel
                {
                    SearchTerm = search,
                    Location = location,
                    MinCapacity = minCapacity,
                    MaxCapacity = maxCapacity,
                    IsActive = isActive,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };

                var viewModel = await _venueService.GetAdminVenues(filter, page, pageSize);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading venues in admin panel");
                TempData["ErrorMessage"] = "An error occurred while loading venues.";

                return View(new VenuesViewModel
                {
                    Venues = new List<VenueViewModel>(),
                    Filter = new VenueFilterModel(),
                    Locations = new List<string>()
                });
            }
        }

        [HttpGet]
        public IActionResult CreateVenue()
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            return View(new VenueCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVenue(VenueCreateViewModel model)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                var success = await _venueService.CreateVenue(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Venue created successfully!";
                    return RedirectToAction("Venues");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create venue.";
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditVenue(int id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var venue = await _venueService.GetVenueById(id);
            if (venue == null)
            {
                TempData["ErrorMessage"] = "Venue not found.";
                return RedirectToAction("Venues");
            }

            var viewModel = new VenueEditViewModel
            {
                Id = venue.Id,
                Name = venue.Name,
                Location = venue.Location,
                Description = venue.Description,
                Capacity = venue.Capacity
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVenue(VenueEditViewModel model)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                var success = await _venueService.UpdateVenue(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Venue updated successfully!";
                    return RedirectToAction("Venues");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update venue.";
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var success = await _venueService.DeleteVenue(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Venue deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Cannot delete venue that has events.";
            }

            return RedirectToAction("Venues");
        }

        [HttpGet]
        public async Task<IActionResult> VenueDetails(int id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var viewModel = await _venueService.GetVenueDetails(id);
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Venue not found.";
                return RedirectToAction("Venues");
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVenueStatus(int id)
        {
            if (!_sessionManager.IsAdmin())
                return Json(new { success = false, message = "Unauthorized" });

            var success = await _venueService.ToggleVenueStatus(id);
            if (success)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "Failed to update venue status." });
            }
        }


        // Add these methods to AdminController.cs
        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            try
            {
                var filter = new ReportFilterModel
                {
                    StartDate = DateTime.Now.AddMonths(-1),
                    EndDate = DateTime.Now
                };

                var viewModel = await _reportService.GenerateReport(filter);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports");
                TempData["ErrorMessage"] = "An error occurred while generating reports.";

                return View(new ReportsViewModel());
            }
        }


        private async Task<List<string>> GetCategories()
        {
            try
            {
                return await _context.EventCategories
                    .Select(c => c.Name)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<List<string>> GetOrganizers()
        {
            try
            {
                return await _context.Users
                    .Where(u => u.Role == "organizer")
                    .Select(u => u.Name)
                    .Distinct()
                    .OrderBy(o => o)
                    .ToListAsync();
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<List<string>> GetStatusOptions()
        {
            try
            {
                return await _context.Events
                    .Select(e => e.Status)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();
            }
            catch
            {
                return new List<string>();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reports(ReportsViewModel model)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (!ModelState.IsValid)
            {
                model.ReportTypes = await _reportService.GetReportTypes();
                model.Categories = await GetCategories();
                model.Organizers = await GetOrganizers();
                model.StatusOptions = await GetStatusOptions();
                return View(model);
            }

            var viewModel = await _reportService.GenerateReport(model.Filter);

            viewModel.ReportTypes = await _reportService.GetReportTypes();
            viewModel.Categories = await GetCategories();
            viewModel.Organizers = await GetOrganizers();
            viewModel.StatusOptions = await GetStatusOptions();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportReport(ReportFilterModel filter)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            try
            {
                var format = filter.ExportFormat?.ToLower() ?? "csv";
                var fileBytes = await _reportService.ExportReport(filter, format);

                if (format == "csv")
                {
                    return File(fileBytes, "application/vnd.ms-excel", $"report_{DateTime.Now:yyyyMMddHHmmss}.csv");
                }
                if (format == "pdf")
                {
                    return File(fileBytes, "application/pdf", $"report_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                }

                // HTML download
                return File(fileBytes, "application/octet-stream", $"report_{DateTime.Now:yyyyMMddHHmmss}.html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report");
                TempData["ErrorMessage"] = "An error occurred while exporting the report.";
                return RedirectToAction("Reports");
            }
        }



        // Add these methods to AdminController.cs
        [HttpGet]
        public async Task<IActionResult> EventCategories(
            int page = 1,
            int pageSize = 10,
            string search = "",
            string sortBy = "Name",
            string sortOrder = "asc")
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            try
            {
                var filter = new CategoryFilterModel
                {
                    SearchTerm = search,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };

                var viewModel = await _categoryService.GetCategories(filter, page, pageSize);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event categories");
                TempData["ErrorMessage"] = "An error occurred while loading categories.";

                return View(new EventCategoriesViewModel
                {
                    Categories = new List<EventCategoryViewModel>(),
                    Filter = new CategoryFilterModel()
                });
            }
        }

        [HttpGet]
        public IActionResult CreateCategory()
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            return View(new EventCategoryCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(EventCategoryCreateViewModel model)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                var success = await _categoryService.CreateCategory(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Category created successfully!";
                    return RedirectToAction("EventCategories");
                }
                else
                {
                    TempData["ErrorMessage"] = "Category with this name already exists.";
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var category = await _categoryService.GetCategoryById(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction("EventCategories");
            }

            var viewModel = new EventCategoryEditViewModel
            {
                Id = category.Id,
                Name = category.Name
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(EventCategoryEditViewModel model)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                var success = await _categoryService.UpdateCategory(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Category updated successfully!";
                    return RedirectToAction("EventCategories");
                }
                else
                {
                    TempData["ErrorMessage"] = "Category with this name already exists.";
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var success = await _categoryService.DeleteCategory(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Category deleted successfully!";
            }
            else
            {
                // Check if category has events
                var hasEvents = await _categoryService.CategoryHasEvents(id);
                TempData["ErrorMessage"] = hasEvents ?
                    "Cannot delete category that has events. Please reassign events first." :
                    "Failed to delete category.";
            }

            return RedirectToAction("EventCategories");
        }

        [HttpGet]
        public async Task<IActionResult> CategoryDetails(int id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var viewModel = await _categoryService.GetCategoryDetails(id);
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToAction("EventCategories");
            }

            return View(viewModel);
        }



        // Add these methods to AdminController.cs
        [HttpGet]
        public async Task<IActionResult> Promotions(
            int page = 1,
            int pageSize = 10,
            string search = "",
            string discountType = "",
            string status = "",
            DateTime? startDate = null,
            DateTime? endDate = null,
            string sortBy = "StartDate",
            string sortOrder = "desc")
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            try
            {
                var filter = new PromotionFilterModel
                {
                    SearchTerm = search,
                    DiscountType = discountType,
                    Status = status,
                    StartDate = startDate,
                    EndDate = endDate,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };

                var viewModel = await _promotionService.GetPromotions(filter, page, pageSize);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading promotions");
                TempData["ErrorMessage"] = "An error occurred while loading promotions.";

                return View(new PromotionsViewModel
                {
                    Promotions = new List<PromotionViewModel>(),
                    Filter = new PromotionFilterModel(),
                    DiscountTypes = new List<string> { "percentage", "fixed" },
                    StatusOptions = new List<string> { "active", "inactive", "expired" }
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreatePromotion()
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            ViewBag.DiscountTypes = await _promotionService.GetDiscountTypes();
            return View(new PromotionCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromotion(PromotionCreateViewModel model)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            // Check if code is unique
            if (!await _promotionService.IsPromotionCodeUnique(model.Code))
            {
                ModelState.AddModelError("Code", "Promotion code already exists.");
            }

            if (ModelState.IsValid)
            {
                var success = await _promotionService.CreatePromotion(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Promotion created successfully!";
                    return RedirectToAction("Promotions");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create promotion.";
                }
            }

            ViewBag.DiscountTypes = await _promotionService.GetDiscountTypes();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditPromotion(int id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var promotion = await _promotionService.GetPromotionById(id);
            if (promotion == null)
            {
                TempData["ErrorMessage"] = "Promotion not found.";
                return RedirectToAction("Promotions");
            }

            var viewModel = new PromotionEditViewModel
            {
                Id = promotion.Id,
                Code = promotion.Code,
                DiscountType = promotion.DiscountType,
                DiscountValue = promotion.DiscountValue,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                Status = promotion.Status
            };

            ViewBag.DiscountTypes = await _promotionService.GetDiscountTypes();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPromotion(PromotionEditViewModel model)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            // Check if code is unique (excluding current promotion)
            if (!await _promotionService.IsPromotionCodeUnique(model.Code, model.Id))
            {
                ModelState.AddModelError("Code", "Promotion code already exists.");
            }

            if (ModelState.IsValid)
            {
                var success = await _promotionService.UpdatePromotion(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Promotion updated successfully!";
                    return RedirectToAction("Promotions");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update promotion.";
                }
            }

            ViewBag.DiscountTypes = await _promotionService.GetDiscountTypes();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var success = await _promotionService.DeletePromotion(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Promotion deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete promotion.";
            }

            return RedirectToAction("Promotions");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePromotionStatus(int id)
        {
            if (!_sessionManager.IsAdmin())
                return Json(new { success = false, message = "Unauthorized" });

            var success = await _promotionService.TogglePromotionStatus(id);
            if (success)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "Failed to update promotion status." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PromotionDetails(int id)
        {
            if (!_sessionManager.IsAdmin())
                return RedirectToAction("AccessDenied", "Account");

            var viewModel = await _promotionService.GetPromotionDetails(id);
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Promotion not found.";
                return RedirectToAction("Promotions");
            }

            return View(viewModel);
        }
    }
}