using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;

namespace TutorHubBD.Web.Services
{
    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OtpService> _logger;

        private const int OtpLength = 6;
        private const int OtpExpiryMinutes = 10;

        public OtpService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IConfiguration configuration,
            ILogger<OtpService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> GenerateAndSendOtpAsync(string email, string purpose)
        {
            try
            {
                await InvalidateExistingOtpsAsync(email, purpose);

                var otpCode = GenerateSecureOtp();

                var userOtp = new UserOtp
                {
                    Email = email.ToLowerInvariant(),
                    OtpCode = otpCode,
                    ExpiryTime = DateTime.Now.AddMinutes(OtpExpiryMinutes),
                    IsUsed = false,
                    Purpose = purpose,
                    CreatedAt = DateTime.Now
                };

                _context.UserOtps.Add(userOtp);
                await _context.SaveChangesAsync();

                var emailSent = await SendOtpEmailAsync(email, otpCode, purpose);

                if (emailSent)
                {
                    _logger.LogInformation("OTP sent successfully to {Email} for {Purpose}", email, purpose);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to send OTP email to {Email}", email);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for {Email}", email);
                return false;
            }
        }

        public async Task<bool> VerifyOtpAsync(string email, string code, string purpose)
        {
            try
            {
                var normalizedEmail = email.ToLowerInvariant();

                var otpRecord = await _context.UserOtps
                    .FirstOrDefaultAsync(o => 
                        o.Email == normalizedEmail && 
                        o.OtpCode == code && 
                        o.Purpose == purpose &&
                        !o.IsUsed);

                if (otpRecord == null)
                {
                    _logger.LogWarning("OTP not found or already used for {Email}", email);
                    return false;
                }

                if (DateTime.Now > otpRecord.ExpiryTime)
                {
                    _logger.LogWarning("OTP expired for {Email}", email);
                    return false;
                }

                otpRecord.IsUsed = true;
                _context.UserOtps.Update(otpRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation("OTP verified successfully for {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for {Email}", email);
                return false;
            }
        }

        public async Task InvalidateExistingOtpsAsync(string email, string purpose)
        {
            try
            {
                var normalizedEmail = email.ToLowerInvariant();

                var existingOtps = await _context.UserOtps
                    .Where(o => o.Email == normalizedEmail && 
                               o.Purpose == purpose && 
                               !o.IsUsed)
                    .ToListAsync();

                foreach (var otp in existingOtps)
                {
                    otp.IsUsed = true;
                }

                if (existingOtps.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Invalidated {Count} existing OTPs for {Email}", existingOtps.Count, email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating OTPs for {Email}", email);
            }
        }

        private string GenerateSecureOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return number.ToString("D6");
        }

        private async Task<bool> SendOtpEmailAsync(string email, string otpCode, string purpose)
        {
            var subject = purpose switch
            {
                OtpPurpose.Registration => "Verify Your Email - TutorHubBD",
                OtpPurpose.PasswordReset => "Reset Your Password - TutorHubBD",
                OtpPurpose.EmailChange => "Confirm Email Change - TutorHubBD",
                _ => "Your Verification Code - TutorHubBD"
            };

            var purposeText = purpose switch
            {
                OtpPurpose.Registration => "complete your registration",
                OtpPurpose.PasswordReset => "reset your password",
                OtpPurpose.EmailChange => "confirm your email change",
                _ => "verify your identity"
            };

            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='text-align: center; padding: 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 10px 10px 0 0;'>
                        <h1 style='color: white; margin: 0;'>TutorHubBD</h1>
                    </div>
                    
                    <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
                        <h2 style='color: #333; margin-top: 0;'>Email Verification</h2>
                        
                        <p style='color: #666; font-size: 16px;'>
                            You requested to {purposeText}. Please use the following verification code:
                        </p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <div style='display: inline-block; background: #667eea; color: white; font-size: 32px; font-weight: bold; letter-spacing: 8px; padding: 15px 30px; border-radius: 10px;'>
                                {otpCode}
                            </div>
                        </div>
                        
                        <p style='color: #666; font-size: 14px;'>
                            <strong>Important:</strong>
                            <ul>
                                <li>This code will expire in <strong>{OtpExpiryMinutes} minutes</strong>.</li>
                                <li>Do not share this code with anyone.</li>
                                <li>If you didn't request this, please ignore this email.</li>
                            </ul>
                        </p>
                        
                        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
                        
                        <p style='color: #999; font-size: 12px; text-align: center;'>
                            This is an automated message from TutorHubBD. Please do not reply to this email.
                        </p>
                    </div>
                </div>
            ";

            try
            {
                await _notificationService.SendEmailAsync(email, subject, emailBody);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", email);
                return false;
            }
        }
    }
}
