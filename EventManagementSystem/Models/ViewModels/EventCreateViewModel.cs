using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EventManagementSystem.Models.ViewModels
{
    public class EventCreateViewModel
    {
        // Event fields
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int CategoryId { get; set; }

        // Venue selection
        public int? VenueId { get; set; }

        // New venue fields
        public string? NewVenueName { get; set; }
        public string? NewVenueLocation { get; set; }
        public string? NewVenueDescription { get; set; }
        public int? NewVenueCapacity { get; set; } // ✅ ADD THIS

        // Event fields
        public int TotalCapacity { get; set; }
        public decimal TicketPrice { get; set; }
        public bool PublishNow { get; set; }

        public List<IFormFile>? EventImages { get; set; }
    }


}