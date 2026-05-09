using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Pulls the current set of policies / records / schedules for an automobile and
    /// pushes the derived "current value" back into AutomobileInfo's denormalized fields.
    /// The dependent managers are resolved lazily via <see cref="IServiceProvider"/> to
    /// break the DI cycle (each manager depends on this updater, and this updater needs
    /// the managers to read history).
    /// </summary>
    public sealed class AutomobileSnapshotUpdater : IAutomobileSnapshotUpdater
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAutoEntityManager<AutomobileInfo> _autoManager;
        private readonly ILogger<AutomobileSnapshotUpdater> _logger;

        public AutomobileSnapshotUpdater(
            IServiceProvider serviceProvider,
            IAutoEntityManager<AutomobileInfo> autoManager,
            ILogger<AutomobileSnapshotUpdater> logger = null)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(autoManager);
            _serviceProvider = serviceProvider;
            _autoManager = autoManager;
            _logger = logger;
        }

        public async Task<ProcessingResult<bool>> RecomputeInsuranceSnapshotAsync(int automobileId)
        {
            var autoResult = await _autoManager.GetEntityByIdAsync(automobileId);
            if (!autoResult.Success || autoResult.Value == null)
            {
                _logger?.LogDebug("Skipping insurance snapshot recompute - automobile {Id} not found", automobileId);
                return ProcessingResult<bool>.Ok(false);
            }

            var policyManager = _serviceProvider.GetRequiredService<IAutoInsurancePolicyManager>();
            var policiesResult = await policyManager.GetByAutomobileAsync(automobileId);
            if (!policiesResult.Success)
            {
                return ProcessingResult<bool>.Fail(policiesResult.ErrorMessage, policiesResult.ErrorType);
            }

            var now = DateTime.UtcNow;
            var current = policiesResult.Value
                .Where(p => !p.IsDeleted && p.IsActive && p.EffectiveDate <= now && p.ExpiryDate > now)
                .OrderByDescending(p => p.EffectiveDate)
                .ThenByDescending(p => p.Id)
                .FirstOrDefault();

            var auto = autoResult.Value;
            if (current != null)
            {
                auto.InsuranceProvider = current.Provider;
                auto.InsurancePolicyNumber = current.PolicyNumber;
                auto.InsuranceExpiryDate = current.ExpiryDate;
            }
            else
            {
                auto.InsuranceProvider = null;
                auto.InsurancePolicyNumber = null;
                auto.InsuranceExpiryDate = null;
            }

            return await SaveAutomobileAsync(auto);
        }

        public async Task<ProcessingResult<bool>> RecomputeServiceSnapshotAsync(int automobileId)
        {
            var autoResult = await _autoManager.GetEntityByIdAsync(automobileId);
            if (!autoResult.Success || autoResult.Value == null)
            {
                _logger?.LogDebug("Skipping service snapshot recompute - automobile {Id} not found", automobileId);
                return ProcessingResult<bool>.Ok(false);
            }

            var recordManager = _serviceProvider.GetRequiredService<IServiceRecordManager>();
            var recordsResult = await recordManager.GetByAutomobileAsync(automobileId);
            if (!recordsResult.Success)
            {
                return ProcessingResult<bool>.Fail(recordsResult.ErrorMessage, recordsResult.ErrorType);
            }

            var latest = recordsResult.Value
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Mileage)
                .FirstOrDefault();

            var auto = autoResult.Value;
            if (latest != null)
            {
                auto.LastServiceDate = latest.Date;
                auto.LastServiceMeterReading = latest.Mileage;
            }
            else
            {
                auto.LastServiceDate = null;
                auto.LastServiceMeterReading = null;
            }

            return await SaveAutomobileAsync(auto);
        }

        public async Task<ProcessingResult<bool>> RecomputeScheduleSnapshotAsync(int automobileId)
        {
            var autoResult = await _autoManager.GetEntityByIdAsync(automobileId);
            if (!autoResult.Success || autoResult.Value == null)
            {
                _logger?.LogDebug("Skipping schedule snapshot recompute - automobile {Id} not found", automobileId);
                return ProcessingResult<bool>.Ok(false);
            }

            var scheduleManager = _serviceProvider.GetRequiredService<IAutoScheduledServiceManager>();
            var schedulesResult = await scheduleManager.GetByAutomobileAsync(automobileId);
            if (!schedulesResult.Success)
            {
                return ProcessingResult<bool>.Fail(schedulesResult.ErrorMessage, schedulesResult.ErrorType);
            }

            var soonest = schedulesResult.Value
                .Where(s => !s.IsDeleted && s.IsActive && s.NextDueDate.HasValue)
                .OrderBy(s => s.NextDueDate.Value)
                .FirstOrDefault();

            var auto = autoResult.Value;
            if (soonest != null)
            {
                auto.NextServiceDueDate = soonest.NextDueDate;
                auto.NextServiceDueMeterReading = soonest.NextDueMileage;
            }
            else
            {
                auto.NextServiceDueDate = null;
                auto.NextServiceDueMeterReading = null;
            }

            return await SaveAutomobileAsync(auto);
        }

        private async Task<ProcessingResult<bool>> SaveAutomobileAsync(AutomobileInfo auto)
        {
            var updateResult = await _autoManager.UpdateAsync(auto);
            if (!updateResult.Success)
            {
                _logger?.LogWarning("Failed to update automobile {Id} snapshot: {Error}", auto.Id, updateResult.ErrorMessage);
                return ProcessingResult<bool>.Fail(updateResult.ErrorMessage, updateResult.ErrorType);
            }
            return ProcessingResult<bool>.Ok(true);
        }
    }
}
