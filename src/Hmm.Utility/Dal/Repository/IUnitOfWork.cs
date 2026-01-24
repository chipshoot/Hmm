using System.Threading;
using System.Threading.Tasks;

namespace Hmm.Utility.Dal.Repository
{
    /// <summary>
    /// Unit of Work pattern interface for managing transaction boundaries.
    /// Allows multiple repository operations to be grouped into a single atomic transaction.
    /// </summary>
    /// <remarks>
    /// This interface enables proper transaction management by separating the commit operation
    /// from individual repository operations. Repositories track changes but don't save them;
    /// the Unit of Work controls when all pending changes are persisted.
    /// </remarks>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Commits all pending changes to the data source synchronously.
        /// </summary>
        /// <returns>The number of state entries written to the data source.</returns>
        int Commit();

        /// <summary>
        /// Commits all pending changes to the data source asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation, with the number of state entries written.</returns>
        Task<int> CommitAsync(CancellationToken cancellationToken = default);
    }
}
