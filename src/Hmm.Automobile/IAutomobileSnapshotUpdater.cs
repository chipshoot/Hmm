using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Recomputes the denormalized insurance/service snapshot fields on AutomobileInfo
    /// (InsuranceProvider/PolicyNumber/ExpiryDate, LastServiceDate/MeterReading,
    /// NextServiceDueDate/MeterReading) from the source-of-truth entities. Called by the
    /// AutoInsurancePolicy / ServiceRecord / AutoScheduledService managers after a write
    /// so the Flutter client (which reads the snapshot fields) stays consistent without
    /// any client change.
    /// </summary>
    public interface IAutomobileSnapshotUpdater
    {
        Task<ProcessingResult<bool>> RecomputeInsuranceSnapshotAsync(int automobileId);

        Task<ProcessingResult<bool>> RecomputeServiceSnapshotAsync(int automobileId);

        Task<ProcessingResult<bool>> RecomputeScheduleSnapshotAsync(int automobileId);
    }
}
