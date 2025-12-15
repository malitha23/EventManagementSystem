// Services/PromotionService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventManagementSystem.Data;
using EventManagementSystem.Models;
using EventManagementSystem.Models.ViewModels;

public interface IPromotionService
{
    Task<PromotionsViewModel> GetPromotions(PromotionFilterModel filter, int page = 1, int pageSize = 10);
    Task<PromotionViewModel> GetPromotionById(int id);
    Task<PromotionDetailsViewModel> GetPromotionDetails(int id);
    Task<bool> CreatePromotion(PromotionCreateViewModel model);
    Task<bool> UpdatePromotion(PromotionEditViewModel model);
    Task<bool> DeletePromotion(int id);
    Task<bool> TogglePromotionStatus(int id);
    Task<bool> IsPromotionCodeUnique(string code, int? excludeId = null);
    Task<List<string>> GetDiscountTypes();
    Task<List<string>> GetStatusOptions();
}

namespace EventManagementSystem.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PromotionService> _logger;

        public PromotionService(ApplicationDbContext context, ILogger<PromotionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PromotionsViewModel> GetPromotions(PromotionFilterModel filter, int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Promotions.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    query = query.Where(p =>
                        p.Code.Contains(filter.SearchTerm) ||
                        p.DiscountType.Contains(filter.SearchTerm));
                }

                if (!string.IsNullOrEmpty(filter.DiscountType))
                {
                    query = query.Where(p => p.DiscountType == filter.DiscountType);
                }

                if (!string.IsNullOrEmpty(filter.Status) && filter.Status != "all")
                {
                    if (filter.Status == "expired")
                        query = query.Where(p => p.EndDate < DateTime.UtcNow);
                    else
                        query = query.Where(p => p.Status == filter.Status);
                }


                if (filter.StartDate.HasValue)
                {
                    query = query.Where(p => p.StartDate >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(p => p.EndDate <= filter.EndDate.Value);
                }

                // Apply sorting
                query = filter.SortBy.ToLower() switch
                {
                    "code" => filter.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Code)
                        : query.OrderBy(p => p.Code),
                    "discountvalue" => filter.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.DiscountValue)
                        : query.OrderBy(p => p.DiscountValue),
                    "enddate" => filter.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.EndDate)
                        : query.OrderBy(p => p.EndDate),
                    _ => filter.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.StartDate)
                        : query.OrderBy(p => p.StartDate)
                };

                // Get total count
                var totalPromotions = await query.CountAsync();

                // Apply pagination
                var promotions = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PromotionViewModel
                    {
                        Id = p.Id,
                        Code = p.Code,
                        DiscountType = p.DiscountType,
                        DiscountValue = p.DiscountValue,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Status = p.Status
                    })
                    .ToListAsync();

                // Update status for expired promotions
                foreach (var promotion in promotions)
                {
                    if (promotion.EndDate < DateTime.UtcNow && promotion.Status == "active")
                    {
                        promotion.Status = "expired";
                    }
                }

                return new PromotionsViewModel
                {
                    Promotions = promotions,
                    Filter = filter ?? new PromotionFilterModel(), 
                    TotalPromotions = totalPromotions,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalPromotions / (double)pageSize),
                    DiscountTypes = await GetDiscountTypes(),
                    StatusOptions = await GetStatusOptions()
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting promotions");
                throw;
            }
        }

        public async Task<PromotionViewModel> GetPromotionById(int id)
        {
            try
            {
                var promotion = await _context.Promotions
                    .Where(p => p.Id == id)
                    .Select(p => new PromotionViewModel
                    {
                        Id = p.Id,
                        Code = p.Code,
                        DiscountType = p.DiscountType,
                        DiscountValue = p.DiscountValue,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Status = p.Status,
                        CreatedAt = p.CreatedAt,
                    })
                    .FirstOrDefaultAsync();

                return promotion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting promotion by ID: {Id}", id);
                throw;
            }
        }

        public async Task<PromotionDetailsViewModel> GetPromotionDetails(int id)
        {
            try
            {
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null)
                    return null;

                // Get promotion usage from bookings (assuming you have a Booking model)
                var recentUsage = await _context.Bookings
                    .Where(b => b.PromotionCode == promotion.Code)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(10)
                    .Select(b => new BookingSummary
                    {
                        BookingId = b.Id,
                        UserEmail = b.Customer.Email,
                        Amount = b.TotalAmount,
                        DiscountApplied = b.DiscountAmount,
                        BookingDate = b.CreatedAt
                    })
                    .ToListAsync();


                var uniqueUsers = await _context.Bookings
                    .Where(b => b.PromotionCode == promotion.Code)
                    .Select(b => b.CustomerId)
                    .Distinct()
                    .CountAsync();

                var daysRemaining = promotion.EndDate > DateTime.UtcNow
                    ? (promotion.EndDate - DateTime.UtcNow).Days
                    : 0;

                var isExpired = promotion.EndDate < DateTime.UtcNow;

                var viewModel = new PromotionDetailsViewModel
                {
                    Promotion = new PromotionViewModel
                    {
                        Id = promotion.Id,
                        Code = promotion.Code,
                        DiscountType = promotion.DiscountType,
                        DiscountValue = promotion.DiscountValue,
                        StartDate = promotion.StartDate,
                        EndDate = promotion.EndDate,
                        Status = promotion.Status,
                        CreatedAt = promotion.CreatedAt
                    },
                    RecentUsage = recentUsage,
                    Stats = new PromotionStats
                    {
                        UniqueUsers = uniqueUsers,
                        DaysRemaining = daysRemaining,
                        IsExpired = isExpired
                    }
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting promotion details for ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> CreatePromotion(PromotionCreateViewModel model)
        {
            try
            {
                // Validate dates
                if (model.EndDate <= model.StartDate)
                {
                    return false;
                }

                var promotion = new Promotion
                {
                    Code = model.Code.ToUpper(),
                    DiscountType = model.DiscountType,
                    DiscountValue = model.DiscountValue,
                    StartDate = model.StartDate.ToUniversalTime(),
                    EndDate = model.EndDate.ToUniversalTime(),
                    Status = model.Status
                };

                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promotion");
                return false;
            }
        }

        public async Task<bool> UpdatePromotion(PromotionEditViewModel model)
        {
            try
            {
                var promotion = await _context.Promotions.FindAsync(model.Id);
                if (promotion == null)
                    return false;

                // Validate dates
                if (model.EndDate <= model.StartDate)
                {
                    return false;
                }

                // Check if code is already used by another promotion
                if (await _context.Promotions.AnyAsync(p =>
                    p.Code == model.Code.ToUpper() && p.Id != model.Id))
                {
                    return false;
                }

                promotion.Code = model.Code.ToUpper();
                promotion.DiscountType = model.DiscountType;
                promotion.DiscountValue = model.DiscountValue;
                promotion.StartDate = model.StartDate.ToUniversalTime();
                promotion.EndDate = model.EndDate.ToUniversalTime();
                promotion.Status = model.Status;

                _context.Promotions.Update(promotion);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating promotion ID: {Id}", model.Id);
                return false;
            }
        }

        public async Task<bool> DeletePromotion(int id)
        {
            try
            {
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null)
                    return false;

                    _context.Promotions.Remove(promotion);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting promotion ID: {Id}", id);
                return false;
            }
        }

        public async Task<bool> TogglePromotionStatus(int id)
        {
            try
            {
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null)
                    return false;

                // Toggle between active and inactive
                promotion.Status = promotion.Status == "active" ? "inactive" : "active";
                promotion.UpdatedAt = DateTime.UtcNow;

                _context.Promotions.Update(promotion);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling promotion status ID: {Id}", id);
                return false;
            }
        }

        public async Task<bool> IsPromotionCodeUnique(string code, int? excludeId = null)
        {
            try
            {
                var query = _context.Promotions.Where(p => p.Code == code.ToUpper());

                if (excludeId.HasValue)
                {
                    query = query.Where(p => p.Id != excludeId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking promotion code uniqueness: {Code}", code);
                throw;
            }
        }

        public async Task<List<string>> GetDiscountTypes()
        {
            return new List<string> { "percentage", "fixed" };
        }

        public async Task<List<string>> GetStatusOptions()
        {
            return new List<string> { "active", "inactive", "expired" };
        }
    }
}