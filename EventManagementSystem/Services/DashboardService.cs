using EventManagementSystem.Data;
using EventManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

public interface IDashboardService
{
    Task<OrganizerDashboardViewModel> GetOrganizerDashboardData(int organizerId);
    Task<DashboardStats> GetDashboardStats(int organizerId);


    // Add the missing method definition
    Task<AdminDashboardViewModel> GetAdminDashboardData();
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStats> GetDashboardStats(int organizerId)
    {
        var events = await _context.Events
            .Where(e => e.OrganizerId == organizerId)
            .ToListAsync();

        var bookings = await _context.Bookings
            .Include(b => b.Event)
            .Where(b => b.Event.OrganizerId == organizerId)
            .ToListAsync();

        var totalRevenue = bookings.Where(b => b.PaymentStatus == "paid").Sum(b => b.FinalAmount);
        var totalBookings = bookings.Count;
        var ticketsSold = bookings.Sum(b => b.NumberOfTickets);
        var activeEvents = events.Count(e => e.Status == "upcoming");
        var pendingPayments = bookings.Where(b => b.PaymentStatus == "pending").Sum(b => b.FinalAmount);
        var averageOccupancy = events.Any() ? events.Average(e =>
        {
            var sold = bookings.Where(b => b.EventId == e.Id).Sum(b => b.NumberOfTickets);
            return e.TotalCapacity == 0 ? 0 : ((decimal)sold / e.TotalCapacity) * 100;
        }) : 0;

        return new DashboardStats
        {
            TotalRevenue = totalRevenue,
            TotalBookings = totalBookings,
            TicketsSold = ticketsSold,
            ActiveEvents = activeEvents,
            PendingPayments = pendingPayments,
            AverageOccupancy = averageOccupancy
        };
    }

    public async Task<OrganizerDashboardViewModel> GetOrganizerDashboardData(int organizerId)
    {
        var stats = await GetDashboardStats(organizerId);

        // Upcoming events
        var upcomingEvents = await _context.Events
            .Where(e => e.OrganizerId == organizerId && e.Status == "upcoming" && e.EventDate >= DateTime.Today)
            .OrderBy(e => e.EventDate)
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
            .Take(5)
            .ToListAsync();

        // Recent bookings
        var recentBookings = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Event)
            .Where(b => b.Event.OrganizerId == organizerId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .Select(b => new BookingSummary
            {
                Id = b.Id,
                EventTitle = b.Event.Title,
                CustomerName = b.Customer.Name,
                NumberOfTickets = b.NumberOfTickets,
                FinalAmount = b.FinalAmount,
                PaymentStatus = b.PaymentStatus,
                CreatedAt = b.CreatedAt
            }).ToListAsync();

        // Top events
        var topEvents = await _context.Events
            .Where(e => e.OrganizerId == organizerId)
            .Select(e => new TopEvent
            {
                Id = e.Id,
                Title = e.Title,
                BookingsCount = _context.Bookings.Where(b => b.EventId == e.Id).Count(),
                TotalRevenue = _context.Bookings.Where(b => b.EventId == e.Id && b.PaymentStatus == "paid")
                    .Sum(b => (decimal?)b.FinalAmount) ?? 0,
                OccupancyRate = e.TotalCapacity == 0 ? 0 :
                    (_context.Bookings.Where(b => b.EventId == e.Id).Sum(b => (decimal?)b.NumberOfTickets) ?? 0) / e.TotalCapacity * 100
            })
            .OrderByDescending(e => e.TotalRevenue)
            .Take(5)
            .ToListAsync();

        // Get data for the last 6 months
        var sixMonthsAgo = DateTime.Now.AddMonths(-6);

        var monthlyRevenues = await _context.Bookings
            .Include(b => b.Event)
            .Where(b => b.Event.OrganizerId == organizerId &&
                       b.PaymentStatus == "paid" &&
                       b.CreatedAt >= sixMonthsAgo)
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

        // Convert to MonthlyRevenue list with proper formatting
        var monthlyRevenueList = new List<MonthlyRevenue>();

        // Create entries for all last 6 months, even if no revenue
        for (int i = 5; i >= 0; i--)
        {
            var date = DateTime.Now.AddMonths(-i);
            var monthYear = date.ToString("MMM yyyy");

            var revenue = monthlyRevenues
                .FirstOrDefault(m => m.Year == date.Year && m.Month == date.Month)?
                .Revenue ?? 0;

            monthlyRevenueList.Add(new MonthlyRevenue
            {
                Month = monthYear,
                Revenue = revenue
            });
        }

        return new OrganizerDashboardViewModel
        {
            Stats = stats,
            UpcomingEvents = upcomingEvents,
            RecentBookings = recentBookings,
            TopEvents = topEvents,
            MonthlyRevenues = monthlyRevenueList
        };
    }




    public async Task<AdminDashboardViewModel> GetAdminDashboardData()
    {
        // Get admin-specific stats
        var stats = await GetAdminDashboardStats();

        // Upcoming events
        var upcomingEvents = await _context.Events
            .Where(e => e.Status == "upcoming" && e.EventDate >= DateTime.Today)
            .OrderBy(e => e.EventDate)
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
            .Take(5)
            .ToListAsync();

        // Recent bookings
        var recentBookings = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Event)
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .Select(b => new BookingSummary
            {
                Id = b.Id,
                EventTitle = b.Event.Title,
                CustomerName = b.Customer.Name,
                NumberOfTickets = b.NumberOfTickets,
                FinalAmount = b.FinalAmount,
                PaymentStatus = b.PaymentStatus,
                CreatedAt = b.CreatedAt
            }).ToListAsync();

        // Top events
        var topEvents = await _context.Events
            .Select(e => new TopEvent
            {
                Id = e.Id,
                Title = e.Title,
                BookingsCount = _context.Bookings.Where(b => b.EventId == e.Id).Count(),
                TotalRevenue = _context.Bookings.Where(b => b.EventId == e.Id && b.PaymentStatus == "paid")
                    .Sum(b => (decimal?)b.FinalAmount) ?? 0,
                OccupancyRate = e.TotalCapacity == 0 ? 0 :
                    (_context.Bookings.Where(b => b.EventId == e.Id).Sum(b => (decimal?)b.NumberOfTickets) ?? 0) / e.TotalCapacity * 100
            })
            .OrderByDescending(e => e.TotalRevenue)
            .Take(5)
            .ToListAsync();

        // Top organizers
        var topOrganizers = await GetTopOrganizersAsync();

        // Monthly revenue data
        var monthlyRevenueList = await GetMonthlyRevenueDataAsync();

        return new AdminDashboardViewModel
        {
            Stats = stats,
            UpcomingEvents = upcomingEvents,
            RecentBookings = recentBookings,
            TopEvents = topEvents,
            TopOrganizers = topOrganizers,
            MonthlyRevenues = monthlyRevenueList
        };
    }

    private async Task<DashboardAdminStats> GetAdminDashboardStats()
    {
        var stats = new DashboardAdminStats();

        // Get total users count
        stats.TotalUsers = await _context.Users.CountAsync();

        // Get total organizers count
        stats.TotalOrganizers = await _context.Users
            .Where(u => u.Role == "organizer")
            .CountAsync();

        // Get all bookings
        var bookings = await _context.Bookings.ToListAsync();
        stats.TotalBookings = bookings.Count;

        // Calculate total revenue from paid bookings
        stats.TotalRevenue = bookings
            .Where(b => b.PaymentStatus == "paid")
            .Sum(b => b.FinalAmount);

        // Calculate average booking value
        stats.AverageBookingValue = stats.TotalBookings > 0 ?
            stats.TotalRevenue / stats.TotalBookings : 0;

        // Get new users this month
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        stats.NewUsersThisMonth = await _context.Users
            .Where(u => u.CreatedAt >= startOfMonth)
            .CountAsync();

        // Calculate user growth percentage
        var previousMonthUsers = await _context.Users
            .Where(u => u.CreatedAt < startOfMonth &&
                       u.CreatedAt >= startOfMonth.AddMonths(-1))
            .CountAsync();
        stats.UserGrowthPercentage = previousMonthUsers > 0 ?
            ((decimal)stats.NewUsersThisMonth / previousMonthUsers) * 100 : 100;

        // Get active organizers (those with events)
        stats.ActiveOrganizers = await _context.Users
            .Where(u => u.Role == "organizer" &&
                       _context.Events.Any(e => e.OrganizerId == u.Id))
            .CountAsync();

        // Get organizers with events
        stats.OrganizersWithEvents = await _context.Users
            .Where(u => u.Role == "organizer" &&
                       _context.Events.Any(e => e.OrganizerId == u.Id))
            .CountAsync();

        // Get top organizer revenue
        var topOrganizerRevenueData = await (from organizer in _context.Users
                                             where organizer.Role == "organizer"
                                             join evt in _context.Events on organizer.Id equals evt.OrganizerId
                                             join booking in _context.Bookings on evt.Id equals booking.EventId
                                             where booking.PaymentStatus == "paid"
                                             group booking by organizer.Id into g
                                             select g.Sum(b => b.FinalAmount))
                                         .OrderByDescending(r => r)
                                         .FirstOrDefaultAsync();

        stats.TopOrganizerRevenue = topOrganizerRevenueData > 0 ? topOrganizerRevenueData : 0;

        // Get booking status counts
        stats.ConfirmedBookings = await _context.Bookings
            .Where(b => b.BookingStatus == "confirmed")
            .CountAsync();
        stats.PendingBookings = await _context.Bookings
            .Where(b => b.PaymentStatus == "pending")
            .CountAsync();
        stats.CancelledBookings = await _context.Bookings
            .Where(b => b.BookingStatus == "cancelled")
            .CountAsync();

        // Calculate booking confirmation rate
        stats.BookingConfirmationRate = stats.TotalBookings > 0 ?
            ((decimal)stats.ConfirmedBookings / stats.TotalBookings) * 100 : 0;

        // Get active events count
        stats.ActiveEvents = await _context.Events
            .Where(e => e.Status == "upcoming")
            .CountAsync();

        // Get tickets sold
        stats.TicketsSold = bookings.Sum(b => b.NumberOfTickets);

        // Get pending payments
        stats.PendingPayments = bookings
            .Where(b => b.PaymentStatus == "pending")
            .Sum(b => b.FinalAmount);

        // Calculate average occupancy
        var events = await _context.Events.ToListAsync();
        var totalOccupancy = events.Sum(e =>
        {
            var sold = bookings.Where(b => b.EventId == e.Id).Sum(b => b.NumberOfTickets);
            return e.TotalCapacity == 0 ? 0 : ((decimal)sold / e.TotalCapacity) * 100;
        });

        stats.AverageOccupancy = events.Any() ? totalOccupancy / events.Count : 0;

        return stats;
    }

    private async Task<List<OrganizerSummary>> GetTopOrganizersAsync()
    {
        var topOrganizers = await (from organizer in _context.Users
                                   where organizer.Role == "organizer"
                                   select new OrganizerSummary
                                   {
                                       Id = organizer.Id.ToString(),
                                       Name = organizer.Name,
                                       Email = organizer.Email,
                                       TotalEvents = _context.Events.Count(e => e.OrganizerId == organizer.Id),
                                       TotalBookings = _context.Bookings
                                           .Count(b => _context.Events
                                               .Where(e => e.OrganizerId == organizer.Id)
                                               .Select(e => e.Id)
                                               .Contains(b.EventId)),
                                       TotalRevenue = _context.Bookings
                                           .Where(b => b.PaymentStatus == "paid" &&
                                                      _context.Events
                                                          .Where(e => e.OrganizerId == organizer.Id)
                                                          .Select(e => e.Id)
                                                          .Contains(b.EventId))
                                           .Sum(b => (decimal?)b.FinalAmount) ?? 0
                                   })
                                 .Where(o => o.TotalEvents > 0) // Only organizers with events
                                 .OrderByDescending(o => o.TotalRevenue)
                                 .Take(5)
                                 .ToListAsync();

        return topOrganizers;
    }

    private async Task<List<MonthlyRevenue>> GetMonthlyRevenueDataAsync()
    {
        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        var monthlyRevenues = await _context.Bookings
            .Where(b => b.PaymentStatus == "paid" && b.CreatedAt >= sixMonthsAgo)
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

        // Convert to MonthlyRevenue list
        var monthlyRevenueList = new List<MonthlyRevenue>();
        for (int i = 5; i >= 0; i--)
        {
            var date = DateTime.Now.AddMonths(-i);
            var monthYear = date.ToString("MMM yyyy");

            var revenue = monthlyRevenues
                .FirstOrDefault(m => m.Year == date.Year && m.Month == date.Month)?
                .Revenue ?? 0;

            monthlyRevenueList.Add(new MonthlyRevenue
            {
                Month = monthYear,
                Revenue = revenue
            });
        }

        return monthlyRevenueList;
    }





}
