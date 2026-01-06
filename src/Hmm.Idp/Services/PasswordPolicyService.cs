using Microsoft.AspNetCore.Identity;
using System.Text;

namespace Hmm.Idp.Services
{
    public class PasswordPolicyService
    {
        private readonly PasswordOptions _passwordOptions;

        public PasswordPolicyService(PasswordOptions passwordOptions)
        {
            _passwordOptions = passwordOptions;
        }

        public (bool IsValid, List<string> Errors) ValidatePassword(string password)
        {
            var errors = new List<string>();

            // Check password length
            if (string.IsNullOrEmpty(password) || password.Length < _passwordOptions.RequiredLength)
            {
                errors.Add($"Password must be at least {_passwordOptions.RequiredLength} characters long.");
            }

            // Check for unique characters
            if (_passwordOptions.RequiredUniqueChars > 0)
            {
                var uniqueChars = password.Distinct().Count();
                if (uniqueChars < _passwordOptions.RequiredUniqueChars)
                {
                    errors.Add($"Password must contain at least {_passwordOptions.RequiredUniqueChars} unique characters.");
                }
            }

            // Check for digit
            if (_passwordOptions.RequireDigit && !password.Any(c => char.IsDigit(c)))
            {
                errors.Add("Password must contain at least one digit (0-9).");
            }

            // Check for lowercase
            if (_passwordOptions.RequireLowercase && !password.Any(c => char.IsLower(c)))
            {
                errors.Add("Password must contain at least one lowercase letter (a-z).");
            }

            // Check for uppercase
            if (_passwordOptions.RequireUppercase && !password.Any(c => char.IsUpper(c)))
            {
                errors.Add("Password must contain at least one uppercase letter (A-Z).");
            }

            // Check for non-alphanumeric
            if (_passwordOptions.RequireNonAlphanumeric && !password.Any(c => !char.IsLetterOrDigit(c)))
            {
                errors.Add("Password must contain at least one special character (e.g., !@#$%^&*).");
            }

            // Check for common passwords (example implementation)
            if (IsCommonPassword(password))
            {
                errors.Add("Password is too common and easily guessable. Please choose a stronger password.");
            }

            return (errors.Count == 0, errors);
        }

        private bool IsCommonPassword(string password)
        {
            // This is a very basic list - in a real implementation, you would use a more comprehensive list
            // or an API to check against commonly used passwords
            var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "123456", "12345678", "qwerty", "abc123", "password123",
                "admin", "welcome", "admin123", "login", "master", "letmein"
            };

            return commonPasswords.Contains(password);
        }

        public string GenerateSecurePassword()
        {
            var random = new Random();
            var password = new StringBuilder();

            // Add required character types
            if (_passwordOptions.RequireUppercase)
                password.Append((char)random.Next('A', 'Z' + 1));

            if (_passwordOptions.RequireLowercase)
                password.Append((char)random.Next('a', 'z' + 1));

            if (_passwordOptions.RequireDigit)
                password.Append((char)random.Next('0', '9' + 1));

            if (_passwordOptions.RequireNonAlphanumeric)
            {
                var specialChars = "!@#$%^&*()_-+=<>?";
                password.Append(specialChars[random.Next(specialChars.Length)]);
            }

            // Add random characters until we meet the minimum length
            while (password.Length < _passwordOptions.RequiredLength)
            {
                var charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_-+=<>?";
                password.Append(charSet[random.Next(charSet.Length)]);
            }

            // Shuffle the password characters
            var passwordArray = password.ToString().ToCharArray();
            for (var i = passwordArray.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (passwordArray[i], passwordArray[j]) = (passwordArray[j], passwordArray[i]);
            }

            return new string(passwordArray);
        }
    }
}