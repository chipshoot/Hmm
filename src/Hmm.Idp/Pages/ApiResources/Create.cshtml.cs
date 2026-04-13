// Pages/ApiResources/Create.cshtml.cs
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.ApiResources
{
    [Authorize(Roles = "Administrator")]
    public class CreateModel : PageModel
    {
        private readonly ApiResourceService _apiResourceService;

        public CreateModel(ApiResourceService apiResourceService)
        {
            _apiResourceService = apiResourceService;
        }

        [BindProperty]
        public ViewModel ApiResource { get; set; } = new();

        [BindProperty]
        public string ClaimsInput { get; set; }

        [BindProperty]
        public string ScopesInput { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Process claims and scopes from comma-separated inputs
            if (!string.IsNullOrWhiteSpace(ClaimsInput))
            {
                ApiResource.UserClaims = ClaimsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(ScopesInput))
            {
                ApiResource.Scopes = ScopesInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
            }

            await _apiResourceService.CreateApiResourceAsync(ApiResource);

            return RedirectToPage("./Index");
        }
    }
}