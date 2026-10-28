using System.Threading;
using System.Threading.Tasks;
using PTK.SIGMA.Data.Context;

namespace PTK.SIGMA.Data.Dao;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}