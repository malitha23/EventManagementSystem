// Services/ReportService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventManagementSystem.Data;
using EventManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Globalization;

public interface IReportService
{
    Task<ReportsViewModel> GenerateReport(ReportFilterModel filter);
    Task<byte[]> ExportReport(ReportFilterModel filter, string format);
    Task<ReportSummary> GetReportSummary(DateTime? startDate, DateTime? endDate);
    Task<List<string>> GetReportTypes();
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportsViewModel> GenerateReport(ReportFilterModel filter)
    {
        var viewModel = new ReportsViewModel
        {
            Filter = filter,
            Results = new ReportResults
            {
                ReportType = filter.ReportType,
                Title = GetReportTitle(filter.ReportType),
                GeneratedAt = DateTime.Now
            }
        };

        // Set default dates if not provided
        if (!filter.StartDate.HasValue)
        {
            filter.StartDate = DateTime.Now.AddMonths(-6);
        }
        if (!filter.EndDate.HasValue)
        {
            filter.EndDate = DateTime.Now;
        }

        // Generate report based on type
        switch (filter.ReportType.ToLower())
        {
            case "revenue":
                await GenerateRevenueReport(viewModel, filter);
                break;
            case "bookings":
                await GenerateBookingReport(viewModel, filter);
                break;
            case "events":
                await GenerateEventReport(viewModel, filter);
                break;
            case "users":
                await GenerateUserReport(viewModel, filter);
                break;
            case "summary":
                await GenerateSummaryReport(viewModel, filter);
                break;
            default:
                await GenerateRevenueReport(viewModel, filter);
                break;
        }

        // Populate filter options
        viewModel.ReportTypes = await GetReportTypes();
        viewModel.Categories = await GetCategories();
        viewModel.Organizers = await GetOrganizers();
        viewModel.StatusOptions = await GetStatusOptions();

        return viewModel;
    }

    private async Task GenerateRevenueReport(ReportsViewModel viewModel, ReportFilterModel filter)
    {
        // Get summary data
        viewModel.Results.Summary = await GetReportSummary(filter.StartDate, filter.EndDate);

        // Get monthly revenue data
        var monthlyRevenues = await GetMonthlyRevenues(filter.StartDate.Value, filter.EndDate.Value);
        viewModel.Results.DataPoints = monthlyRevenues.Select(m => new ReportDataPoint
        {
            Label = m.Month,
            Revenue = m.Revenue,
            Events = m.Events,
            Bookings = m.Bookings,
            Tickets = m.Tickets,
            Percentage = viewModel.Results.Summary.TotalRevenue > 0 ?
                (m.Revenue / viewModel.Results.Summary.TotalRevenue) * 100 : 0
        }).ToList();

        // Get category revenue
        if (filter.IncludeDetails)
        {
            var categoryRevenues = await GetCategoryRevenues(filter.StartDate.Value, filter.EndDate.Value);
            viewModel.Results.Details = categoryRevenues.Select(c => new ReportDetail
            {
                Name = c.Category,
                Category = "Category",
                Amount = c.Revenue,
                Quantity = c.Events,
                Status = $"{c.Percentage:F1}%"
            }).ToList();
        }

        // Generate chart data
        viewModel.Results.Charts = GenerateRevenueCharts(monthlyRevenues);
    }

    private async Task GenerateBookingReport(ReportsViewModel viewModel, ReportFilterModel filter)
    {
        viewModel.Results.Summary = await GetReportSummary(filter.StartDate, filter.EndDate);

        // Get booking trends
        var trends = await GetBookingTrends(filter.StartDate.Value, filter.EndDate.Value, filter.GroupBy);
        viewModel.Results.DataPoints = trends.Select(t => new ReportDataPoint
        {
            Label = t.Period,
            Bookings = t.Bookings,
            Tickets = t.Tickets,
            Revenue = t.Revenue
        }).ToList();

        // Get payment methods
        if (filter.IncludeDetails)
        {
            var paymentMethods = await GetPaymentMethods(filter.StartDate.Value, filter.EndDate.Value);
            viewModel.Results.Details = paymentMethods.Select(p => new ReportDetail
            {
                Name = p.Method,
                Category = "Payment Method",
                Quantity = p.Count,
                Amount = p.Amount,
                Status = $"{p.Percentage:F1}%"
            }).ToList();
        }
    }

    private async Task GenerateEventReport(ReportsViewModel viewModel, ReportFilterModel filter)
    {
        viewModel.Results.Summary = await GetReportSummary(filter.StartDate, filter.EndDate);

        // Get event performances
        var performances = await GetEventPerformances(filter.StartDate.Value, filter.EndDate.Value, filter.Category, filter.Organizer);
        viewModel.Results.DataPoints = performances.Select(p => new ReportDataPoint
        {
            Label = p.Event,
            Revenue = p.Revenue,
            Quantity = p.TicketsSold,
            Percentage = p.OccupancyRate
        }).Take(20).ToList();

        // Get event statuses
        if (filter.IncludeDetails)
        {
            var statuses = await GetEventStatuses(filter.StartDate.Value, filter.EndDate.Value);
            viewModel.Results.Details = statuses.Select(s => new ReportDetail
            {
                Name = s.Status,
                Category = "Status",
                Quantity = s.Count,
                Amount = 0,
                Status = $"{s.Percentage:F1}%"
            }).ToList();
        }
    }

    private async Task GenerateUserReport(ReportsViewModel viewModel, ReportFilterModel filter)
    {
        viewModel.Results.Summary = await GetReportSummary(filter.StartDate, filter.EndDate);

        // Get user growth
        var growth = await GetUserGrowth(filter.StartDate.Value, filter.EndDate.Value, filter.GroupBy);
        viewModel.Results.DataPoints = growth.Select(g => new ReportDataPoint
        {
            Label = g.Period,
            Users = g.NewUsers,
            Percentage = g.GrowthRate
        }).ToList();

        // Get user activities
        if (filter.IncludeDetails)
        {
            var activities = await GetUserActivities(filter.StartDate.Value, filter.EndDate.Value);
            viewModel.Results.Details = activities.Select(a => new ReportDetail
            {
                Name = a.User,
                Category = a.Role,
                Quantity = a.Bookings,
                Amount = a.TotalSpent,
                Date = a.LastActivity
            }).Take(20).ToList();
        }
    }

    private async Task GenerateSummaryReport(ReportsViewModel viewModel, ReportFilterModel filter)
    {
        viewModel.Results.Summary = await GetReportSummary(filter.StartDate, filter.EndDate);

        // Get top performers
        var topEvents = await GetTopEvents(filter.StartDate.Value, filter.EndDate.Value, 10);
        viewModel.Results.DataPoints = topEvents.Select(e => new ReportDataPoint
        {
            Label = e.Title,
            Revenue = e.Revenue,
            Quantity = e.TicketsSold,
            Percentage = e.OccupancyRate
        }).ToList();

        // Get recent activities
        if (filter.IncludeDetails)
        {
            var recentBookings = await GetRecentBookings(filter.StartDate.Value, filter.EndDate.Value, 20);
            viewModel.Results.Details = recentBookings.Select(b => new ReportDetail
            {
                Name = b.EventTitle,
                Category = b.CustomerName,
                Quantity = b.NumberOfTickets,
                Amount = b.FinalAmount,
                Date = b.CreatedAt,
                Status = b.PaymentStatus
            }).ToList();
        }
    }

    private async Task<ReportSummary> GetReportSummary(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.Bookings.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(b => b.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(b => b.CreatedAt <= endDate.Value);
        }

        var paidBookings = await query.Where(b => b.PaymentStatus == "paid").ToListAsync();

        return new ReportSummary
        {
            TotalRevenue = paidBookings.Sum(b => b.FinalAmount),
            TotalBookings = await query.CountAsync(),
            TotalTicketsSold = await query.SumAsync(b => b.NumberOfTickets),
            TotalEvents = await _context.Events.CountAsync(),
            TotalUsers = await _context.Users.CountAsync(),
            TotalOrganizers = await _context.Users.CountAsync(u => u.Role == "organizer"),
            TotalVenues = await _context.Venues.CountAsync(),
            AverageBookingValue = paidBookings.Any() ? paidBookings.Average(b => b.FinalAmount) : 0,
            AverageOccupancyRate = await CalculateAverageOccupancy(startDate, endDate),
            ConversionRate = await CalculateConversionRate(startDate, endDate)
        };
    }

    private async Task<decimal> CalculateAverageOccupancy(DateTime? startDate, DateTime? endDate)
    {
        var events = await _context.Events.ToListAsync();
        decimal totalOccupancy = 0;
        int count = 0;

        foreach (var evt in events)
        {
            var ticketsSold = await _context.Bookings
                .Where(b => b.EventId == evt.Id)
                .SumAsync(b => b.NumberOfTickets);

            if (evt.TotalCapacity > 0)
            {
                totalOccupancy += ((decimal)ticketsSold / evt.TotalCapacity) * 100;
                count++;
            }
        }

        return count > 0 ? totalOccupancy / count : 0;
    }

    private async Task<decimal> CalculateConversionRate(DateTime? startDate, DateTime? endDate)
    {
        var totalUsers = await _context.Users.CountAsync();
        var usersWithBookings = await _context.Users
            .CountAsync(u => _context.Bookings.Any(b => b.CustomerId == u.Id));

        return totalUsers > 0 ? ((decimal)usersWithBookings / totalUsers) * 100 : 0;
    }

    private async Task<List<MonthlyRevenue>> GetMonthlyRevenues(DateTime startDate, DateTime endDate)
    {
        var months = new List<MonthlyRevenue>();
        var currentDate = new DateTime(startDate.Year, startDate.Month, 1);

        while (currentDate <= endDate)
        {
            var monthStart = currentDate;
            var monthEnd = currentDate.AddMonths(1).AddDays(-1);

            var bookings = await _context.Bookings
                .Where(b => b.CreatedAt >= monthStart && b.CreatedAt <= monthEnd && b.PaymentStatus == "paid")
                .ToListAsync();

            var events = await _context.Events
                .CountAsync(e => e.CreatedAt >= monthStart && e.CreatedAt <= monthEnd);

            months.Add(new MonthlyRevenue
            {
                Month = currentDate.ToString("MMM yyyy"),
                Revenue = bookings.Sum(b => b.FinalAmount),
                Events = events,
                Bookings = bookings.Count,
                Tickets = bookings.Sum(b => b.NumberOfTickets)
            });

            currentDate = currentDate.AddMonths(1);
        }

        return months;
    }

    private async Task<List<CategoryRevenue>> GetCategoryRevenues(DateTime startDate, DateTime endDate)
    {
        var result = await (from e in _context.Events
                            join c in _context.EventCategories on e.CategoryId equals c.Id
                            join b in _context.Bookings on e.Id equals b.EventId
                            where b.CreatedAt >= startDate && b.CreatedAt <= endDate && b.PaymentStatus == "paid"
                            group new { e, c, b } by new { c.Name } into g
                            select new CategoryRevenue
                            {
                                Category = g.Key.Name,
                                Events = g.Select(x => x.e.Id).Distinct().Count(),
                                Bookings = g.Count(),
                                Revenue = g.Sum(x => x.b.FinalAmount)
                            }).ToListAsync();

        var totalRevenue = result.Sum(r => r.Revenue);
        foreach (var item in result)
        {
            item.Percentage = totalRevenue > 0 ? (item.Revenue / totalRevenue) * 100 : 0;
        }

        return result.OrderByDescending(r => r.Revenue).ToList();
    }

    private async Task<List<BookingTrend>> GetBookingTrends(DateTime startDate, DateTime endDate, string groupBy)
    {
        var trends = new List<BookingTrend>();

        if (groupBy == "day")
        {
            var currentDate = startDate.Date;
            while (currentDate <= endDate.Date)
            {
                var nextDate = currentDate.AddDays(1);
                var bookings = await _context.Bookings
                    .Where(b => b.CreatedAt >= currentDate && b.CreatedAt < nextDate)
                    .ToListAsync();

                trends.Add(new BookingTrend
                {
                    Period = currentDate.ToString("MMM dd"),
                    Bookings = bookings.Count,
                    Tickets = bookings.Sum(b => b.NumberOfTickets),
                    Revenue = bookings.Where(b => b.PaymentStatus == "paid").Sum(b => b.FinalAmount)
                });

                currentDate = nextDate;
            }
        }
        else if (groupBy == "week")
        {
            var currentDate = startDate.Date;
            while (currentDate <= endDate.Date)
            {
                var weekEnd = currentDate.AddDays(7);
                var bookings = await _context.Bookings
                    .Where(b => b.CreatedAt >= currentDate && b.CreatedAt < weekEnd)
                    .ToListAsync();

                trends.Add(new BookingTrend
                {
                    Period = $"Week {CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(currentDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}",
                    Bookings = bookings.Count,
                    Tickets = bookings.Sum(b => b.NumberOfTickets),
                    Revenue = bookings.Where(b => b.PaymentStatus == "paid").Sum(b => b.FinalAmount)
                });

                currentDate = weekEnd;
            }
        }
        else // month
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
            while (currentDate <= endDate)
            {
                var monthEnd = currentDate.AddMonths(1);
                var bookings = await _context.Bookings
                    .Where(b => b.CreatedAt >= currentDate && b.CreatedAt < monthEnd)
                    .ToListAsync();

                trends.Add(new BookingTrend
                {
                    Period = currentDate.ToString("MMM yyyy"),
                    Bookings = bookings.Count,
                    Tickets = bookings.Sum(b => b.NumberOfTickets),
                    Revenue = bookings.Where(b => b.PaymentStatus == "paid").Sum(b => b.FinalAmount)
                });

                currentDate = monthEnd;
            }
        }

        return trends;
    }

    private async Task<List<PaymentMethodSummary>> GetPaymentMethods(DateTime startDate, DateTime endDate)
    {
        // This is a placeholder - you'll need to add PaymentMethod to your Booking model
        // For now, return empty list
        return new List<PaymentMethodSummary>();
    }

    private async Task<List<EventPerformance>> GetEventPerformances(DateTime startDate, DateTime endDate, string category, string organizer)
    {
        var query = from e in _context.Events
                    join c in _context.EventCategories on e.CategoryId equals c.Id
                    join o in _context.Users on e.OrganizerId equals o.Id
                    where e.EventDate >= startDate && e.EventDate <= endDate
                    select new { e, c, o };

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(x => x.c.Name == category);
        }
        if (!string.IsNullOrEmpty(organizer))
        {
            query = query.Where(x => x.o.Name.Contains(organizer) || x.o.Email.Contains(organizer));
        }

        var events = await query.ToListAsync();
        var performances = new List<EventPerformance>();

        foreach (var item in events)
        {
            var bookings = await _context.Bookings
                .Where(b => b.EventId == item.e.Id && b.PaymentStatus == "paid")
                .ToListAsync();

            performances.Add(new EventPerformance
            {
                Event = item.e.Title,
                Organizer = item.o.Name,
                Category = item.c.Name,
                Date = item.e.EventDate,
                Capacity = item.e.TotalCapacity,
                TicketsSold = bookings.Sum(b => b.NumberOfTickets),
                Revenue = bookings.Sum(b => b.FinalAmount),
                OccupancyRate = item.e.TotalCapacity > 0 ?
                    (bookings.Sum(b => b.NumberOfTickets) * 100m) / item.e.TotalCapacity : 0
            });
        }

        return performances.OrderByDescending(p => p.Revenue).ToList();
    }

    private async Task<List<EventStatusSummary>> GetEventStatuses(DateTime startDate, DateTime endDate)
    {
        var statuses = await _context.Events
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .GroupBy(e => e.Status)
            .Select(g => new EventStatusSummary
            {
                Status = g.Key,
                Count = g.Count()
            }).ToListAsync();

        var total = statuses.Sum(s => s.Count);
        foreach (var status in statuses)
        {
            status.Percentage = total > 0 ? (status.Count * 100m) / total : 0;
        }

        return statuses;
    }

    private async Task<List<UserGrowth>> GetUserGrowth(DateTime startDate, DateTime endDate, string groupBy)
    {
        var growth = new List<UserGrowth>();
        var currentDate = startDate.Date;
        var previousTotal = 0;

        while (currentDate <= endDate.Date)
        {
            DateTime nextDate;
            string period;

            if (groupBy == "day")
            {
                nextDate = currentDate.AddDays(1);
                period = currentDate.ToString("MMM dd");
            }
            else if (groupBy == "week")
            {
                nextDate = currentDate.AddDays(7);
                period = $"Week {CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(currentDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}";
            }
            else
            {
                nextDate = currentDate.AddMonths(1);
                period = currentDate.ToString("MMM yyyy");
            }

            var newUsers = await _context.Users
                .CountAsync(u => u.CreatedAt >= currentDate && u.CreatedAt < nextDate);

            var totalUsers = await _context.Users
                .CountAsync(u => u.CreatedAt < nextDate);

            growth.Add(new UserGrowth
            {
                Period = period,
                NewUsers = newUsers,
                TotalUsers = totalUsers,
                GrowthRate = previousTotal > 0 ? ((decimal)(totalUsers - previousTotal) / previousTotal) * 100 : 100
            });

            previousTotal = totalUsers;
            currentDate = nextDate;
        }

        return growth;
    }

    private async Task<List<UserActivity>> GetUserActivities(DateTime startDate, DateTime endDate)
    {
        var activities = await (from u in _context.Users
                                select new UserActivity
                                {
                                    User = u.Name,
                                    Email = u.Email,
                                    Role = u.Role,
                                    Joined = u.CreatedAt,
                                    Bookings = _context.Bookings.Count(b => b.CustomerId == u.Id),
                                    TotalSpent = _context.Bookings
                                        .Where(b => b.CustomerId == u.Id && b.PaymentStatus == "paid")
                                        .Sum(b => (decimal?)b.FinalAmount) ?? 0,
                                    LastActivity = _context.Bookings
                                        .Where(b => b.CustomerId == u.Id)
                                        .Max(b => (DateTime?)b.CreatedAt) ?? u.CreatedAt
                                }).ToListAsync();

        return activities.OrderByDescending(a => a.LastActivity).Take(50).ToList();
    }

    private async Task<List<TopEvent>> GetTopEvents(DateTime startDate, DateTime endDate, int count)
    {
        return await _context.Events
            .Where(e => e.EventDate >= startDate && e.EventDate <= endDate)
            .Select(e => new TopEvent
            {
                Id = e.Id,
                Title = e.Title,
                BookingsCount = _context.Bookings.Count(b => b.EventId == e.Id),
                TotalRevenue = _context.Bookings
                    .Where(b => b.EventId == e.Id && b.PaymentStatus == "paid")
                    .Sum(b => (decimal?)b.FinalAmount) ?? 0,
                OccupancyRate = e.TotalCapacity == 0 ? 0 :
                    (_context.Bookings
                        .Where(b => b.EventId == e.Id)
                        .Sum(b => (decimal?)b.NumberOfTickets) ?? 0) / e.TotalCapacity * 100
            })
            .OrderByDescending(e => e.TotalRevenue)
            .Take(count)
            .ToListAsync();
    }

    private async Task<List<BookingSummary>> GetRecentBookings(DateTime startDate, DateTime endDate, int count)
    {
        return await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Event)
            .Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate)
            .OrderByDescending(b => b.CreatedAt)
            .Take(count)
            .Select(b => new BookingSummary
            {
                Id = b.Id,
                EventTitle = b.Event.Title,
                CustomerName = b.Customer.Name,
                NumberOfTickets = b.NumberOfTickets,
                FinalAmount = b.FinalAmount,
                PaymentStatus = b.PaymentStatus,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();
    }

    private Dictionary<string, string> GenerateRevenueCharts(List<MonthlyRevenue> monthlyRevenues)
    {
        var charts = new Dictionary<string, string>();

        // Generate simple chart data for revenue trend
        if (monthlyRevenues.Any())
        {
            var labels = string.Join(",", monthlyRevenues.Select(m => $"'{m.Month}'"));
            var data = string.Join(",", monthlyRevenues.Select(m => m.Revenue));

            charts.Add("revenueTrend", $"{{\"labels\": [{labels}], \"data\": [{data}]}}");
        }

        return charts;
    }

    private string GetReportTitle(string reportType)
    {
        return reportType.ToLower() switch
        {
            "revenue" => "Revenue Analysis Report",
            "bookings" => "Bookings Analysis Report",
            "events" => "Events Performance Report",
            "users" => "Users Activity Report",
            "summary" => "System Summary Report",
            _ => "Analytical Report"
        };
    }

    public async Task<byte[]> ExportReport(ReportFilterModel filter, string format)
    {
        var report = await GenerateReport(filter);

        if (format.ToLower() == "csv")
        {
            return GenerateCsv(report);
        }
        else if (format.ToLower() == "pdf")
        {
            // You'll need a PDF library like iTextSharp or QuestPDF for this
            // For now, return CSV
            return GenerateCsv(report);
        }
        else
        {
            return GenerateCsv(report);
        }
    }

    private byte[] GenerateCsv(ReportsViewModel report)
    {
        var csv = new StringBuilder();

        // Add header
        csv.AppendLine($"Report: {report.Results.Title}");
        csv.AppendLine($"Generated: {report.Results.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine($"Period: {report.Filter.StartDate:yyyy-MM-dd} to {report.Filter.EndDate:yyyy-MM-dd}");
        csv.AppendLine();

        // Add summary
        csv.AppendLine("SUMMARY");
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Total Revenue,{report.Results.Summary.TotalRevenue}");
        csv.AppendLine($"Total Events,{report.Results.Summary.TotalEvents}");
        csv.AppendLine($"Total Bookings,{report.Results.Summary.TotalBookings}");
        csv.AppendLine($"Total Tickets Sold,{report.Results.Summary.TotalTicketsSold}");
        csv.AppendLine($"Total Users,{report.Results.Summary.TotalUsers}");
        csv.AppendLine($"Total Organizers,{report.Results.Summary.TotalOrganizers}");
        csv.AppendLine($"Average Booking Value,{report.Results.Summary.AverageBookingValue}");
        csv.AppendLine($"Average Occupancy Rate,{report.Results.Summary.AverageOccupancyRate}%");
        csv.AppendLine($"Conversion Rate,{report.Results.Summary.ConversionRate}%");
        csv.AppendLine();

        // Add data points
        if (report.Results.DataPoints.Any())
        {
            csv.AppendLine("DETAILED DATA");
            csv.AppendLine("Label,Revenue,Events,Bookings,Tickets,Percentage");
            foreach (var point in report.Results.DataPoints)
            {
                csv.AppendLine($"\"{point.Label}\",{point.Revenue},{point.Events},{point.Bookings},{point.Tickets},{point.Percentage:F2}%");
            }
            csv.AppendLine();
        }

        // Add details
        if (report.Results.Details.Any())
        {
            csv.AppendLine("DETAILS");
            csv.AppendLine("Name,Category,Amount,Quantity,Status,Date");
            foreach (var detail in report.Results.Details)
            {
                csv.AppendLine($"\"{detail.Name}\",{detail.Category},{detail.Amount},{detail.Quantity},{detail.Status},{detail.Date:yyyy-MM-dd}");
            }
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<List<string>> GetReportTypes()
    {
        return new List<string>
        {
            "revenue",
            "bookings",
            "events",
            "users",
            "summary"
        };
    }

    private async Task<List<string>> GetCategories()
    {
        return await _context.EventCategories
            .Select(c => c.Name)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    private async Task<List<string>> GetOrganizers()
    {
        return await _context.Users
            .Where(u => u.Role == "organizer")
            .Select(u => u.Name)
            .Distinct()
            .OrderBy(o => o)
            .ToListAsync();
    }

    private async Task<List<string>> GetStatusOptions()
    {
        return await _context.Events
            .Select(e => e.Status)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }

    Task<ReportSummary> IReportService.GetReportSummary(DateTime? startDate, DateTime? endDate)
    {
        throw new NotImplementedException();
    }
}