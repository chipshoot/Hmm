using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.Account
{
    /// <summary>
    /// Landing page after a successful registration POST. We always land here
    /// regardless of whether the verification email actually went out — that
    /// way the response can't be used to enumerate which addresses our SMTP
    /// host accepts.
    /// </summary>
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        [FromQuery]
        public string Email { get; set; }

        public IActionResult OnGet() => Page();
    }
}
