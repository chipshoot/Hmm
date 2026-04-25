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

        var result = await _signInManager.CheckPasswordSignInAsync(user, context.Password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Password matched, but the account hasn't been verified yet — surface
            // a distinct error code so the client app can prompt the user to check
            // their inbox / hit /Account/ResendConfirmation, instead of just
            // showing "wrong password".
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
        else if (result.IsLockedOut)
        {
            context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidGrant,
                "account_locked");
        }
        else
        {
            context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidGrant,
                "invalid_username_or_password");
        }
    }
}
