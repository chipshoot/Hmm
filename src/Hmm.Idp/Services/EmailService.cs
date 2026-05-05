using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Hmm.Idp.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlMessage)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlMessage };

                using var client = new SmtpClient();

                // Map UseSsl + port to MailKit's connection mode:
                //   UseSsl=false  → cleartext (e.g. mailpit on 1025)
                //   UseSsl=true + port 465 → implicit TLS (Resend, Amazon SES, most SMTPS)
                //   UseSsl=true + any other port → STARTTLS upgrade (port 587 etc.)
                // System.Net.Mail.SmtpClient (replaced) only ever did STARTTLS even
                // when EnableSsl=true was set against port 465, which is why the
                // previous code couldn't talk to providers that only expose SMTPS.
                var secureOption = _emailSettings.UseSsl
                    ? (_emailSettings.SmtpPort == 465
                        ? SecureSocketOptions.SslOnConnect
                        : SecureSocketOptions.StartTls)
                    : SecureSocketOptions.None;

                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, secureOption);

                if (!string.IsNullOrWhiteSpace(_emailSettings.Username))
                {
                    await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {EmailAddress}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {EmailAddress}", to);
                return false;
            }
        }

        public Task<bool> SendVerificationEmailAsync(string email, string userId, string verificationToken)
        {
            // Legacy path — builds the URL from EmailSettings.ApplicationUrl. Kept so the
            // existing call sites (and tests) keep compiling, but new code should call the
            // overload that accepts a pre-built callback URL.
            var callbackUrl = $"{_emailSettings.ApplicationUrl}/Account/ConfirmEmail?userId={WebUtility.UrlEncode(userId)}&token={WebUtility.UrlEncode(verificationToken)}";
            return SendVerificationEmailAsync(email, callbackUrl);
        }

        public async Task<bool> SendVerificationEmailAsync(string email, string callbackUrl)
        {
            var subject = "Confirm Your Email";
            var message = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .button {{ display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; }}
                        .footer {{ margin-top: 20px; font-size: 12px; color: #6c757d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Confirm Your Email Address</h2>
                        </div>
                        <div class='content'>
                            <p>Thank you for registering with us. Please confirm your email address by clicking the button below:</p>
                            <p style='text-align: center;'>
                                <a href='{callbackUrl}' class='button'>Confirm Email</a>
                            </p>
                            <p>If you didn't request this confirmation, you can safely ignore this email.</p>
                            <p>If the button doesn't work, copy and paste the following link into your browser:</p>
                            <p>{callbackUrl}</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(email, subject, message);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string userId, string resetToken)
        {
            var callbackUrl = $"{_emailSettings.ApplicationUrl}/Account/ResetPassword?userId={WebUtility.UrlEncode(userId)}&token={WebUtility.UrlEncode(resetToken)}";

            var subject = "Reset Your Password";
            var message = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .button {{ display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; }}
                        .footer {{ margin-top: 20px; font-size: 12px; color: #6c757d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Reset Your Password</h2>
                        </div>
                        <div class='content'>
                            <p>You recently requested to reset your password. Click the button below to continue:</p>
                            <p style='text-align: center;'>
                                <a href='{callbackUrl}' class='button'>Reset Password</a>
                            </p>
                            <p>If you didn't request a password reset, you can safely ignore this email.</p>
                            <p>This password reset link will expire in 24 hours.</p>
                            <p>If the button doesn't work, copy and paste the following link into your browser:</p>
                            <p>{callbackUrl}</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(email, subject, message);
        }

        public async Task<bool> SendAccountLockedEmailAsync(string email, string username)
        {
            var subject = "Account Locked - Multiple Failed Login Attempts";
            var message = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .button {{ display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; }}
                        .footer {{ margin-top: 20px; font-size: 12px; color: #6c757d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Account Security Alert</h2>
                        </div>
                        <div class='content'>
                            <p>Your account with username <strong>{username}</strong> has been temporarily locked due to multiple failed login attempts.</p>
                            <p>If this was you, you can reset your password using the forgot password function:</p>
                            <p style='text-align: center;'>
                                <a href='{_emailSettings.ApplicationUrl}/Account/ForgotPassword' class='button'>Reset Password</a>
                            </p>
                            <p>If you didn't attempt to log in, your account may have been targeted in a brute force attack. Your account has been locked to protect your security.</p>
                            <p>If you need immediate assistance, please contact our support team.</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(email, subject, message);
        }
    }

    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string htmlMessage);

        /// <summary>
        /// Legacy variant that builds the callback URL from <c>EmailSettings.ApplicationUrl</c>.
        /// Prefer the overload that takes a fully-qualified <paramref name="callbackUrl"/>:
        /// the caller has the current <c>HttpContext</c> and can build the right URL via
        /// <c>Url.Page(...)</c>, which works correctly across dev / staging / prod without
        /// having to keep <c>EmailSettings.ApplicationUrl</c> in sync with <c>IssuerUri</c>.
        /// </summary>
        Task<bool> SendVerificationEmailAsync(string email, string userId, string verificationToken);

        /// <summary>
        /// Sends a verification email whose action link points at <paramref name="callbackUrl"/>.
        /// </summary>
        Task<bool> SendVerificationEmailAsync(string email, string callbackUrl);

        Task<bool> SendPasswordResetEmailAsync(string email, string userId, string resetToken);
        Task<bool> SendAccountLockedEmailAsync(string email, string username);
    }

    public class EmailSettings
    {
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; }
        public string ApplicationUrl { get; set; }

        /// <summary>
        /// Where the "Sign in" button on the email-verification success page
        /// points. Production should point at the consumer-facing site
        /// (https://homemademessage.com), not the IDP's own /Account/Login —
        /// the IDP UI is for identity management and shouldn't be the
        /// landing page for end users. Dev can override to localhost.
        /// </summary>
        public string PostVerificationUrl { get; set; } = "https://homemademessage.com";
    }
}