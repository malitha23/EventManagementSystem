// Models/ViewModels/VenueViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models.ViewModels
{
    public class VenuesViewModel
    {
        public List<VenueViewModel> Venues { get; set; } = new();
        public VenueFilterModel Filter { get; set; } = new();
        public int TotalVenues { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public List<string> Locations { get; set; } = new();
    }

    public class VenueViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Venue Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Capacity")]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1")]
        public int Capacity { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Events Count")]
        public int EventsCount { get; set; }

        [Display(Name = "Total Revenue")]
        public decimal TotalRevenue { get; set; }

        [Display(Name = "Occupancy Rate")]
        public decimal OccupancyRate { get; set; }
    }

    public class VenueCreateViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Venue Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Range(1, 100000, ErrorMessage = "Capacity must be between 1 and 100,000")]
        [Display(Name = "Capacity")]
        public int Capacity { get; set; } = 100;
    }

    public class VenueEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Venue Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Range(1, 100000, ErrorMessage = "Capacity must be between 1 and 100,000")]
        [Display(Name = "Capacity")]
        public int Capacity { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;
    }

    public class VenueFilterModel
    {
        public string SearchTerm { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int? MinCapacity { get; set; }
        public int? MaxCapacity { get; set; }
        public bool? IsActive { get; set; }
        public string SortBy { get; set; } = "Name";
        public string SortOrder { get; set; } = "asc";
    }

    public class VenueDetailsViewModel
    {
        public VenueViewModel Venue { get; set; } = new();
        public List<EventSummary> UpcomingEvents { get; set; } = new();
        public List<EventSummary> PastEvents { get; set; } = new();
        public VenueStats Stats { get; set; } = new();
    }

    public class VenueStats
    {
        public int TotalEvents { get; set; }
        public int TotalBookings { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOccupancy { get; set; }
        public int UpcomingEventsCount { get; set; }
        public int PastEventsCount { get; set; }
    }
}