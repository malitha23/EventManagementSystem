using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Data;
using EventManagementSystem.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using EventManagementSystem.Services;
using QRCoder;
using System.Drawing.Imaging;

namespace EventManagementSystem.Controllers
{
    //[Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ISessionManager _sessionManager;

        public BookingController(ApplicationDbContext db, ISessionManager sessionManager)
        {
            _db = db;
            _sessionManager = sessionManager;
        }


        // ==============================
        //  SHOW BOOKING PAGE
        // ==============================
        public async Task<IActionResult> BookEvent(int id)
        {
            var user = _sessionManager.GetUserSession();
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { ReturnUrl = Url.Action("BookEvent", new { id }) });
            }

            var ev = await _db.Events
                .Include(e => e.Venue)
                .Include(e => e.Category)
                .Include(e => e.EventImages)
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
                return NotFound();

            var bookedTickets = ev.Bookings
                .Where(b => b.BookingStatus == "confirmed")
                .Sum(b => b.NumberOfTickets);

            var availableTickets = ev.TotalCapacity - bookedTickets;

            // Store in ViewData
            ViewData["AvailableTickets"] = availableTickets;
            ViewData["TicketPrice"] = ev.TicketPrice;

            // Loyalty points
            var loyaltyPoints = await _db.LoyaltyPoints
                .Where(lp => lp.CustomerId == user.Id)
                .Select(lp => lp.Points)
                .FirstOrDefaultAsync();

            ViewData["LoyaltyPoints"] = loyaltyPoints;

            return View(ev);
        }

        // ==============================
        //  VALIDATE PROMOTION CODE (AJAX)
        // ==============================
        [HttpGet]
        public async Task<IActionResult> ValidatePromotion(string code)
        {
            var promotion = await _db.Promotions
                .FirstOrDefaultAsync(p => p.Code == code
                    && p.Status == "active"
                    && p.StartDate <= DateTime.UtcNow
                    && p.EndDate >= DateTime.UtcNow);

            if (promotion == null)
            {
                return Json(new
                {
                    isValid = false,
                    message = "Invalid or expired promotion code"
                });
            }

            return Json(new
            {
                isValid = true,
                message = $"Promotion applied! {promotion.DiscountValue}{(promotion.DiscountType == "percentage" ? "%" : " Rs")} discount",
                promotion = new
                {
                    code = promotion.Code,
                    discountType = promotion.DiscountType,
                    discountValue = promotion.DiscountValue
                }
            });
        }

        // ==============================
        //  BOOK EVENT
        // ==============================

        [HttpPost]
        public async Task<IActionResult> BookEvent(
    int EventId,
    int NumberOfTickets,
    string? PromoCode,
    int LoyaltyUsed,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount)
        {
            var user = _sessionManager.GetUserSession();
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Event exist check + capacity
            var ev = await _db.Events
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.Id == EventId);

            if (ev == null)
            {
                TempData["ErrorMessage"] = "Event not found";
                return RedirectToAction("Index", "Event");
            }

            // Available tickets
            var bookedTickets = ev.Bookings
                .Where(b => b.BookingStatus == "confirmed")
                .Sum(b => b.NumberOfTickets);
            var availableTickets = ev.TotalCapacity - bookedTickets;

            if (NumberOfTickets > availableTickets)
            {
                TempData["ErrorMessage"] = $"Only {availableTickets} tickets available";
                return RedirectToAction("BookEvent", new { id = EventId });
            }

            // Validate loyalty points
            var userLoyalty = await _db.LoyaltyPoints
                .FirstOrDefaultAsync(lp => lp.CustomerId == user.Id);

            if (LoyaltyUsed > (userLoyalty?.Points ?? 0))
            {
                TempData["ErrorMessage"] = "Insufficient loyalty points";
                return RedirectToAction("BookEvent", new { id = EventId });
            }

            // Promotion validation
            decimal promotionDiscount = 0;
            if (!string.IsNullOrEmpty(PromoCode))
            {
                var promotion = await _db.Promotions
                    .FirstOrDefaultAsync(p => p.Code == PromoCode
                        && p.Status == "active"
                        && p.StartDate <= DateTime.UtcNow
                        && p.EndDate >= DateTime.UtcNow);

                if (promotion == null)
                {
                    TempData["ErrorMessage"] = "Invalid promotion code";
                    return RedirectToAction("BookEvent", new { id = EventId });
                }

                var subtotal = ev.TicketPrice * NumberOfTickets;
                if (promotion.DiscountType == "percentage")
                {
                    promotionDiscount = subtotal * (promotion.DiscountValue / 100);
                }
                else
                {
                    promotionDiscount = Math.Min(promotion.DiscountValue, subtotal);
                }
            }

            // Loyalty discount calculation
            decimal loyaltyDiscount = Math.Min(LoyaltyUsed * 0.10m, (ev.TicketPrice * NumberOfTickets) - promotionDiscount);

            // Amount validation
            var calculatedTotal = ev.TicketPrice * NumberOfTickets;
            var calculatedDiscount = promotionDiscount + loyaltyDiscount;
            var calculatedFinal = calculatedTotal - calculatedDiscount;

            if (Math.Abs(TotalAmount - calculatedTotal) > 0.01m ||
                Math.Abs(DiscountAmount - calculatedDiscount) > 0.01m ||
                Math.Abs(FinalAmount - calculatedFinal) > 0.01m)
            {
                TempData["ErrorMessage"] = "Amount calculation mismatch. Please try again.";
                return RedirectToAction("BookEvent", new { id = EventId });
            }

            // Create booking
            var booking = new Booking
            {
                EventId = EventId,
                CustomerId = user.Id,
                NumberOfTickets = NumberOfTickets,
                TicketPrice = ev.TicketPrice,
                TotalAmount = TotalAmount,
                DiscountAmount = DiscountAmount,
                FinalAmount = FinalAmount,
                PromotionCode = PromoCode,
                LoyaltyUsed = LoyaltyUsed,
                LoyaltyEarned = CalculateLoyaltyEarned(FinalAmount),
                PaymentStatus = "pending",
                BookingStatus = "pending",
                CreatedAt = DateTime.UtcNow
            };

            
            try
            {
                // Add booking
                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync(); // Save first to get booking.Id

            }
            catch
            {
                TempData["ErrorMessage"] = "Booking failed. Please try again.";
                return RedirectToAction("BookEvent", new { id = EventId });
            }

            // Redirect to checkout
            return RedirectToAction("Checkout", new { id = booking.Id });
        }


        // ==============================
        //  CHECKOUT PAGE
        // ==============================
        public async Task<IActionResult> Checkout(int id)
        {
            var user = _sessionManager.GetUserSession();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var booking = await _db.Bookings
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .Include(b => b.Tickets) // include existing tickets if any
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound();

            if (booking.CustomerId != user.Id)
                return Forbid();

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // --- Loyalty updates ---
                var userLoyalty = await _db.LoyaltyPoints.FirstOrDefaultAsync(lp => lp.CustomerId == user.Id);
                if (userLoyalty == null)
                {
                    userLoyalty = new LoyaltyPoint
                    {
                        CustomerId = user.Id,
                        Points = 0,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.LoyaltyPoints.Add(userLoyalty);
                }

                if (booking.LoyaltyUsed > 0)
                {
                    userLoyalty.Points -= booking.LoyaltyUsed;
                    _db.LoyaltyHistories.Add(new LoyaltyHistory
                    {
                        CustomerId = user.Id,
                        BookingId = booking.Id,
                        ChangeType = "use",
                        Points = booking.LoyaltyUsed,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (booking.LoyaltyEarned > 0)
                {
                    userLoyalty.Points += booking.LoyaltyEarned;
                    _db.LoyaltyHistories.Add(new LoyaltyHistory
                    {
                        CustomerId = user.Id,
                        BookingId = booking.Id,
                        ChangeType = "earn",
                        Points = booking.LoyaltyEarned,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                userLoyalty.UpdatedAt = DateTime.UtcNow;
                _db.LoyaltyPoints.Update(userLoyalty);

                // --- Payment record ---
                if (!_db.Payments.Any(p => p.BookingId == booking.Id))
                {
                    _db.Payments.Add(new Payment
                    {
                        BookingId = booking.Id,
                        Amount = booking.FinalAmount,
                        PaymentMethod = "card",
                        Status = "completed",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // --- Booking status ---
                booking.PaymentStatus = "paid";
                booking.BookingStatus = "confirmed";
                _db.Bookings.Update(booking);

                // --- Generate tickets ---
                if (!booking.Tickets.Any()) // prevent duplicate tickets
                {
                    for (int i = 1; i <= booking.NumberOfTickets; i++)
                    {
                        string ticketNumber = $"TCKT-{booking.Id:00000}-{i:000}";
                        string qrContent = $"https://yourdomain.com/Tickets/Verify/{ticketNumber}";

                        // Generate QR code image as Base64 string
                        using (var qrGenerator = new QRCodeGenerator())
                        using (var qrData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q))
                        using (var qrCode = new QRCode(qrData))
                        using (var bitmap = qrCode.GetGraphic(20)) // 20 pixels per module
                        using (var ms = new MemoryStream())
                        {
                            bitmap.Save(ms, ImageFormat.Png);
                            string qrBase64 = Convert.ToBase64String(ms.ToArray());
                            string qrImageUrl = $"data:image/png;base64,{qrBase64}";

                            _db.Tickets.Add(new Ticket
                            {
                                BookingId = booking.Id,
                                TicketNumber = ticketNumber,
                                QRCode = qrImageUrl, // store Base64 image
                                Status = "valid"
                            });
                        }
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Failed to process checkout. Please try again.";
                return RedirectToAction("BookEvent", new { id = booking.EventId });
            }

            // Pass booking (with tickets) to view
            booking = await _db.Bookings
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .Include(b => b.Tickets)
                .Include(b => b.Event.Venue)
                .FirstOrDefaultAsync(b => b.Id == id);

            return View(booking);
        }



        // ==============================
        //  BOOKING CONFIRMATION
        // ==============================
        public async Task<IActionResult> BookingConfirmation(int id)
        {
            var user = _sessionManager.GetUserSession();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var booking = await _db.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e.Venue)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound();

            // Verify booking belongs to current user
            if (booking.CustomerId != user.Id)
                return Forbid();

            return View(booking);
        }

        // ==============================
        //  LIST USER BOOKINGS
        // ==============================
        public async Task<IActionResult> MyBookings()
        {
            var user = _sessionManager.GetUserSession();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var bookings = await _db.Bookings
                .Where(b => b.CustomerId == user.Id)
                .Include(b => b.Event)
                .ThenInclude(e => e.Venue)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // ==============================
        //  CALCULATE LOYALTY EARNED
        // ==============================
        private int CalculateLoyaltyEarned(decimal amount)
        {
            // 1 point per Rs 10 spent
            return (int)(amount / 10);
        }


        [HttpPost]
        public async Task<JsonResult> ApplyPromoCode([FromBody] PromoCodeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
                return Json(new { success = false, message = "Promo code is empty", discount = 0 });

            var now = DateTime.Now;
            var promo = await _db.Promotions
                .FirstOrDefaultAsync(p => p.Code == request.Code
                                          && p.Status == "active"
                                          && p.StartDate <= now
                                          && p.EndDate >= now);

            if (promo == null)
                return Json(new { success = false, message = "Invalid promo code", discount = 0 });

            decimal discountAmount = 0;

            // Apply discount on total order price, not per ticket
            if (promo.DiscountType == "fixed")
            {
                discountAmount = promo.DiscountValue; // fixed discount total
            }
            else if (promo.DiscountType == "percentage")
            {
                discountAmount = request.TotalPrice * promo.DiscountValue / 100; // percentage of total
            }

            return Json(new { success = true, message = "Promo code applied!", discount = discountAmount, discount_type = promo.DiscountType });
        }


        public class PromoCodeRequest
        {
            public string Code { get; set; } = string.Empty;
            public decimal TotalPrice { get; set; } // total price of order
        }



    }
}
