﻿using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters
{
    public class AutomobileResultFilterAttribute : ResultFilterAttribute
    {
        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var resultFromAction = context.Result as ObjectResult;
            if (resultFromAction?.Value == null ||
                resultFromAction.StatusCode is < 200 or >= 300)
            {
                await next();
                return;
            }

            var mapper = context.HttpContext.RequestServices.GetRequiredService<IMapper>();
            var linkGen = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();
            if (mapper != null)
            {
                var newApiAuto = mapper.Map<AutomobileInfo, ApiAutomobile>(resultFromAction.Value as AutomobileInfo);
                newApiAuto.CreateLinks(context, linkGen);
                resultFromAction.Value = newApiAuto;
            }

            await next();
        }
    }
}