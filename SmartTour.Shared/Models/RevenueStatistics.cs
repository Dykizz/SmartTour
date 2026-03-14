using System;
using System.Collections.Generic;

namespace SmartTour.Shared.Models;

public class RevenueStatistics
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public int TotalSuccessTransactions { get; set; }
    public List<RevenueChartItem> ChartData { get; set; } = new();
    public List<PackageRevenueItem> PackageDistribution { get; set; } = new();
    public List<Payment> RecentTransactions { get; set; } = new();
}

public class RevenueChartItem
{
    public string Label { get; set; } // Can be "01/10" or "10/2025"
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
}

public class PackageRevenueItem
{
    public string PackageName { get; set; }
    public decimal Amount { get; set; }
    public int Count { get; set; }
}
