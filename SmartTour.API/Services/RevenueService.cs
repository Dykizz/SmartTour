using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;

namespace SmartTour.API.Services;

public class RevenueService : IRevenueService
{
    private readonly AppDbContext _context;

    public RevenueService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RevenueStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfToday = now.Date;

        var query = _context.Payments.Where(p => p.Status == "Success");

        if (startDate.HasValue)
            query = query.Where(p => p.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(p => p.CreatedAt <= endDate.Value);

        var successPayments = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var stats = new RevenueStatistics
        {
            TotalRevenue = successPayments.Sum(p => p.Amount),
            TotalSuccessTransactions = successPayments.Count,
            // These remain focused on "Today" and "Monthly" as KPIs, but could be adjusted if needed.
            // For now, let's keep them as fixed KPIs and use TotalRevenue for the filtered range.
            TodayRevenue = successPayments.Where(p => p.CreatedAt >= startOfToday).Sum(p => p.Amount),
            MonthlyRevenue = successPayments.Where(p => p.CreatedAt >= startOfMonth).Sum(p => p.Amount),
            RecentTransactions = successPayments.Take(10).ToList()
        };

        // Generate Chart Data based on range
        stats.ChartData.Clear();
        
        DateTime chartStart = startDate ?? now.AddMonths(-6);
        DateTime chartEnd = endDate ?? now;
        
        // Ensure chartEnd is at least today if not specified
        if (!endDate.HasValue && chartEnd < now) chartEnd = now;

        double totalDays = (chartEnd - chartStart).TotalDays;

        if (totalDays <= 31)
        {
            // Daily breakdown
            for (var dt = chartStart.Date; dt <= chartEnd.Date; dt = dt.AddDays(1))
            {
                var nextDay = dt.AddDays(1);
                var dayPayments = successPayments.Where(p => p.CreatedAt >= dt && p.CreatedAt < nextDay).ToList();
                stats.ChartData.Add(new RevenueChartItem
                {
                    Label = dt.ToString("dd/MM"),
                    Amount = dayPayments.Sum(p => p.Amount),
                    TransactionCount = dayPayments.Count
                });
            }
        }
        else
        {
            // Monthly breakdown (Last 6 months OR selected range)
            int monthsToShow = totalDays > 180 ? (int)Math.Ceiling(totalDays / 30) : 6;
            if (monthsToShow > 12) monthsToShow = 12; // Cap at 12 months for chart

            for (int i = monthsToShow - 1; i >= 0; i--)
            {
                var monthDate = chartEnd.AddMonths(-i);
                var mStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var mEnd = mStart.AddMonths(1);
                var monthPayments = successPayments.Where(p => p.CreatedAt >= mStart && p.CreatedAt < mEnd).ToList();

                stats.ChartData.Add(new RevenueChartItem
                {
                    Label = monthDate.ToString("MM/yyyy"),
                    Amount = monthPayments.Sum(p => p.Amount),
                    TransactionCount = monthPayments.Count
                });
            }
        }

        // Package Distribution
        var packages = await _context.ServicePackages.ToListAsync();
        stats.PackageDistribution = successPayments
            .GroupBy(p => p.PackageCode)
            .Select(g => new PackageRevenueItem
            {
                PackageName = packages.FirstOrDefault(pk => pk.Code == g.Key)?.Name ?? g.Key,
                Amount = g.Sum(p => p.Amount),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return stats;
    }

    public async Task<PagedResponse<Payment>> GetPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Payments
            .Include(p => p.User)
            .Where(p => p.Status == "Success");

        if (startDate.HasValue)
            query = query.Where(p => p.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(p => p.CreatedAt <= endDate.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<Payment>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
