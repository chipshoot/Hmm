// Pages/ApiResources/Delete.cshtml.cs
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.ApiResources
{
    public class DeleteModel : PageModel
    {
        private readonly ApiResourceService _apiResourceService;

        public DeleteModel(ApiResourceService apiResourceService)
        {
            _apiResourceService = apiResourceService;
        }

        [BindProperty]
        public ViewModel ApiResource { get; set; }

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

        public async Task<IActionResult> OnPostAsync()
        {
            if (ApiResource == null || string.IsNullOrEmpty(ApiResource.Name))
            {
                return NotFound();
            }

            await _apiResourceService.DeleteApiResourceAsync(ApiResource.Name);

            return RedirectToPage("./Index");
        }
    }
}