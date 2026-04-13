using System.Text.Json;
using System.Text.Json.Serialization;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

            await _emailService.SendVerificationEmailAsync(
                request.Email,
                user.Id,
                "VerificationToken");

            _logger.LogInformation("User {Username} registered via API", request.Username);

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
