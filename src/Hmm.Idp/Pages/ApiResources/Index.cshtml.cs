// Pages/ApiResources/Index.cshtml.cs

using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.ApiResources
{
    [Authorize(Roles = "Administrator")]
    public class IndexModel(ApiResourceService apiResourceService) : PageModel
    {
        public List<ViewModel> ApiResources { get; private set; }

        public async Task OnGetAsync()
        {
            ApiResources = await apiResourceService.GetAllApiResourcesAsync();
        }
    }
}