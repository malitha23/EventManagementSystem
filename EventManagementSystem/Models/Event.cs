using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("event_date")]
        public DateTime EventDate { get; set; }

        [Column("start_time")]
        public TimeSpan StartTime { get; set; }

        [Column("end_time")]
        public TimeSpan EndTime { get; set; }

        [Column("ticket_price")]
        public decimal TicketPrice { get; set; }

        [Column("status")]
        public string Status { get; set; } = "upcoming";

        [Column("total_capacity")]
        public int TotalCapacity { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Snake case for FK columns
        [Column("category_id")]
        public int CategoryId { get; set; }
        public EventCategory? Category { get; set; }

        [Column("venue_id")]
        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        [Column("organizer_id")]
        public int OrganizerId { get; set; }
        public List<EventImage> EventImages { get; set; } = new List<EventImage>();

        public List<Booking> Bookings { get; set; } = new();

        public virtual User Organizer { get; set; }

    }

    public class EventImage
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public Event? Event { get; set; }
    }

    public class EventCategory
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class Venue
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("location")]
        public string Location { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("capacity")]
        public int Capacity { get; set; }   // ✅ FIXED
    }

}
