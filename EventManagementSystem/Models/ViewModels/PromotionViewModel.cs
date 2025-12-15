// Models/ViewModels/PromotionViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models.ViewModels
{
    public class PromotionsViewModel
    {
        public List<PromotionViewModel> Promotions { get; set; } = new();
        public PromotionFilterModel Filter { get; set; } = new();
        public int TotalPromotions { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public List<string> DiscountTypes { get; set; } = new();
        public List<string> StatusOptions { get; set; } = new();
    }

    public class PromotionViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Promotion Code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Discount Type")]
        public string DiscountType { get; set; } = "percentage"; // "percentage" or "fixed"

        [Required]
        [Display(Name = "Discount Value")]
        public decimal DiscountValue { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "active";

        [Display(Name = "Created")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Times Used")]
        public int UsageCount { get; set; }

        [Display(Name = "Total Discount")]
        public decimal TotalDiscountGiven { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive => Status == "active" && StartDate <= DateTime.Now && EndDate >= DateTime.Now;
    }

    public class PromotionCreateViewModel
    {
        [Required]
        [StringLength(50, ErrorMessage = "Code must be between 1 and 50 characters.")]
        [Display(Name = "Promotion Code")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Code can only contain uppercase letters and numbers.")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Discount Type")]
        public string DiscountType { get; set; } = "percentage";

        [Required]
        [Display(Name = "Discount Value")]
        [Range(0.01, 100000, ErrorMessage = "Discount value must be greater than 0.")]
        public decimal DiscountValue { get; set; } = 10;

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        [DateGreaterThan("StartDate", ErrorMessage = "End date must be after start date.")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Status")]
        public string Status { get; set; } = "active";

        [Display(Name = "Max Usage (Optional)")]
        [Range(1, 100000, ErrorMessage = "Max usage must be at least 1 if specified.")]
        public int? MaxUsage { get; set; }

        [Display(Name = "Minimum Booking Amount (Optional)")]
        [Range(0, 1000000, ErrorMessage = "Minimum amount must be 0 or more.")]
        public decimal? MinBookingAmount { get; set; }
    }

    public class PromotionEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Code must be between 1 and 50 characters.")]
        [Display(Name = "Promotion Code")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Code can only contain uppercase letters and numbers.")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Discount Type")]
        public string DiscountType { get; set; } = "percentage";

        [Required]
        [Display(Name = "Discount Value")]
        [Range(0.01, 100000, ErrorMessage = "Discount value must be greater than 0.")]
        public decimal DiscountValue { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        [DateGreaterThan("StartDate", ErrorMessage = "End date must be after start date.")]
        public DateTime EndDate { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "active";
    }

    public class PromotionFilterModel
    {
        public string SearchTerm { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string SortBy { get; set; } = "StartDate";
        public string SortOrder { get; set; } = "desc";
    }

    public class PromotionDetailsViewModel
    {
        public PromotionViewModel Promotion { get; set; } = new();
        public List<BookingSummary> RecentUsage { get; set; } = new();
        public PromotionStats Stats { get; set; } = new();
    }

    public class PromotionStats
    {
        public int TotalUsage { get; set; }
        public decimal TotalDiscountGiven { get; set; }
        public decimal AverageDiscountPerUse { get; set; }
        public int UniqueUsers { get; set; }
        public decimal ConversionRate { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsExpired { get; set; }
    }

    // Custom validation attribute for date comparison
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateGreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var currentValue = (DateTime)value;
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (property == null)
                throw new ArgumentException($"Property {_comparisonProperty} not found");

            var comparisonValue = (DateTime)property.GetValue(validationContext.ObjectInstance);

            if (currentValue <= comparisonValue)
                return new ValidationResult(ErrorMessage ?? "End date must be after start date");

            return ValidationResult.Success;
        }
    }
}