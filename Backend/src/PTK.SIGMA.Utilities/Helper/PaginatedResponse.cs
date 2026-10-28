using System.Collections.Generic;

namespace PTK.SIGMA.Utilities.Helper;

public sealed class PaginatedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
