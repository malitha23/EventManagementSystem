// Models/ViewModels/ReportViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models.ViewModels
{
    public class ReportsViewModel
    {
        public ReportFilterModel Filter { get; set; } = new();
        public ReportResults Results { get; set; } = new();
        public List<string> ReportTypes { get; set; } = new();
        public List<string> StatusOptions { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public List<string> Organizers { get; set; } = new();
    }

    public class ReportFilterModel
    {
        [Required]
        [Display(Name = "Report Type")]
        public string ReportType { get; set; } = "revenue";

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Category")]
        public string Category { get; set; }

        [Display(Name = "Organizer")]
        public string Organizer { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Format")]
        public string ExportFormat { get; set; } = "html";

        [Display(Name = "Include Details")]
        public bool IncludeDetails { get; set; } = true;

        [Display(Name = "Group By")]
        public string GroupBy { get; set; } = "month";
    }

    public class ReportResults
    {
        public string ReportType { get; set; }
        public string Title { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ReportSummary Summary { get; set; } = new();
        public List<ReportDataPoint> DataPoints { get; set; } = new();
        public List<ReportDetail> Details { get; set; } = new();
        public Dictionary<string, string> Charts { get; set; } = new();
    }

    public class ReportSummary
    {
        public decimal TotalRevenue { get; set; }
        public int TotalEvents { get; set; }
        public int TotalBookings { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalUsers { get; set; }
        public int TotalOrganizers { get; set; }
        public int TotalVenues { get; set; }
        public decimal AverageBookingValue { get; set; }
        public decimal AverageOccupancyRate { get; set; }
        public decimal ConversionRate { get; set; }
    }

    public class ReportDataPoint
    {
        public string Label { get; set; }
        public decimal Revenue { get; set; }
        public int Events { get; set; }
        public int Bookings { get; set; }
        public int Tickets { get; set; }
        public int Users { get; set; }
        public int Quantity { get; set; }

        public decimal Percentage { get; set; }
    }

    public class ReportDetail
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int Quantity { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string Organizer { get; set; }
    }

    public class RevenueReport
    {
        public List<MonthlyRevenue> MonthlyRevenues { get; set; } = new();
        public List<CategoryRevenue> CategoryRevenues { get; set; } = new();
        public List<OrganizerRevenue> OrganizerRevenues { get; set; } = new();
        public List<VenueRevenue> VenueRevenues { get; set; } = new();
    }

    public class CategoryRevenue
    {
        public string Category { get; set; }
        public int Events { get; set; }
        public int Bookings { get; set; }
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
    }

    public class OrganizerRevenue
    {
        public string Organizer { get; set; }
        public string Email { get; set; }
        public int Events { get; set; }
        public int Bookings { get; set; }
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
    }

    public class VenueRevenue
    {
        public string Venue { get; set; }
        public string Location { get; set; }
        public int Events { get; set; }
        public int Bookings { get; set; }
        public decimal Revenue { get; set; }
        public decimal OccupancyRate { get; set; }
    }

    public class BookingReport
    {
        public List<BookingTrend> Trends { get; set; } = new();
        public List<PaymentMethodSummary> PaymentMethods { get; set; } = new();
        public List<BookingStatusSummary> Statuses { get; set; } = new();
        public List<CustomerBookingSummary> TopCustomers { get; set; } = new();
    }

    public class BookingTrend
    {
        public string Period { get; set; }
        public int Bookings { get; set; }
        public int Tickets { get; set; }
        public decimal Revenue { get; set; }
    }

    public class PaymentMethodSummary
    {
        public string Method { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class BookingStatusSummary
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class CustomerBookingSummary
    {
        public string Customer { get; set; }
        public string Email { get; set; }
        public int Bookings { get; set; }
        public int Tickets { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastBooking { get; set; }
    }

    public class EventReport
    {
        public List<EventPerformance> Performances { get; set; } = new();
        public List<EventStatusSummary> Statuses { get; set; } = new();
        public List<CategoryPerformance> CategoryPerformances { get; set; } = new();
        public List<OrganizerPerformance> OrganizerPerformances { get; set; } = new();
    }

    public class EventPerformance
    {
        public string Event { get; set; }
        public string Organizer { get; set; }
        public string Category { get; set; }
        public DateTime Date { get; set; }
        public int Capacity { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal OccupancyRate { get; set; }
    }

    public class EventStatusSummary
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class CategoryPerformance
    {
        public string Category { get; set; }
        public int Events { get; set; }
        public decimal AverageOccupancy { get; set; }
        public decimal AverageRevenue { get; set; }
    }

    public class OrganizerPerformance
    {
        public string Organizer { get; set; }
        public int Events { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOccupancy { get; set; }
    }

    public class UserReport
    {
        public List<UserGrowth> Growth { get; set; } = new();
        public List<UserRoleSummary> Roles { get; set; } = new();
        public List<UserActivity> Activities { get; set; } = new();
        public List<UserLoyalty> Loyalty { get; set; } = new();
    }

    public class UserGrowth
    {
        public string Period { get; set; }
        public int NewUsers { get; set; }
        public int TotalUsers { get; set; }
        public decimal GrowthRate { get; set; }
    }

    public class UserRoleSummary
    {
        public string Role { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class UserActivity
    {
        public string User { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime Joined { get; set; }
        public int Bookings { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class UserLoyalty
    {
        public string User { get; set; }
        public string Email { get; set; }
        public int Points { get; set; }
        public int Bookings { get; set; }
        public decimal TotalSpent { get; set; }
        public string Tier { get; set; }
    }
}