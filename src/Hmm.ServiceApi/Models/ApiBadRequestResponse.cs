﻿using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.ServiceApi.Models
{
    public class ApiBadRequestResponse : ApiResponse
    {
        public ApiBadRequestResponse(ModelStateDictionary modelState) : base(400)
        {
            Guard.Against<ArgumentException>(modelState.IsValid, $"ModelState must be invalid : {nameof(modelState)}");

            // ReSharper disable once PossibleNullReferenceException
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