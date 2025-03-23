// Pages/ApiResources/Details.cshtml.cs
using Hmm.Idp.Models;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.ApiResources
{
    public class DetailsModel : PageModel
    {
        private readonly ApiResourceService _apiResourceService;

        public DetailsModel(ApiResourceService apiResourceService)
        {
            _apiResourceService = apiResourceService;
        }

        public ApiResourceViewModel ApiResource { get; set; }

        public async Task<IActionResult> OnGetAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return NotFound();
            }

            ApiResource = await _apiResourceService.GetApiResourceByNameAsync(name);

            if (ApiResource == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}