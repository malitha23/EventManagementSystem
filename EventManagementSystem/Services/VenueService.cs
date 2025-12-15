// Services/VenueService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventManagementSystem.Data;
using EventManagementSystem.Models;
using EventManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

public interface IVenueService
{
    Task<VenuesViewModel> GetAdminVenues(VenueFilterModel filter, int page = 1, int pageSize = 10);
    Task<Venue> GetVenueById(int id);
    Task<VenueDetailsViewModel> GetVenueDetails(int id);
    Task<bool> CreateVenue(VenueCreateViewModel model);
    Task<bool> UpdateVenue(VenueEditViewModel model);
    Task<bool> DeleteVenue(int id);
    Task<bool> ToggleVenueStatus(int id);
    Task<List<string>> GetVenueLocations();
    Task<VenueStats> GetVenueStats(int venueId);
}

public class VenueService : IVenueService
{
    private readonly ApplicationDbContext _context;

    public VenueService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VenuesViewModel> GetAdminVenues(VenueFilterModel filter, int page = 1, int pageSize = 10)
    {
        var query = _context.Venues.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.SearchTerm))
            query = query.Where(v =>
                v.Name.Contains(filter.SearchTerm) ||
                v.Location.Contains(filter.SearchTerm) ||
                (v.Description != null && v.Description.Contains(filter.SearchTerm)));

        if (!string.IsNullOrEmpty(filter.Location))
            query = query.Where(v => v.Location == filter.Location);

        if (filter.MinCapacity.HasValue)
            query = query.Where(v => v.Capacity >= filter.MinCapacity.Value);

        if (filter.MaxCapacity.HasValue)
            query = query.Where(v => v.Capacity <= filter.MaxCapacity.Value);

        // Sorting
        query = ApplySorting(query, filter.SortBy, filter.SortOrder);

        var totalVenues = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalVenues / pageSize);

        // Get paginated venues
        var venues = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var venueIds = venues.Select(v => v.Id).ToList();

        // Get all events for these venues
        var events = await _context.Events
            .Where(e => venueIds.Contains(e.VenueId))
            .ToListAsync();

        var eventIds = events.Select(e => e.Id).ToList();

        // Get all bookings for these events
        var bookings = await _context.Bookings
            .Where(b => eventIds.Contains(b.EventId))
            .ToListAsync();

        // Build venue stats
        var venueList = venues.Select(v =>
        {
            var venueEvents = events.Where(e => e.VenueId == v.Id).ToList();
            var venueEventIds = venueEvents.Select(e => e.Id).ToList();
            var venueBookings = bookings.Where(b => venueEventIds.Contains(b.EventId)).ToList();

            var totalRevenue = venueBookings
                .Where(b => !string.IsNullOrEmpty(b.PaymentStatus) &&
                            b.PaymentStatus.Equals("paid", StringComparison.OrdinalIgnoreCase))
                .Sum(b => b.FinalAmount);

            decimal averageOccupancy = 0;
            foreach (var evt in venueEvents)
            {
                if (evt.TotalCapacity <= 0) continue;
                var ticketsSold = venueBookings
                    .Where(b => b.EventId == evt.Id)
                    .Sum(b => b.NumberOfTickets);
                averageOccupancy += ((decimal)ticketsSold / evt.TotalCapacity) * 100;
            }
            averageOccupancy = venueEvents.Count > 0 ? averageOccupancy / venueEvents.Count : 0;

            return new VenueViewModel
            {
                Id = v.Id,
                Name = v.Name,
                Location = v.Location,
                Description = v.Description,
                Capacity = v.Capacity,
              
                EventsCount = venueEvents.Count,
                TotalRevenue = totalRevenue,
                OccupancyRate = averageOccupancy
            };
        }).ToList();

        // Get unique locations for filter dropdown
        var locations = await _context.Venues
            .Select(v => v.Location)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();

        return new VenuesViewModel
        {
            Venues = venueList,
            Filter = filter,
            TotalVenues = totalVenues,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Locations = locations
        };
    }

    private IQueryable<Venue> ApplySorting(IQueryable<Venue> query, string sortBy, string sortOrder)
    {
        sortBy ??= "Name";
        sortOrder ??= "asc";

        return sortBy.ToLower() switch
        {
            "name" => sortOrder == "asc" ? query.OrderBy(v => v.Name) : query.OrderByDescending(v => v.Name),
            "location" => sortOrder == "asc" ? query.OrderBy(v => v.Location) : query.OrderByDescending(v => v.Location),
            "capacity" => sortOrder == "asc" ? query.OrderBy(v => v.Capacity) : query.OrderByDescending(v => v.Capacity),
            "events" => sortOrder == "asc" ?
                query.OrderBy(v => _context.Events.Count(e => e.VenueId == v.Id)) :
                query.OrderByDescending(v => _context.Events.Count(e => e.VenueId == v.Id)),
            _ => query.OrderBy(v => v.Name)
        };
    }

    public async Task<Venue> GetVenueById(int id)
    {
        return await _context.Venues.FindAsync(id);
    }

    public async Task<VenueDetailsViewModel> GetVenueDetails(int id)
    {
        var venue = await _context.Venues.FindAsync(id);
        if (venue == null)
            return null;

        // Get venue events
        var events = await _context.Events
            .Where(e => e.VenueId == id)
            .Include(e => e.Organizer)
            .Include(e => e.Category)
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
                CategoryName = e.Category?.Name ?? "N/A",
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
                CategoryName = e.Category?.Name ?? "N/A",
                Status = e.Status
            })
            .Take(10)
            .ToList();

        // Calculate stats
        var stats = await GetVenueStats(id);

        var venueViewModel = new VenueViewModel
        {
            Id = venue.Id,
            Name = venue.Name,
            Location = venue.Location,
            Description = venue.Description,
            Capacity = venue.Capacity,
            CreatedAt = DateTime.UtcNow,
            EventsCount = events.Count,
            TotalRevenue = stats.TotalRevenue,
            OccupancyRate = stats.AverageOccupancy
        };

        return new VenueDetailsViewModel
        {
            Venue = venueViewModel,
            UpcomingEvents = upcomingEvents,
            PastEvents = pastEvents,
            Stats = stats
        };
    }

    public async Task<VenueStats> GetVenueStats(int venueId)
    {
        // Get all events for this venue
        var events = await _context.Events
            .Where(e => e.VenueId == venueId)
            .ToListAsync();

        if (!events.Any())
        {
            return new VenueStats
            {
                TotalEvents = 0,
                TotalBookings = 0,
                TotalTicketsSold = 0,
                TotalRevenue = 0,
                AverageOccupancy = 0,
                UpcomingEventsCount = 0,
                PastEventsCount = 0
            };
        }

        var eventIds = events.Select(e => e.Id).ToList();

        // Get bookings only for these events
        var bookings = await _context.Bookings
            .Where(b => eventIds.Contains(b.EventId))
            .ToListAsync();

        var now = DateTime.Now;

        // Count upcoming & past events
        var upcomingEventsCount = events.Count(e => e.EventDate >= now);
        var pastEventsCount = events.Count(e => e.EventDate < now);

        // Total tickets sold
        var totalTicketsSold = bookings.Sum(b => b.NumberOfTickets);

        // Total revenue for paid bookings (case-insensitive)
        var totalRevenue = bookings
            .Where(b => !string.IsNullOrEmpty(b.PaymentStatus) &&
                        b.PaymentStatus.Equals("paid", StringComparison.OrdinalIgnoreCase))
            .Sum(b => b.FinalAmount);

        // Average occupancy
        decimal averageOccupancy = 0;
        foreach (var evt in events)
        {
            if (evt.TotalCapacity <= 0) continue; // Avoid division by zero

            var ticketsSoldForEvent = bookings
                .Where(b => b.EventId == evt.Id)
                .Sum(b => b.NumberOfTickets);

            averageOccupancy += ((decimal)ticketsSoldForEvent / evt.TotalCapacity) * 100;
        }
        averageOccupancy = events.Count > 0 ? averageOccupancy / events.Count : 0;

        return new VenueStats
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


    public async Task<bool> CreateVenue(VenueCreateViewModel model)
    {
        try
        {
            var venue = new Venue
            {
                Name = model.Name,
                Location = model.Location,
                Description = model.Description,
                Capacity = model.Capacity
                // Add CreatedAt when you add the field to model
            };

            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating venue: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateVenue(VenueEditViewModel model)
    {
        try
        {
            var venue = await _context.Venues.FindAsync(model.Id);
            if (venue == null) return false;

            venue.Name = model.Name;
            venue.Location = model.Location;
            venue.Description = model.Description;
            venue.Capacity = model.Capacity;
            // venue.IsActive = model.IsActive; // Add this field later

            _context.Venues.Update(venue);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating venue: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteVenue(int id)
    {
        try
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return false;

            // Check if venue has events
            var hasEvents = await _context.Events.AnyAsync(e => e.VenueId == id);
            if (hasEvents)
            {
                // Instead of deleting, mark as inactive (when we have IsActive field)
                // For now, just return false
                return false;
            }

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting venue: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ToggleVenueStatus(int id)
    {
        try
        {
            // When you add IsActive field to Venue model
            /*
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return false;

            venue.IsActive = !venue.IsActive;
            _context.Venues.Update(venue);
            await _context.SaveChangesAsync();
            return true;
            */
            return false; // Temporary until IsActive field is added
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling venue status: {ex.Message}");
            return false;
        }
    }

    public async Task<List<string>> GetVenueLocations()
    {
        return await _context.Venues
            .Select(v => v.Location)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();
    }
}