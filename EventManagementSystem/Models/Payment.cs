using System;

namespace EventManagementSystem.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }       // Foreign key to Booking
        public decimal Amount { get; set; }      // Payment amount
        public string PaymentMethod { get; set; } = "online";  // e.g., card, paypal
        public string? TransactionId { get; set; }              // Gateway transaction id
        public string Status { get; set; } = "pending";        // pending / completed / failed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Booking Booking { get; set; }
    }
}
