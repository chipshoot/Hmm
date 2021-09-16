using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;

namespace Hmm.Core.DefaultManager
{
    public class SubsystemManager : ISubsystemManager
    {
        private readonly IRepository<Subsystem> _dataSource;
        private readonly SubsystemValidator _validator;

        public SubsystemManager(IRepository<Subsystem> dataSource, SubsystemValidator validator)
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
                var addedSystem = _dataSource.Add(system);
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
            if (!_validator.IsValidEntity(system, ProcessResult))
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

        IEnumerable<Subsystem> IEntityManager<Subsystem>.GetEntities()
        {
            try
            {
                var sys = _dataSource.GetEntities();
                return sys;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public ProcessingResult ProcessResult { get; } = new ProcessingResult();
    }
}