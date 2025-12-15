using System.Collections.Generic;

namespace EventManagementSystem.Models.ViewModels
{
    public class ProfileViewModel
    {
        public User User { get; set; }
        public int LoyaltyPoints { get; set; }
        public List<LoyaltyHistory> LoyaltyHistory { get; set; } = new();
        public List<Booking> Bookings { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();

        // For organizer
        public string UserRole { get; set; } = "customer";
        public List<Event> OrganizerEvents { get; set; } = new List<Event>();
        public OrganizerProfileStats OrganizerStats { get; set; } = new OrganizerProfileStats();

        public EventCreateViewModel NewEvent { get; set; }
    }

    public class OrganizerProfileStats
    {
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int PastEvents { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalBookings { get; set; }
    }
}
