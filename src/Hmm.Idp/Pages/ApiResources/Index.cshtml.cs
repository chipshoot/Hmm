// Pages/ApiResources/Index.cshtml.cs

using Hmm.Idp.Models;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.ApiResources
{
    public class IndexModel(ApiResourceService apiResourceService) : PageModel
    {
        public List<ApiResourceViewModel> ApiResources { get; private set; }

        public async Task OnGetAsync()
        {
            ApiResources = await apiResourceService.GetAllApiResourcesAsync();
        }
    }
}