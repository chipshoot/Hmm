namespace Hmm.Idp.Pages.Admin.User;

public class UserClaim
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public string Issuer { get; set; }
    public string OriginalIssuer { get; set; }

    public ApplicationUser User { get; set; }
}