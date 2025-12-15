using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Services;
using EventManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Data;

namespace EventManagementSystem.Controllers
{
    public class OrganizerController : Controller
    {
        private readonly ISessionManager _sessionManager;
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<OrganizerController> _logger;
        private readonly ApplicationDbContext _context;


        public OrganizerController(
            ISessionManager sessionManager,
            IDashboardService dashboardService,
            ILogger<OrganizerController> logger,
            ApplicationDbContext context)
        {
            _sessionManager = sessionManager;
            _dashboardService = dashboardService;
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!_sessionManager.IsAuthenticated() || !_sessionManager.IsOrganizer())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            try
            {
                var organizer = _sessionManager.GetUserSession();
                Console.WriteLine($"Organizer ID: {organizer?.Id}");
                var viewModel = await _dashboardService.GetOrganizerDashboardData(organizer!.Id);

                viewModel.NewEvent = new EventCreateViewModel();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading organizer dashboard");

                // Return fully initialized fallback to prevent null reference
                return View(new OrganizerDashboardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var organizer = _sessionManager.GetUserSession();
                var stats = await _dashboardService.GetDashboardStats(organizer!.Id);
                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return Json(new { error = "Unable to load statistics" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyRevenueChart(int months = 6)
        {
            try
            {
                var organizer = _sessionManager.GetUserSession();

                var startDate = DateTime.Now.AddMonths(-months);

                var monthlyRevenues = await _context.Bookings
                    .Include(b => b.Event)
                    .Where(b => b.Event.OrganizerId == organizer!.Id &&
                               b.PaymentStatus == "paid" &&
                               b.CreatedAt >= startDate)
                    .GroupBy(b => new { Year = b.CreatedAt.Year, Month = b.CreatedAt.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(b => b.FinalAmount)
                    })
                    .OrderBy(g => g.Year)
                    .ThenBy(g => g.Month)
                    .ToListAsync();

                // Create complete list for all months in the period
                var result = new List<object>();
                var currentDate = startDate;

                while (currentDate <= DateTime.Now)
                {
                    var monthYear = currentDate.ToString("MMM yyyy");
                    var revenue = monthlyRevenues
                        .FirstOrDefault(m => m.Year == currentDate.Year && m.Month == currentDate.Month)?
                        .Revenue ?? 0;

                    result.Add(new
                    {
                        month = monthYear,
                        revenue = revenue
                    });

                    currentDate = currentDate.AddMonths(1);
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly revenue chart");
                return Json(new { error = "Unable to load chart data" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentBookings()
        {
            try
            {
                var organizer = _sessionManager.GetUserSession();

                var recentBookings = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Event)
                    .Where(b => b.Event.OrganizerId == organizer!.Id)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(10)
                    .Select(b => new
                    {
                        id = b.Id,
                        eventTitle = b.Event.Title,
                        customerName = b.Customer.Name,
                        numberOfTickets = b.NumberOfTickets,
                        finalAmount = b.FinalAmount,
                        paymentStatus = b.PaymentStatus,
                        createdAt = b.CreatedAt.ToString("MMM dd, HH:mm")
                    })
                    .ToListAsync();

                return Json(recentBookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent bookings");
                return Json(new { error = "Unable to load recent bookings" });
            }
        }
    }
}
