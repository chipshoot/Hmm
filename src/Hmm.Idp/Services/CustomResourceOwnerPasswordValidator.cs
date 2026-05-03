using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Hmm.Idp.Pages.Admin.User;
using Microsoft.AspNetCore.Identity;

namespace Hmm.Idp.Services;

public class CustomResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public CustomResourceOwnerPasswordValidator(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        // Try username first, then fall back to email
        var user = await _userManager.FindByNameAsync(context.UserName)
                   ?? await _userManager.FindByEmailAsync(context.UserName);

        if (user == null || !user.IsActive)
        {
            context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidGrant,
                "invalid_username_or_password");
            return;
        }

        // Bail early if the account is already locked out — short-circuits both
        // the password check and the email-confirmation check.
        if (await _userManager.IsLockedOutAsync(user))
        {
            context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidGrant,
                "account_locked");
            return;
        }

        // Verify the password directly through UserManager. We avoid
        // SignInManager.CheckPasswordSignInAsync here because that method
        // *also* enforces SignInOptions like RequireConfirmedEmail — which
        // would make it impossible to distinguish "wrong password" from
        // "right password but unconfirmed". For our two-tier UX (Resend
        // Email vs. retry password), we need that distinction.
        var passwordOk = await _userManager.CheckPasswordAsync(user, context.Password);

        if (!passwordOk)
        {
            // Track the failed attempt in Identity's lockout counter so the
            // user still gets locked out after enough wrong guesses, even
            // though we bypassed CheckPasswordSignInAsync's bookkeeping.
            await _userManager.AccessFailedAsync(user);

            // If that flip put them over the edge, report account_locked.
            if (await _userManager.IsLockedOutAsync(user))
            {
                context.Result = new GrantValidationResult(
                    TokenRequestErrors.InvalidGrant,
                    "account_locked");
                return;
            }

            context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidGrant,
                "invalid_username_or_password");
            return;
        }

        // Password is correct — reset failed-attempt counter so prior wrong
        // guesses don't accumulate forever.
        await _userManager.ResetAccessFailedCountAsync(user);

        // Now the email-confirmation gate. Distinct error so the client can
        // surface a Resend Email prompt instead of "wrong password".
        if (!user.EmailConfirmed)
        {
            context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidGrant,
                "email_not_confirmed");
            return;
        }

        context.Result = new GrantValidationResult(
            user.Id,
            "password");
    }
}
