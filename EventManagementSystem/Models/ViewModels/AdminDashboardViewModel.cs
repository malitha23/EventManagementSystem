using System;
using System.Collections.Generic;

namespace EventManagementSystem.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public DashboardAdminStats Stats { get; set; }
        public List<MonthlyRevenue> MonthlyRevenues { get; set; }
        public List<TopEvent> TopEvents { get; set; }
        public List<EventSummary> UpcomingEvents { get; set; }
        public List<BookingSummary> RecentBookings { get; set; }
        public List<OrganizerSummary> TopOrganizers { get; set; }
        public EventCreateViewModel NewEvent { get; set; }
    }

    public class OrganizerSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int TotalEvents { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DashboardAdminStats
    {
        // Basic stats
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TicketsSold { get; set; }
        public int ActiveEvents { get; set; }
        public decimal PendingPayments { get; set; }
        public decimal AverageOccupancy { get; set; }

        // Admin-specific stats
        public int TotalUsers { get; set; }
        public int TotalOrganizers { get; set; }
        public decimal AverageBookingValue { get; set; }
        public int NewUsersThisMonth { get; set; }
        public decimal UserGrowthPercentage { get; set; }
        public int ActiveOrganizers { get; set; }
        public int OrganizersWithEvents { get; set; }
        public decimal TopOrganizerRevenue { get; set; }
        public int ConfirmedBookings { get; set; }
        public int PendingBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal BookingConfirmationRate { get; set; }
    }

}
