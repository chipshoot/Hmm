using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
                if (addedRender == null)
                {
                    ProcessResult.PropagandaResult(_dataSource.ProcessMessage);
                }
                return addedRender;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<NoteRender> CreateAsync(NoteRender render)
        {
            if (!_validator.IsValidEntity(render, ProcessResult))
            {
                return null;
            }

            try
            {
                var addedRender = await _dataSource.AddAsync(render);
                if (addedRender == null)
                {
                    ProcessResult.PropagandaResult(_dataSource.ProcessMessage);
                }
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

            // make sure the render exists in system
            if (GetEntities().All(r => r.Id != render.Id))
            {
                ProcessResult.AddErrorMessage($"Cannot update render: {render.Name}, because system cannot find it in data source");
                return null;
            }
            var updatedRender = _dataSource.Update(render);
            if (updatedRender == null)
            {
                ProcessResult.PropagandaResult(_dataSource.ProcessMessage);
            }

            return updatedRender;
        }

        public async Task<NoteRender> UpdateAsync(NoteRender render)
        {
            if (!_validator.IsValidEntity(render, ProcessResult))
            {
                return null;
            }

            // make sure the render exists in system
            var renders = await GetEntitiesAsync();
            if (renders.All(r => r.Id != render.Id))
            {
                ProcessResult.AddErrorMessage($"Cannot update render: {render.Name}, because system cannot find it in data source");
                return null;
            }
            var updatedRender = await _dataSource.UpdateAsync(render);
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

        public async Task<IEnumerable<NoteRender>> GetEntitiesAsync(Expression<Func<NoteRender, bool>> query = null)
        {
            try
            {
                var renders = await _dataSource.GetEntitiesAsync(query);
                return renders;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public NoteRender GetEntityById(int id)
        {
            try
            {
                var catalog = _dataSource.GetEntity(id);
                return catalog;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<NoteRender> GetEntityByIdAsync(int id)
        {
            try
            {
                var render = await _dataSource.GetEntityAsync(id);
                return render;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public ProcessingResult ProcessResult { get; } = new();
    }
}