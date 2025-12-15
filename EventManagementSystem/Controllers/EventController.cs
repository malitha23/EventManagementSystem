using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Data;
using EventManagementSystem.Models;
using EventManagementSystem.Models.ViewModels;

public class EventController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventController> _logger;
    private readonly IWebHostEnvironment _environment;

    public EventController(ApplicationDbContext context,
                           ILogger<EventController> logger,
                           IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .Include(e => e.EventImages)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
            return NotFound();

        return View(ev); // Single Event object
    }


    [HttpGet]
    public async Task<IActionResult> GetEventCategories()
    {
        try
        {
            var categories = await _context.EventCategories
                .Select(c => new { id = c.Id, name = c.Name })
                .ToListAsync();

            return Json(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading event categories");
            return Json(new List<object>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetVenues()
    {
        try
        {
            var venues = await _context.Venues
                .Select(v => new { id = v.Id, name = v.Name, location = v.Location })
                .ToListAsync();

            return Json(venues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading venues");
            return Json(new List<object>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] EventCreateViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    message = "Please fix validation errors",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }

            // Get current organizer from session
            var organizerId = HttpContext.Session.GetInt32("UserId");
            if (organizerId == null)
            {
                return Json(new
                {
                    success = false,
                    message = "User not authenticated. Please login again."
                });
            }

            int? venueId = model.VenueId;

            // If new venue is added, insert into Venue table
            if (!string.IsNullOrEmpty(model.NewVenueName) && !string.IsNullOrEmpty(model.NewVenueLocation))
            {
                var newVenue = new Venue
                {
                    Name = model.NewVenueName!,
                    Location = model.NewVenueLocation!,
                    Description = model.NewVenueDescription,
                    Capacity = model.NewVenueCapacity ?? 0
                };

                _context.Venues.Add(newVenue);
                await _context.SaveChangesAsync();

                venueId = newVenue.Id; // use new venue ID for the event
            }

            // Create event
            var newEvent = new Event
            {
                Title = model.Title,
                Description = model.Description,
                EventDate = model.EventDate,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                TicketPrice = model.TicketPrice,
                TotalCapacity = model.TotalCapacity,
                CategoryId = model.CategoryId,
                VenueId = venueId.Value,
                OrganizerId = organizerId.Value,
                Status = "upcoming",
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            // Handle multiple image uploads
            if (model.EventImages != null && model.EventImages.Count > 0)
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "events");
                if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                foreach (var image in model.EventImages)
                {
                    if (image.Length > 0)
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                        var filePath = Path.Combine(uploadsPath, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        _context.EventImages.Add(new EventImage
                        {
                            EventId = newEvent.Id,
                            ImageUrl = $"/uploads/events/{uniqueFileName}"
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            return Json(new
            {
                success = true,
                message = "Event created successfully!",
                eventId = newEvent.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            return Json(new
            {
                success = false,
                message = "An error occurred while creating the event. Please try again."
            });
        }
    }

    // GET: Event/Edit/5
    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await _context.Events
            .Include(e => e.EventImages)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
            return NotFound();

        // Pass categories and venues to the view
        ViewBag.Categories = await _context.EventCategories.ToListAsync();
        ViewBag.Venues = await _context.Venues.ToListAsync();

        return View(ev);
    }


    // POST: Event/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Event model, List<IFormFile>? NewImages)
    {
        if (id != model.Id) return BadRequest();

        var ev = await _context.Events
            .Include(e => e.EventImages)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null) return NotFound();

        // Update basic properties
        ev.Title = model.Title;
        ev.Description = model.Description;
        ev.EventDate = model.EventDate;
        ev.StartTime = model.StartTime;
        ev.EndTime = model.EndTime;
        ev.TicketPrice = model.TicketPrice;
        ev.TotalCapacity = model.TotalCapacity;
        ev.Status = model.Status;
        ev.CategoryId = model.CategoryId;
        ev.VenueId = model.VenueId;

        // Handle new images
        if (NewImages != null && NewImages.Count > 0)
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "events");
            if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

            foreach (var image in NewImages)
            {
                if (image.Length > 0)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                    var filePath = Path.Combine(uploadsPath, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    ev.EventImages.Add(new EventImage
                    {
                        EventId = ev.Id,
                        ImageUrl = $"/uploads/events/{uniqueFileName}"
                    });
                }
            }
        }

        _context.Update(ev);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = ev.Id });
    }


    // POST: Event/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _context.Events
            .Include(e => e.EventImages)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null) return NotFound();

        // Delete images from wwwroot
        foreach (var img in ev.EventImages)
        {
            var filePath = Path.Combine(_environment.WebRootPath, img.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();

        return RedirectToAction("Profile", "Account"); // Or wherever you list events
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var image = await _context.EventImages.FindAsync(id);
        if (image == null)
            return Json(new { success = false, message = "Image not found." });

        // Delete file from wwwroot
        var filePath = Path.Combine(_environment.WebRootPath, image.ImageUrl.TrimStart('/').Replace("/", "\\"));
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        _context.EventImages.Remove(image);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }



}