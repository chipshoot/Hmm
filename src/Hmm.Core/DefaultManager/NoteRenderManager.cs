using System;
using System.Collections.Generic;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;

namespace Hmm.Core.DefaultManager
{
    public class NoteRenderManager : INoteRenderManager
    {
        private readonly IRepository<NoteRender> _dataSource;
        private readonly IHmmValidator<NoteRender> _validator;

        public NoteRenderManager(IRepository<NoteRender> dataSource, IHmmValidator<NoteRender> validator)
        {
            Guard.Against<ArgumentNullException>(dataSource == null, nameof(dataSource));
            Guard.Against<ArgumentNullException>(validator == null, nameof(validator));

            _dataSource = dataSource;
            _validator = validator;
        }

        public NoteRender Create(NoteRender render)
        {
            if (!_validator.IsValidEntity(render, ProcessResult))
            {
                return null;
            }

            try
            {
                var addedRender = _dataSource.Add(render);
                return addedRender;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public NoteRender Update(NoteRender render)
        {
            if (!_validator.IsValidEntity(render, ProcessResult))
            {
                return null;
            }

            var updatedRender = _dataSource.Update(render);
            if (updatedRender == null)
            {
                ProcessResult.PropagandaResult(_dataSource.ProcessMessage);
            }

            return updatedRender;
        }

        public IEnumerable<NoteRender> GetEntities()
        {
            try
            {
                var renders = _dataSource.GetEntities();
                return renders;
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