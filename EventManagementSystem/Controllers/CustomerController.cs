using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Services;
using EventManagementSystem.Models;

namespace EventManagementSystem.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISessionManager _sessionManager;

        public CustomerController(ApplicationDbContext dbContext, ISessionManager sessionManager)
        {
            _dbContext = dbContext;
            _sessionManager = sessionManager;
        }

        public async Task<IActionResult> Dashboard(string search, string category, string location,
            decimal? minPrice, decimal? maxPrice, string sort = "date_asc")
        {
            if (!_sessionManager.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            if (!_sessionManager.IsCustomer())
                return RedirectToAction("AccessDenied", "Account");

            var categories = await _dbContext.EventCategories.ToListAsync();
            var venues = await _dbContext.Venues.ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Venues = venues;

            // Store current filter values for view
            ViewBag.SearchQuery = search;
            ViewBag.CategoryQuery = category;
            ViewBag.LocationQuery = location;
            ViewBag.MinPriceQuery = minPrice;
            ViewBag.MaxPriceQuery = maxPrice;
            ViewBag.SortQuery = sort;

            // Base query with related data
            var eventsQuery = _dbContext.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.EventImages)
                .Include(e => e.Bookings)
                .Where(e => e.Status == "upcoming" && e.EventDate >= DateTime.Today)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(search))
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.Title.Contains(search) ||
                    e.Description.Contains(search) ||
                    e.Venue.Location.Contains(search));
            }

            if (!string.IsNullOrEmpty(category) && category != "all")
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.Category.Name.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrEmpty(location) && location != "all")
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.Venue.Location.ToLower().Contains(location.ToLower()));
            }

            if (minPrice.HasValue)
                eventsQuery = eventsQuery.Where(e => e.TicketPrice >= minPrice.Value);

            if (maxPrice.HasValue)
                eventsQuery = eventsQuery.Where(e => e.TicketPrice <= maxPrice.Value);

            // Sorting
            eventsQuery = sort switch
            {
                "date_desc" => eventsQuery.OrderByDescending(e => e.EventDate),
                "price_asc" => eventsQuery.OrderBy(e => e.TicketPrice),
                "price_desc" => eventsQuery.OrderByDescending(e => e.TicketPrice),
                _ => eventsQuery.OrderBy(e => e.EventDate), // Default: date_asc
            };

            var events = await eventsQuery.ToListAsync();

            ViewBag.EventsCount = events.Count;
            ViewBag.UserName = _sessionManager.GetUserName();

            return View(events);
        }

        // API endpoint for getting event details
        [HttpGet]
        public async Task<IActionResult> GetEventDetails(int id)
        {
            var eventDetails = await _dbContext.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.EventImages)
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventDetails == null)
                return NotFound();

            var bookedTickets = eventDetails.Bookings?.Sum(b => b.NumberOfTickets) ?? 0;
            var availableTickets = eventDetails.TotalCapacity - bookedTickets;

            var result = new
            {
                id = eventDetails.Id,
                title = eventDetails.Title,
                description = eventDetails.Description,
                eventDate = eventDetails.EventDate.ToString("yyyy-MM-dd"),
                startTime = eventDetails.StartTime.ToString(@"hh\:mm"),
                endTime = eventDetails.EndTime.ToString(@"hh\:mm"),
                location = eventDetails.Venue?.Location,
                venueName = eventDetails.Venue?.Name,
                category = eventDetails.Category?.Name,
                ticketPrice = eventDetails.TicketPrice,
                totalCapacity = eventDetails.TotalCapacity,
                availableTickets = availableTickets,
                images = eventDetails.EventImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>()
            };

            return Json(result);
        }

        // Book Event Page
        public async Task<IActionResult> BookEvent(int id)
        {
            if (!_sessionManager.IsAuthenticated() || !_sessionManager.IsCustomer())
                return RedirectToAction("Login", "Account");

            var eventDetails = await _dbContext.Events
                .Include(e => e.Venue)
                .Include(e => e.Category)
                .Include(e => e.EventImages)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventDetails == null)
            {
                TempData["ErrorMessage"] = "Event not found!";
                return RedirectToAction("Dashboard");
            }

            ViewBag.UserName = _sessionManager.GetUserName();
            return View(eventDetails);
        }

        // Clear filters action
        public IActionResult ClearFilters()
        {
            return RedirectToAction("Dashboard");
        }
    }
}