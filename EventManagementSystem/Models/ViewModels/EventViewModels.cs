
namespace EventManagementSystem.Models.ViewModels
{
    public class AdminEventsViewModel
    {
        public List<EventViewModel> Events { get; set; }
        public EventFilterModel Filter { get; set; }
        public int TotalEvents { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Organizers { get; set; }
    }

    public class EventViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal TicketPrice { get; set; }
        public string Status { get; set; }
        public int TotalCapacity { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal OccupancyRate { get; set; }
        public string CategoryName { get; set; }
        public string VenueName { get; set; }
        public string OrganizerName { get; set; }
        public string OrganizerEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MainImageUrl { get; set; }
        public int BookingCount { get; set; }
    }

    public class EventFilterModel
    {
        public string SearchTerm { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
        public string Organizer { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
    }
}