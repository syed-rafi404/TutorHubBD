using System.ComponentModel.DataAnnotations;

namespace TutorHubBD.Web.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the email verification step during registration
    /// </summary>
    public class VerifyEmailViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the verification code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "The code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "The code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string OtpCode { get; set; } = string.Empty;

        // Pass-through registration data (stored in TempData/Session)
        public string? FullName { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
    }

    /// <summary>
    /// ViewModel for initiating password reset
    /// </summary>
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for resetting password after OTP verification
    /// </summary>
    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the verification code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "The code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string OtpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for resending OTP
    /// </summary>
    public class ResendOtpViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Purpose { get; set; } = string.Empty;
    }
}
