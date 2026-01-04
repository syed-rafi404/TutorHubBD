using System;
using System.ComponentModel.DataAnnotations;

namespace TutorHubBD.Web.Models
{
    public class UserOtp
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(6)]
        public string OtpCode { get; set; } = string.Empty;

        public DateTime ExpiryTime { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// The purpose of the OTP: Registration, PasswordReset, or EmailChange
        /// </summary>
        [MaxLength(50)]
        public string Purpose { get; set; } = "Registration";
    }

    public static class OtpPurpose
    {
        public const string Registration = "Registration";
        public const string PasswordReset = "PasswordReset";
        public const string EmailChange = "EmailChange";
    }
}
