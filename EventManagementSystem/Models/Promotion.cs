using System;

namespace EventManagementSystem.Models
{
    public class Promotion
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DiscountType { get; set; } = "percentage"; // "percentage" or "fixed"
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "active"; // active / inactive
    }
}
