using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Hmm.Idp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IApplicationUserRepository _userRepository;
    private readonly PasswordPolicyService _passwordPolicyService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AccountController(
        IApplicationUserRepository userRepository,
        PasswordPolicyService passwordPolicyService,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _userRepository = userRepository;
        _passwordPolicyService = passwordPolicyService;
        _emailService = emailService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register()
    {
        RegisterRequest? request;
        try
        {
            Request.Body.Position = 0;
            request = await JsonSerializer.DeserializeAsync<RegisterRequest>(
                Request.Body, JsonOptions);
        }
        catch (JsonException)
        {
            return BadRequest(new RegisterErrorResponse
            {
                Errors = new Dictionary<string, string[]>
                {
                    [""] = ["Invalid JSON body."]
                }
            });
        }

        if (request == null)
        {
            return BadRequest(new RegisterErrorResponse
            {
                Errors = new Dictionary<string, string[]>
                {
                    [""] = ["Request body is required."]
                }
            });
        }

        // Manual validation
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Username))
            errors["Username"] = ["Username is required."];
        if (string.IsNullOrWhiteSpace(request.Email))
            errors["Email"] = ["A valid email is required."];
        if (string.IsNullOrWhiteSpace(request.Password))
            errors["Password"] = ["Password is required."];
        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            errors["ConfirmPassword"] = ["Confirm password is required."];
        if (request.Password.Length < 12)
            errors["Password"] = ["Password must be at least 12 characters."];

        if (errors.Count > 0)
            return BadRequest(new RegisterErrorResponse { Errors = errors });

        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new RegisterErrorResponse
            {
                Errors = new Dictionary<string, string[]>
                {
                    ["ConfirmPassword"] = ["The password and confirmation password do not match."]
                }
            });
        }

        var (isValid, policyErrors) = _passwordPolicyService.ValidatePassword(request.Password);
        if (!isValid)
        {
            return BadRequest(new RegisterErrorResponse
            {
                Errors = new Dictionary<string, string[]>
                {
                    ["Password"] = policyErrors.ToArray()
                }
            });
        }

        try
        {
            var user = await _userRepository.CreateUserAsync(
                request.Username,
                request.Password,
                email: request.Email);

            // Generate a real Identity-issued token, base64url-encode it so
            // '+'/'/'/'=' survive the URL round-trip, and build the callback
            // off the current request scheme/host so dev (localhost), staging,
            // and prod (idp.homemademessage.com) all work without keeping
            // EmailSettings.ApplicationUrl in sync with IssuerUri.
            var rawToken = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

            // Hand-build the verification link off the current request scheme +
            // host so dev (localhost), staging, and prod all work without
            // depending on EmailSettings.ApplicationUrl. We don't use
            // Url.Page() here because that requires IUrlHelper plumbing that
            // is awkward to mock in controller unit tests, and the path is
            // fixed by Pages/Account/ConfirmEmail.cshtml's route convention.
            //
            // source=mobile tells the ConfirmEmail page to suppress the
            // "Sign in" CTA — mobile users should switch back to the
            // installed app, not log into a web page.
            var callbackUrl = $"{Request.Scheme}://{Request.Host}/Account/ConfirmEmail" +
                              $"?userId={Uri.EscapeDataString(user.Id)}" +
                              $"&token={Uri.EscapeDataString(encodedToken)}" +
                              "&source=mobile";

            var emailSent = await _emailService.SendVerificationEmailAsync(
                request.Email,
                callbackUrl);

            if (!emailSent)
            {
                _logger.LogWarning(
                    "User {Username} created but verification email failed to send",
                    request.Username);
            }
            else
            {
                _logger.LogInformation("User {Username} registered via API", request.Username);
            }

            return Created(string.Empty, new RegisterResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                Username = user.UserName!
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Username}", request.Username);
            return BadRequest(new RegisterErrorResponse
            {
                Errors = new Dictionary<string, string[]>
                {
                    [""] = [ex.Message]
                }
            });
        }
    }

    /// <summary>
    /// Re-issues an email-verification link for an account that hasn't been
    /// confirmed yet. Used by native clients (Flutter app) to recover from
    /// missed/expired verification mails without bouncing the user out to a
    /// browser. Always returns 200 with the same generic body regardless of
    /// whether the email matches a real unconfirmed account — no
    /// account-existence enumeration.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation()
    {
        ResendConfirmationRequest? request;
        try
        {
            Request.Body.Position = 0;
            request = await JsonSerializer.DeserializeAsync<ResendConfirmationRequest>(
                Request.Body, JsonOptions);
        }
        catch (JsonException)
        {
            // Malformed body — fail loudly. The user-facing API client should
            // never send bad JSON; this is a programmer error.
            return BadRequest(new { error = "Invalid JSON body." });
        }

        // Even with empty/missing email we keep the response generic so an
        // attacker can't probe the endpoint for valid-account hints.
        if (request != null && !string.IsNullOrWhiteSpace(request.Email))
        {
            try
            {
                var user = await _userRepository.FindByEmailAsync(request.Email);
                if (user is { EmailConfirmed: false })
                {
                    var rawToken = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
                    // This endpoint is hit by the Flutter app's "Resend email"
                    // SnackBarAction — preserve source=mobile so the
                    // ConfirmEmail page hides the web Sign In CTA.
                    var callbackUrl = $"{Request.Scheme}://{Request.Host}/Account/ConfirmEmail" +
                                      $"?userId={Uri.EscapeDataString(user.Id)}" +
                                      $"&token={Uri.EscapeDataString(encodedToken)}" +
                                      "&source=mobile";

                    var sent = await _emailService.SendVerificationEmailAsync(request.Email, callbackUrl);
                    if (!sent)
                    {
                        _logger.LogWarning("Resend confirmation: SMTP failed for {Email}", request.Email);
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "Resend confirmation requested for {Email} — no unconfirmed account on file",
                        request.Email);
                }
            }
            catch (Exception ex)
            {
                // Log but don't surface — generic response is the contract.
                _logger.LogError(ex, "Resend confirmation failed for {Email}", request.Email);
            }
        }

        return Ok(new
        {
            message = "If an account with that email exists and is not yet verified, a new verification link has been sent.",
        });
    }
}

public class ResendConfirmationRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("confirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class RegisterResponse
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}

public class RegisterErrorResponse
{
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]> Errors { get; set; } = new();
}
