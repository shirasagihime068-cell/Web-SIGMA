using System.Threading;
using System.Threading.Tasks;

namespace PTK.SIGMA.Data.Dao;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}