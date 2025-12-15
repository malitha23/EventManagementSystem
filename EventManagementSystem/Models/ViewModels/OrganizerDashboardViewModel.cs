using System;
using System.Collections.Generic;

namespace EventManagementSystem.Models.ViewModels
{
    public class OrganizerDashboardViewModel
    {
        public DashboardStats Stats { get; set; } = new DashboardStats();
        public List<EventSummary> UpcomingEvents { get; set; } = new List<EventSummary>();
        public List<BookingSummary> RecentBookings { get; set; } = new List<BookingSummary>();
        public List<TopEvent> TopEvents { get; set; } = new List<TopEvent>();
        public List<MonthlyRevenue> MonthlyRevenues { get; set; } = new List<MonthlyRevenue>();

        public EventCreateViewModel NewEvent { get; set; }
    }

    public class DashboardStats
    {
        public decimal TotalRevenue { get; set; } = 0;
        public int TotalBookings { get; set; } = 0;
        public int ActiveEvents { get; set; } = 0;
        public int TicketsSold { get; set; } = 0;
        public decimal PendingPayments { get; set; } = 0;
        public decimal AverageOccupancy { get; set; } = 0;
    }

    public class EventSummary
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime EventDate { get; set; }
        public int TotalCapacity { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
        public string OrganizerName { get; set; }
        public string VenueName { get; set; }
        public string CategoryName { get; set; }
        public string Status { get; set; }
    }

    public class BookingSummary
    {
        public int Id { get; set; }
        public string EventTitle { get; set; }
        public string CustomerName { get; set; }
        public int NumberOfTickets { get; set; }
        public decimal FinalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        public int BookingId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal DiscountApplied { get; set; }
        public DateTime BookingDate { get; set; }
    }

    public class TopEvent
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int BookingsCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal OccupancyRate { get; set; }

        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class MonthlyRevenue
    {
        public string Month { get; set; }
        public decimal Revenue { get; set; }

        public int Events { get; set; }
        public int Bookings { get; set; }
        public int Tickets { get; set; }
    }
}
