using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public string TicketNumber { get; set; } = null!;

        public string QRCode { get; set; } = null!;

        public string Status { get; set; } = "valid";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
