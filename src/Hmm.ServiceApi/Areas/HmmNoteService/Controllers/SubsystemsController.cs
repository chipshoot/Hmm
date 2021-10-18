using AutoMapper;
using Hmm.Core.DomainEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    [Route("api/subsystems")]
    public class SubsystemsController : Controller
    {
        #region private fields

        private readonly IEntityLookup _lookupRepo;
        private readonly IMapper _mapper;

        #endregion private fields

        public SubsystemsController(IMapper mapper, IEntityLookup lookupRepo)
        {
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            Guard.Against<ArgumentNullException>(lookupRepo == null, nameof(lookupRepo));

            _mapper = mapper;
            _lookupRepo = lookupRepo;
        }

        public IActionResult Index()
        {
            var subsystems = _lookupRepo.GetEntities<Subsystem>().ToList();
            var ret = _mapper.Map<List<Subsystem>, List<ApiSubsystem>>(subsystems);
            return Ok(ret);
        }
    }
}