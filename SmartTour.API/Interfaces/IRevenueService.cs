using SmartTour.Shared.Models;

namespace SmartTour.API.Interfaces;

public interface IRevenueService
{
    Task<RevenueStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<PagedResponse<Payment>> GetPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null, int pageNumber = 1, int pageSize = 10);
}
