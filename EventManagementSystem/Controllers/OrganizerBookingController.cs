using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Data;
using EventManagementSystem.Models;
using EventManagementSystem.Services;

namespace EventManagementSystem.Controllers
{
    public class OrganizerBookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISessionManager _sessionManager;

        public OrganizerBookingController(ApplicationDbContext context, ISessionManager sessionManager)
        {
            _context = context;
            _sessionManager = sessionManager;
        }

        // GET: OrganizerBooking
        public async Task<IActionResult> Index()
        {
            var user = _sessionManager.GetUserSession();
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { ReturnUrl = Url.Action("Index", "OrganizerBooking") });
            }

            // Fetch bookings for events created by this organizer
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .Include(b => b.Tickets)
                .Where(b => b.Event != null && b.Event.OrganizerId == user.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // POST: OrganizerBooking/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (status != "Confirmed" && status != "Canceled")
                return BadRequest("Invalid status.");


            // Optionally: update tickets as well
            if (status == "Canceled")
            {
                booking.BookingStatus = "cancelled";
                foreach (var ticket in await _context.Tickets.Where(t => t.BookingId == booking.Id).ToListAsync())
                {
                    ticket.Status = "cancelled";
                    ticket.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (status == "Confirmed")
            {
                booking.BookingStatus = "confirmed";
                foreach (var ticket in await _context.Tickets.Where(t => t.BookingId == booking.Id).ToListAsync())
                {
                    ticket.Status = "valid";
                    ticket.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: OrganizerBooking/EventBookings/5
        public async Task<IActionResult> EventBookings(int id) // id = EventId
        {
            var user = _sessionManager.GetUserSession();
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { ReturnUrl = Url.Action("EventBookings", "OrganizerBooking", new { id }) });
            }

            // Verify that the event belongs to this organizer
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == user.Id);

            if (ev == null) return NotFound();

            // Get bookings for this event
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Tickets)
                .Where(b => b.EventId == id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.EventTitle = ev.Title;

            return View(bookings);
        }

        // POST: OrganizerBooking/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateByEventBookingStatus(int id, string status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (status != "Confirmed" && status != "Canceled")
                return BadRequest("Invalid status.");


            // Optionally: update tickets as well
            if (status == "Canceled")
            {
                booking.BookingStatus = "cancelled";
                foreach (var ticket in await _context.Tickets.Where(t => t.BookingId == booking.Id).ToListAsync())
                {
                    ticket.Status = "cancelled";
                    ticket.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (status == "Confirmed")
            {
                booking.BookingStatus = "confirmed";
                foreach (var ticket in await _context.Tickets.Where(t => t.BookingId == booking.Id).ToListAsync())
                {
                    ticket.Status = "valid";
                    ticket.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            // Example: Redirect to EventBookings for event with id = ev.Id
            return RedirectToAction("EventBookings", "OrganizerBooking", new { id = booking.EventId });

        }


    }


}
