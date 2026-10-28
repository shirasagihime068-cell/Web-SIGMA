using System.Threading.Tasks;

namespace PTK.SIGMA.Domain.SigmaService;

public interface IHomeService
{
    Task<object> GetSummaryAsync(
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<object> GetDashboardAsync(
        string? search,
        string? category,
        string? filter,
        int page,
        int pageSize,
        string? sort,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<object> GetInventoryAsync(
        string? search,
        string? category,
        string? filter,
        int page,
        int pageSize,
        string? sort,
        CancellationToken cancellationToken = default);
}
