using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public User? Customer { get; set; }

        public int EventId { get; set; }
        public Event? Event { get; set; }

        public int NumberOfTickets { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TicketPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; }

        public string? PromotionCode { get; set; }

        public int LoyaltyUsed { get; set; }
        public int LoyaltyEarned { get; set; }

        public string PaymentStatus { get; set; } = "pending";
        public string BookingStatus { get; set; } = "pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Add Tickets navigation
        public List<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
