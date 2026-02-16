using System.Text;
using System.Text.Json;
using Hmm.Idp.Controllers;
using Hmm.Idp.Pages.Admin.User;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hmm.Idp.Tests;

public class AccountControllerTests
{
    private readonly Mock<IUserManagementService> _mockUserService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<AccountController>> _mockLogger;
    private readonly PasswordPolicyService _passwordPolicyService;

    public AccountControllerTests()
    {
        _mockUserService = new Mock<IUserManagementService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<AccountController>>();
        _passwordPolicyService = new PasswordPolicyService(new PasswordOptions
        {
            RequiredLength = 12,
            RequiredUniqueChars = 6,
            RequireDigit = true,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireNonAlphanumeric = true
        });
    }

    private AccountController CreateController(string? jsonBody = null)
    {
        var controller = new AccountController(
            _mockUserService.Object,
            _passwordPolicyService,
            _mockEmailService.Object,
            _mockLogger.Object);

        var httpContext = new DefaultHttpContext();
        if (jsonBody != null)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
            httpContext.Request.Body = stream;
            httpContext.Request.ContentType = "application/json";
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private static string SerializeRequest(object request)
    {
        return JsonSerializer.Serialize(request);
    }

    private static RegisterRequest CreateValidRequest() => new()
    {
        Username = "testuser",
        Email = "test@example.com",
        Password = "StrongP@ss1234",
        ConfirmPassword = "StrongP@ss1234"
    };

    #region Happy Path

    [Fact]
    public async Task Register_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = CreateValidRequest();
        var controller = CreateController(SerializeRequest(request));

        var createdUser = new ApplicationUser
        {
            Id = "user-123",
            UserName = request.Username,
            Email = request.Email
        };

        _mockUserService
            .Setup(s => s.CreateUser(request.Username, request.Password, null, request.Email))
            .ReturnsAsync(createdUser);

        _mockEmailService
            .Setup(s => s.SendVerificationEmailAsync(request.Email, createdUser.Id, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await controller.Register();

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        var response = Assert.IsType<RegisterResponse>(createdResult.Value);
        Assert.Equal("user-123", response.UserId);
        Assert.Equal(request.Email, response.Email);
        Assert.Equal(request.Username, response.Username);

        _mockUserService.Verify(
            s => s.CreateUser(request.Username, request.Password, null, request.Email),
            Times.Once);
        _mockEmailService.Verify(
            s => s.SendVerificationEmailAsync(request.Email, createdUser.Id, It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region JSON Parsing Errors

    [Fact]
    public async Task Register_WithInvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController("{invalid json}");

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey(""));
        Assert.Contains("Invalid JSON body.", errorResponse.Errors[""]);
    }

    [Fact]
    public async Task Register_WithNullJsonBody_ReturnsBadRequest()
    {
        // Arrange - JSON literal "null" deserializes to null
        var controller = CreateController("null");

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey(""));
        Assert.Contains("Request body is required.", errorResponse.Errors[""]);
    }

    #endregion

    #region Field Validation

    [Fact]
    public async Task Register_WithMissingUsername_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Username = "";
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey("Username"));
        Assert.Contains("Username is required.", errorResponse.Errors["Username"]);
    }

    [Fact]
    public async Task Register_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Email = "";
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey("Email"));
        Assert.Contains("A valid email is required.", errorResponse.Errors["Email"]);
    }

    [Fact]
    public async Task Register_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Password = "";
        request.ConfirmPassword = "";
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey("Password"));
    }

    [Fact]
    public async Task Register_WithMissingConfirmPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        request.ConfirmPassword = "";
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey("ConfirmPassword"));
        Assert.Contains("Confirm password is required.", errorResponse.Errors["ConfirmPassword"]);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Password = "Short1!aB";
        request.ConfirmPassword = "Short1!aB";
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey("Password"));
        Assert.Contains("Password must be at least 12 characters.", errorResponse.Errors["Password"]);
    }

    [Theory]
    [InlineData("", "Username")]
    [InlineData(null, "Username")]
    public async Task Register_WithEmptyOrNullUsername_ReturnsBadRequestWithUsernameError(string? username, string expectedKey)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Username = username ?? "";
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey(expectedKey));
    }

    #endregion

    #region Password Mismatch

    [Fact]
    public async Task Register_WithPasswordMismatch_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        request.ConfirmPassword = "DifferentP@ss1234";
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey("ConfirmPassword"));
        Assert.Contains(
            "The password and confirmation password do not match.",
            errorResponse.Errors["ConfirmPassword"]);
    }

    #endregion

    #region Password Policy Validation

    [Fact]
    public async Task Register_WithPasswordMissingDigit_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Password = "StrongP@ssword";
        request.ConfirmPassword = "StrongP@ssword";
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey("Password"));
        Assert.Contains(
            "Password must contain at least one digit (0-9).",
            errorResponse.Errors["Password"]);
    }

    [Fact]
    public async Task Register_WithPasswordMissingSpecialChar_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Password = "StrongPass1234";
        request.ConfirmPassword = "StrongPass1234";
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey("Password"));
        Assert.Contains(
            "Password must contain at least one special character (e.g., !@#$%^&*).",
            errorResponse.Errors["Password"]);
    }

    [Fact]
    public async Task Register_WithPasswordMissingUppercase_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Password = "strongp@ss1234";
        request.ConfirmPassword = "strongp@ss1234";
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey("Password"));
        Assert.Contains(
            "Password must contain at least one uppercase letter (A-Z).",
            errorResponse.Errors["Password"]);
    }

    [Fact]
    public async Task Register_WithPasswordMissingLowercase_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Password = "STRONGP@SS1234";
        request.ConfirmPassword = "STRONGP@SS1234";
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey("Password"));
        Assert.Contains(
            "Password must contain at least one lowercase letter (a-z).",
            errorResponse.Errors["Password"]);
    }

    #endregion

    #region Service Exception Handling

    [Fact]
    public async Task Register_WhenCreateUserThrows_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidRequest();
        var controller = CreateController(SerializeRequest(request));

        _mockUserService
            .Setup(s => s.CreateUser(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<string>()))
            .ThrowsAsync(new Exception("Username already exists"));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey(""));
        Assert.Contains("Username already exists", errorResponse.Errors[""]);
    }

    [Fact]
    public async Task Register_WhenEmailServiceFails_StillReturnsCreated()
    {
        // Arrange
        var request = CreateValidRequest();
        var controller = CreateController(SerializeRequest(request));

        var createdUser = new ApplicationUser
        {
            Id = "user-456",
            UserName = request.Username,
            Email = request.Email
        };

        _mockUserService
            .Setup(s => s.CreateUser(request.Username, request.Password, null, request.Email))
            .ReturnsAsync(createdUser);

        _mockEmailService
            .Setup(s => s.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await controller.Register();

        // Assert - email failure doesn't prevent registration success
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.IsType<RegisterResponse>(createdResult.Value);
    }

    #endregion

    #region Multiple Validation Errors

    [Fact]
    public async Task Register_WithMultipleMissingFields_ReturnsBadRequestWithAllErrors()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "",
            Email = "",
            Password = "",
            ConfirmPassword = ""
        };
        var controller = CreateController(SerializeRequest(request));

        // Act
        var result = await controller.Register();

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<RegisterErrorResponse>(badRequest.Value);
        Assert.True(errorResponse.Errors.ContainsKey("Username"));
        Assert.True(errorResponse.Errors.ContainsKey("Email"));
        Assert.True(errorResponse.Errors.ContainsKey("Password"));
        Assert.True(errorResponse.Errors.ContainsKey("ConfirmPassword"));
    }

    #endregion
}
