// Models/ViewModels/EventCategoryViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models.ViewModels
{
    public class EventCategoriesViewModel
    {
        public List<EventCategoryViewModel> Categories { get; set; } = new();
        public CategoryFilterModel Filter { get; set; } = new();
        public int TotalCategories { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
    }

    public class EventCategoryViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Total Events")]
        public int EventsCount { get; set; }

        [Display(Name = "Total Bookings")]
        public int BookingsCount { get; set; }

        [Display(Name = "Total Revenue")]
        public decimal TotalRevenue { get; set; }

        [Display(Name = "Created")]
        public DateTime? CreatedAt { get; set; }
    }

    public class EventCategoryCreateViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "Category name must be between 1 and 100 characters.")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;
    }

    public class EventCategoryEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Category name must be between 1 and 100 characters.")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;
    }

    public class CategoryFilterModel
    {
        public string SearchTerm { get; set; } = string.Empty;
        public string SortBy { get; set; } = "Name";
        public string SortOrder { get; set; } = "asc";
    }

    public class CategoryDetailsViewModel
    {
        public EventCategoryViewModel Category { get; set; } = new();
        public List<EventSummary> UpcomingEvents { get; set; } = new();
        public List<EventSummary> PastEvents { get; set; } = new();
        public CategoryStats Stats { get; set; } = new();
    }

    public class CategoryStats
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