using SmartTour.Shared.Models;

namespace SmartTour.API.Interfaces;

public interface IRevenueService
{
    Task<RevenueStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<Payment>> GetPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null, int count = 100);
}
