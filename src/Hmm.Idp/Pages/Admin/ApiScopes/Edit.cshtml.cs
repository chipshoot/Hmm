using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Idp.Pages.Admin.ApiScopes;

[Authorize(Roles = "Administrator")]
public class EditModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public EditModel(IConfigurationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ViewModel Scope { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var entity = await _context.ApiScopes
            .Include(s => s.UserClaims)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (entity == null) return NotFound();

        Scope = new ViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            Enabled = entity.Enabled,
            ShowInDiscoveryDocument = entity.ShowInDiscoveryDocument,
            Required = entity.Required,
            Emphasize = entity.Emphasize,
            UserClaims = string.Join(", ", entity.UserClaims?.Select(c => c.Type) ?? Array.Empty<string>())
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var entity = await _context.ApiScopes
            .Include(s => s.UserClaims)
            .FirstOrDefaultAsync(s => s.Id == Scope.Id);
        if (entity == null) return NotFound();

        entity.DisplayName = Scope.DisplayName;
        entity.Description = Scope.Description;
        entity.Enabled = Scope.Enabled;
        entity.ShowInDiscoveryDocument = Scope.ShowInDiscoveryDocument;
        entity.Required = Scope.Required;
        entity.Emphasize = Scope.Emphasize;

        entity.UserClaims?.Clear();
        entity.UserClaims ??= new List<ApiScopeClaim>();
        foreach (var claim in ParseCsv(Scope.UserClaims))
        {
            entity.UserClaims.Add(new ApiScopeClaim { Type = claim, Scope = entity });
        }

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
