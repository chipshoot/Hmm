using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using Hmm.Utility.Validation;

namespace Hmm.ServiceApi.Models
{
    public class ApiBadRequestResponse : ApiResponse
    {
        public ApiBadRequestResponse(ModelStateDictionary modelState) : base(400)
        {
            Guard.Against<ArgumentException>(modelState.IsValid, $"ModelState must be invalid : {nameof(modelState)}");

            Errors = modelState.SelectMany(x => x.Value.Errors)
                .Select(x => x.ErrorMessage).ToArray();
        }

        public ApiBadRequestResponse(string errorMessage) : base(400)
        {
            Errors = new List<string> { errorMessage };
        }

        public IEnumerable<string> Errors { get; }
    }
}