using System.Security.Claims;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Identity;

namespace Hmm.Idp.Pages.Admin.User;

public class ApplicationUser : IdentityUser
{
    public string SubjectId { get; set; }

    public string Password { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public bool IsActive { get; set; } = true;

    public string ProviderName { get; set; }

    public string ProviderSubjectId { get; set; }

    /// <summary>
    /// Gets or sets the custom user claims.
    /// </summary>
    public ICollection<UserClaim> UserClaims { get; set; } = new List<UserClaim>();

    // Additional properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? ProfilePictureUrl { get; set; }

    // Helper method to convert UserClaim to System.Security.Claims.Claim
    public IEnumerable<Claim> GetClaims()
    {
        return UserClaims.Select(uc => new Claim(uc.Type, uc.Value, null, uc.Issuer, uc.OriginalIssuer));
    }

    // Helper method to add a System.Security.Claims.Claim to the UserClaims collection
    public void AddClaim(Claim claim)
    {
        UserClaims.Add(new UserClaim
        {
            Type = claim.Type,
            Value = claim.Value,
            Issuer = claim.Issuer,
            OriginalIssuer = claim.OriginalIssuer
        });
    }

    // Helper method to remove a System.Security.Claims.Claim from the UserClaims collection
    public void RemoveClaim(Claim claim)
    {
        var userClaim = UserClaims.FirstOrDefault(uc =>
            uc.Type == claim.Type &&
            uc.Value == claim.Value);

        if (userClaim != null)
        {
            UserClaims.Remove(userClaim);
        }
    }
}