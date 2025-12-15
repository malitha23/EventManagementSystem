// Create EventService.cs in Services folder
using EventManagementSystem.Data;
using EventManagementSystem.Models;
using EventManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

public interface IEventService
{
    Task<AdminEventsViewModel> GetAdminEvents(EventFilterModel filter, int page = 1, int pageSize = 10);
    Task<Event> GetEventById(int id);
    Task<bool> DeleteEvent(int id);
    Task<bool> ToggleEventStatus(int id);
    Task<List<string>> GetEventStatuses();
    Task<decimal> GetTotalRevenue();
    Task<int> GetTotalEvents();
}

public class EventService : IEventService
{
    private readonly ApplicationDbContext _context;

    public EventService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminEventsViewModel> GetAdminEvents(EventFilterModel filter, int page = 1, int pageSize = 10)
    {
        var query = _context.Events
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .Include(e => e.Organizer)
            .Include(e => e.EventImages)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(e =>
                e.Title.Contains(filter.SearchTerm) ||
                e.Description.Contains(filter.SearchTerm));
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            query = query.Where(e => e.Status == filter.Status);
        }

        if (!string.IsNullOrEmpty(filter.Category))
        {
            query = query.Where(e => e.Category.Name == filter.Category);
        }

        if (!string.IsNullOrEmpty(filter.Organizer))
        {
            query = query.Where(e => e.Organizer.Name.Contains(filter.Organizer));
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(e => e.EventDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(e => e.EventDate <= filter.ToDate.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(e => e.TicketPrice >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(e => e.TicketPrice <= filter.MaxPrice.Value);
        }

        // Apply sorting
        query = ApplySorting(query, filter.SortBy, filter.SortOrder);

        // Get total count for pagination
        var totalEvents = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalEvents / pageSize);

        // Get events with statistics
        var events = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description.Length > 100 ? e.Description.Substring(0, 100) + "..." : e.Description,
                EventDate = e.EventDate,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                TicketPrice = e.TicketPrice,
                Status = e.Status,
                TotalCapacity = e.TotalCapacity,
                TicketsSold = _context.Bookings.Where(b => b.EventId == e.Id).Sum(b => b.NumberOfTickets),
                Revenue = _context.Bookings
                    .Where(b => b.EventId == e.Id && b.PaymentStatus == "paid")
                    .Sum(b => (decimal?)b.FinalAmount) ?? 0,
                CategoryName = e.Category.Name,
                VenueName = e.Venue.Name,
                OrganizerName = e.Organizer.Name,
                OrganizerEmail = e.Organizer.Email,
                CreatedAt = e.CreatedAt,
                MainImageUrl = e.EventImages.FirstOrDefault().ImageUrl ?? "/images/default-event.jpg",
                BookingCount = _context.Bookings.Count(b => b.EventId == e.Id)
            })
            .ToListAsync();

        // Calculate occupancy rate
        foreach (var evt in events)
        {
            evt.OccupancyRate = evt.TotalCapacity > 0 ?
                (evt.TicketsSold * 100m) / evt.TotalCapacity : 0;
        }

        // Get filter options
        var categories = await _context.EventCategories
            .Select(c => c.Name)
            .Distinct()
            .ToListAsync();

        var organizers = await _context.Users
            .Where(u => u.Role == "organizer")
            .Select(u => u.Name)
            .Distinct()
            .ToListAsync();

        return new AdminEventsViewModel
        {
            Events = events,
            Filter = filter,
            TotalEvents = totalEvents,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Categories = categories,
            Organizers = organizers
        };
    }

    private IQueryable<Event> ApplySorting(IQueryable<Event> query, string sortBy, string sortOrder)
    {
        sortBy ??= "EventDate";
        sortOrder ??= "asc";

        return sortBy.ToLower() switch
        {
            "title" => sortOrder == "asc" ? query.OrderBy(e => e.Title) : query.OrderByDescending(e => e.Title),
            "date" => sortOrder == "asc" ? query.OrderBy(e => e.EventDate) : query.OrderByDescending(e => e.EventDate),
            "price" => sortOrder == "asc" ? query.OrderBy(e => e.TicketPrice) : query.OrderByDescending(e => e.TicketPrice),
            "revenue" => sortOrder == "asc" ?
                query.OrderBy(e => _context.Bookings
                    .Where(b => b.EventId == e.Id && b.PaymentStatus == "paid")
                    .Sum(b => b.FinalAmount)) :
                query.OrderByDescending(e => _context.Bookings
                    .Where(b => b.EventId == e.Id && b.PaymentStatus == "paid")
                    .Sum(b => b.FinalAmount)),
            "tickets" => sortOrder == "asc" ?
                query.OrderBy(e => _context.Bookings
                    .Where(b => b.EventId == e.Id)
                    .Sum(b => b.NumberOfTickets)) :
                query.OrderByDescending(e => _context.Bookings
                    .Where(b => b.EventId == e.Id)
                    .Sum(b => b.NumberOfTickets)),
            _ => query.OrderBy(e => e.EventDate)
        };
    }

    public async Task<Event> GetEventById(int id)
    {
        return await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .Include(e => e.Organizer)
            .Include(e => e.EventImages)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<bool> DeleteEvent(int id)
    {
        try
        {
            var eventToDelete = await _context.Events.FindAsync(id);
            if (eventToDelete == null) return false;

            // Check if there are bookings for this event
            var hasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);

            if (hasBookings)
            {
                // Don't delete, just mark as cancelled
                eventToDelete.Status = "cancelled";
                _context.Events.Update(eventToDelete);
            }
            else
            {
                // No bookings, safe to delete
                _context.Events.Remove(eventToDelete);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ToggleEventStatus(int id)
    {
        try
        {
            var eventToUpdate = await _context.Events.FindAsync(id);
            if (eventToUpdate == null) return false;

            // Toggle between active and cancelled
            eventToUpdate.Status = eventToUpdate.Status == "upcoming" ? "cancelled" : "upcoming";
            _context.Events.Update(eventToUpdate);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetEventStatuses()
    {
        return await _context.Events
            .Select(e => e.Status)
            .Distinct()
            .ToListAsync();
    }

    public async Task<decimal> GetTotalRevenue()
    {
        return await _context.Bookings
            .Where(b => b.PaymentStatus == "paid")
            .SumAsync(b => b.FinalAmount);
    }

    public async Task<int> GetTotalEvents()
    {
        return await _context.Events.CountAsync();
    }
}