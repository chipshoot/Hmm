using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager
{
    public class SubsystemManager : ISubsystemManager
    {
        private readonly IRepository<Subsystem> _dataSource;
        private readonly IHmmValidator<Subsystem> _validator;

        public SubsystemManager(
            IRepository<Subsystem> dataSource,
            IHmmValidator<Subsystem> validator)
        {
            Guard.Against<ArgumentNullException>(dataSource == null, nameof(dataSource));
            Guard.Against<ArgumentNullException>(validator == null, nameof(validator));

            _dataSource = dataSource;
            _validator = validator;
        }

        public Subsystem Create(Subsystem system)
        {
            if (!_validator.IsValidEntity(system, ProcessResult))
            {
                return null;
            }

            try
            {
                if (HasApplicationRegistered(system))
                {
                    ProcessResult.AddErrorMessage($"The application {system.Name} is existed in system");
                    return null;
                }

                // Add default author to system
                if (system.DefaultAuthor == null)
                {
                    ProcessResult.AddErrorMessage($"The application {system.Name}'s default author is null or invalid");
                    return null;
                }

                // Add sub system to system
                var addedSystem = _dataSource.Add(system);
                if (addedSystem == null)
                {
                    ProcessResult.PropagandaResult(_dataSource.ProcessMessage);
                }
                return addedSystem;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<Subsystem> CreateAsync(Subsystem system)
        {
            if (!_validator.IsValidEntity(system, ProcessResult))
            {
                return null;
            }

            try
            {
                if (HasApplicationRegistered(system))
                {
                    ProcessResult.AddErrorMessage($"The application {system.Name} is existed in system");
                    return null;
                }

                // Add default author to system
                if (system.DefaultAuthor == null)
                {
                    ProcessResult.AddErrorMessage($"The application {system.Name}'s default author is null or invalid");
                    return null;
                }

                // Add sub system to system
                var addedSystem = await _dataSource.AddAsync(system);
                if (addedSystem == null)
                {
                    ProcessResult.PropagandaResult(_dataSource.ProcessMessage);
                }
                return addedSystem;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public Subsystem Update(Subsystem system)
        {
            if (system == null || !_validator.IsValidEntity(system, ProcessResult))
            {
                return null;
            }

            var updatedSys = _dataSource.Update(system);
            if (updatedSys == null)
            {
                ProcessResult.PropagandaResult(_dataSource.ProcessMessage);
            }

            return updatedSys;
        }

        public async Task<Subsystem> UpdateAsync(Subsystem system)
        {
            if (system == null || !_validator.IsValidEntity(system, ProcessResult))
            {
                return null;
            }

            var updatedSys = await _dataSource.UpdateAsync(system);
            if (updatedSys == null)
            {
                ProcessResult.PropagandaResult(_dataSource.ProcessMessage);
            }

            return updatedSys;
        }

        public PageList<Subsystem> GetEntities(Expression<Func<Subsystem, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var sys = _dataSource.GetEntities(query, resourceCollectionParameters);
                return sys;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<PageList<Subsystem>> GetEntitiesAsync(Expression<Func<Subsystem, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var sys = await _dataSource.GetEntitiesAsync(query, resourceCollectionParameters);
                return sys;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public Subsystem GetEntityById(int id)
        {
            try
            {
                var system = _dataSource.GetEntity(id);
                return system;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<Subsystem> GetEntityByIdAsync(int id)
        {
            try
            {
                var system = await _dataSource.GetEntityAsync(id);
                return system;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public bool Register(Subsystem subsystem)
        {
            var newApplication = Create(subsystem);
            return newApplication != null;
        }

        public bool HasApplicationRegistered(Subsystem subsystem)
        {
            if (subsystem == null)
            {
                return false;
            }
            return GetEntities(s => s.Name == subsystem.Name).FirstOrDefault() != null;
        }

        public ProcessingResult ProcessResult { get; } = new();
    }
}