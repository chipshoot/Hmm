using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.Admin.ApiScopes;

[Authorize(Roles = "Administrator")]
public class CreateModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public CreateModel(IConfigurationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ViewModel Scope { get; set; } = new();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (_context.ApiScopes.Any(s => s.Name == Scope.Name))
        {
            ModelState.AddModelError("Scope.Name", "A scope with this name already exists.");
            return Page();
        }

        var entity = new ApiScope
        {
            Name = Scope.Name,
            DisplayName = Scope.DisplayName,
            Description = Scope.Description,
            Enabled = Scope.Enabled,
            ShowInDiscoveryDocument = Scope.ShowInDiscoveryDocument,
            Required = Scope.Required,
            Emphasize = Scope.Emphasize,
            UserClaims = new List<ApiScopeClaim>()
        };

        foreach (var claim in ParseCsv(Scope.UserClaims))
        {
            entity.UserClaims.Add(new ApiScopeClaim { Type = claim, Scope = entity });
        }

        _context.ApiScopes.Add(entity);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private static IEnumerable<string> ParseCsv(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) yield break;
        foreach (var part in input.Split(new[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0) yield return trimmed;
        }
    }
}
