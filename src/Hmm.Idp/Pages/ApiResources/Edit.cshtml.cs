// Pages/ApiResources/Edit.cshtml.cs
using Hmm.Idp.Models;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.ApiResources
{
    public class EditModel : PageModel
    {
        private readonly ApiResourceService _apiResourceService;

        public EditModel(ApiResourceService apiResourceService)
        {
            _apiResourceService = apiResourceService;
        }

        [BindProperty]
        public ApiResourceViewModel ApiResource { get; set; }

        [BindProperty]
        public string ClaimsInput { get; set; }

        [BindProperty]
        public string ScopesInput { get; set; }

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

            ClaimsInput = string.Join(", ", ApiResource.UserClaims ?? new List<string>());
            ScopesInput = string.Join(", ", ApiResource.Scopes ?? new List<string>());

            return Page();
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
            else
            {
                ApiResource.UserClaims = new List<string>();
            }

            if (!string.IsNullOrWhiteSpace(ScopesInput))
            {
                ApiResource.Scopes = ScopesInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
            }
            else
            {
                ApiResource.Scopes = new List<string>();
            }

            try
            {
                await _apiResourceService.UpdateApiResourceAsync(ApiResource);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating API Resource: {ex.Message}");
                return Page();
            }

            return RedirectToPage("./Index");
        }
    }
}
