using System;

namespace EventManagementSystem.Models
{
    public class LoyaltyPoint
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int Points { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class LoyaltyHistory
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public int? BookingId { get; set; }

        public string ChangeType { get; set; } = "earn"; // 'earn' or 'use'

        public int Points { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
