// Services/EventCategoryService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventManagementSystem.Data;
using EventManagementSystem.Models;
using EventManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

public interface IEventCategoryService
{
    Task<EventCategoriesViewModel> GetCategories(CategoryFilterModel filter, int page = 1, int pageSize = 10);
    Task<EventCategory> GetCategoryById(int id);
    Task<CategoryDetailsViewModel> GetCategoryDetails(int id);
    Task<bool> CreateCategory(EventCategoryCreateViewModel model);
    Task<bool> UpdateCategory(EventCategoryEditViewModel model);
    Task<bool> DeleteCategory(int id);
    Task<bool> CategoryHasEvents(int id);
}

public class EventCategoryService : IEventCategoryService
{
    private readonly ApplicationDbContext _context;

    public EventCategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EventCategoriesViewModel> GetCategories(CategoryFilterModel filter, int page = 1, int pageSize = 10)
    {
        var query = _context.EventCategories.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(c => c.Name.Contains(filter.SearchTerm));
        }

        // Apply sorting
        query = ApplySorting(query, filter.SortBy, filter.SortOrder);

        // Get total count for pagination
        var totalCategories = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCategories / pageSize);

        // Get categories with statistics
        var categoryIds = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => c.Id)
            .ToListAsync();

        // Get categories with their data
        var categoriesWithData = await _context.EventCategories
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new
            {
                Category = c,
                EventsCount = _context.Events.Count(e => e.CategoryId == c.Id),
                BookingsCount = _context.Bookings.Count(b => _context.Events
                    .Where(e => e.CategoryId == c.Id)
                    .Select(e => e.Id)
                    .Contains(b.EventId)),
                TotalRevenue = _context.Bookings
                    .Where(b => _context.Events
                        .Where(e => e.CategoryId == c.Id)
                        .Select(e => e.Id)
                        .Contains(b.EventId) && b.PaymentStatus == "paid")
                    .Sum(b => (decimal?)b.FinalAmount) ?? 0
            })
            .ToListAsync();

        // Convert to EventCategoryViewModel
        var categories = categoriesWithData.Select(x => new EventCategoryViewModel
        {
            Id = x.Category.Id,
            Name = x.Category.Name,
            EventsCount = x.EventsCount,
            BookingsCount = x.BookingsCount,
            TotalRevenue = x.TotalRevenue
        }).ToList();

        return new EventCategoriesViewModel
        {
            Categories = categories,
            Filter = filter,
            TotalCategories = totalCategories,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private IQueryable<EventCategory> ApplySorting(IQueryable<EventCategory> query, string sortBy, string sortOrder)
    {
        sortBy ??= "Name";
        sortOrder ??= "asc";

        return sortBy.ToLower() switch
        {
            "name" => sortOrder == "asc" ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
            "events" => sortOrder == "asc" ?
                query.OrderBy(c => _context.Events.Count(e => e.CategoryId == c.Id)) :
                query.OrderByDescending(c => _context.Events.Count(e => e.CategoryId == c.Id)),
            "revenue" => sortOrder == "asc" ?
                query.OrderBy(c => _context.Bookings
                    .Where(b => _context.Events
                        .Where(e => e.CategoryId == c.Id)
                        .Select(e => e.Id)
                        .Contains(b.EventId) && b.PaymentStatus == "paid")
                    .Sum(b => (decimal?)b.FinalAmount) ?? 0) :
                query.OrderByDescending(c => _context.Bookings
                    .Where(b => _context.Events
                        .Where(e => e.CategoryId == c.Id)
                        .Select(e => e.Id)
                        .Contains(b.EventId) && b.PaymentStatus == "paid")
                    .Sum(b => (decimal?)b.FinalAmount) ?? 0),
            _ => query.OrderBy(c => c.Name)
        };
    }

    public async Task<EventCategory> GetCategoryById(int id)
    {
        return await _context.EventCategories.FindAsync(id);
    }

    public async Task<CategoryDetailsViewModel> GetCategoryDetails(int id)
    {
        var category = await _context.EventCategories.FindAsync(id);
        if (category == null)
            return null;

        // Get category events
        var events = await _context.Events
            .Where(e => e.CategoryId == id)
            .Include(e => e.Organizer)
            .Include(e => e.Venue)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();

        // Split into upcoming and past events
        var now = DateTime.Now;
        var upcomingEvents = events
            .Where(e => e.EventDate >= now)
            .Select(e => new EventSummary
            {
                Id = e.Id,
                Title = e.Title,
                EventDate = e.EventDate,
                OrganizerName = e.Organizer?.Name ?? "N/A",
                VenueName = e.Venue?.Name ?? "N/A",
                Status = e.Status
            })
            .Take(10)
            .ToList();

        var pastEvents = events
            .Where(e => e.EventDate < now)
            .Select(e => new EventSummary
            {
                Id = e.Id,
                Title = e.Title,
                EventDate = e.EventDate,
                OrganizerName = e.Organizer?.Name ?? "N/A",
                VenueName = e.Venue?.Name ?? "N/A",
                Status = e.Status
            })
            .Take(10)
            .ToList();

        // Calculate stats
        var stats = await GetCategoryStats(id);

        var categoryViewModel = new EventCategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            EventsCount = events.Count,
            BookingsCount = stats.TotalBookings,
            TotalRevenue = stats.TotalRevenue
        };

        return new CategoryDetailsViewModel
        {
            Category = categoryViewModel,
            UpcomingEvents = upcomingEvents,
            PastEvents = pastEvents,
            Stats = stats
        };
    }

    public async Task<CategoryStats> GetCategoryStats(int categoryId)
    {
        var events = await _context.Events
            .Where(e => e.CategoryId == categoryId)
            .ToListAsync();

        var eventIds = events.Select(e => e.Id).ToList();

        var bookings = await _context.Bookings
            .Where(b => eventIds.Contains(b.EventId))
            .ToListAsync();

        var now = DateTime.Now;
        var upcomingEventsCount = events.Count(e => e.EventDate >= now);
        var pastEventsCount = events.Count(e => e.EventDate < now);

        var totalTicketsSold = bookings.Sum(b => b.NumberOfTickets);
        var totalRevenue = bookings
            .Where(b => b.PaymentStatus == "paid")
            .Sum(b => b.FinalAmount);

        // Calculate average occupancy
        decimal averageOccupancy = 0;
        if (events.Any())
        {
            foreach (var evt in events)
            {
                var ticketsSold = bookings
                    .Where(b => b.EventId == evt.Id)
                    .Sum(b => b.NumberOfTickets);

                if (evt.TotalCapacity > 0)
                {
                    averageOccupancy += ((decimal)ticketsSold / evt.TotalCapacity) * 100;
                }
            }
            averageOccupancy /= events.Count;
        }

        return new CategoryStats
        {
            TotalEvents = events.Count,
            TotalBookings = bookings.Count,
            TotalTicketsSold = totalTicketsSold,
            TotalRevenue = totalRevenue,
            AverageOccupancy = averageOccupancy,
            UpcomingEventsCount = upcomingEventsCount,
            PastEventsCount = pastEventsCount
        };
    }

    public async Task<bool> CreateCategory(EventCategoryCreateViewModel model)
    {
        try
        {
            // Check if category already exists
            var existingCategory = await _context.EventCategories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == model.Name.ToLower());

            if (existingCategory != null)
            {
                return false; // Category already exists
            }

            var category = new EventCategory
            {
                Name = model.Name.Trim()
            };

            _context.EventCategories.Add(category);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating category: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateCategory(EventCategoryEditViewModel model)
    {
        try
        {
            var category = await _context.EventCategories.FindAsync(model.Id);
            if (category == null) return false;

            // Check if new name already exists (excluding current category)
            var existingCategory = await _context.EventCategories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == model.Name.ToLower() && c.Id != model.Id);

            if (existingCategory != null)
            {
                return false; // Category name already exists
            }

            category.Name = model.Name.Trim();

            _context.EventCategories.Update(category);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating category: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteCategory(int id)
    {
        try
        {
            var category = await _context.EventCategories.FindAsync(id);
            if (category == null) return false;

            // Check if category has events
            var hasEvents = await _context.Events.AnyAsync(e => e.CategoryId == id);
            if (hasEvents)
            {
                return false; // Cannot delete category with events
            }

            _context.EventCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting category: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CategoryHasEvents(int id)
    {
        return await _context.Events.AnyAsync(e => e.CategoryId == id);
    }
}