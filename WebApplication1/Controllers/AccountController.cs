using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Models.ViewModels;
using TutorHubBD.Web.Services;
using System.IO;
using System.Text.Json;

namespace TutorHubBD.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOtpService _otpService;

        // Allowed roles for registration (Admin excluded)
        private static readonly string[] AllowedRoles = { "Guardian", "Teacher" };

        // TempData keys for registration flow
        private const string RegistrationDataKey = "RegistrationData";

        public AccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment,
            IOtpService otpService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
            _otpService = otpService;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register - Step 1: Collect data and send OTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Validate that the role is allowed (only Guardian or Teacher)
                if (!AllowedRoles.Contains(model.Role))
                {
                    ModelState.AddModelError("Role", "Invalid role selected. Please choose Guardian or Teacher.");
                    return View(model);
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    return View(model);
                }

                // Store registration data in TempData for the verification step
                var registrationData = new
                {
                    model.FullName,
                    model.Email,
                    model.Password,
                    model.Role
                };
                TempData[RegistrationDataKey] = JsonSerializer.Serialize(registrationData);

                // Generate and send OTP
                var otpSent = await _otpService.GenerateAndSendOtpAsync(model.Email, OtpPurpose.Registration);

                if (otpSent)
                {
                    TempData["SuccessMessage"] = "A verification code has been sent to your email.";
                    return RedirectToAction(nameof(VerifyEmail), new { email = model.Email });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to send verification email. Please try again.");
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: /Account/VerifyEmail
        [HttpGet]
        public IActionResult VerifyEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(Register));
            }

            var model = new VerifyEmailViewModel { Email = email };
            return View(model);
        }

        // POST: /Account/VerifyEmail - Step 2: Verify OTP and create account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Retrieve registration data from TempData
            var registrationJson = TempData[RegistrationDataKey]?.ToString();
            if (string.IsNullOrEmpty(registrationJson))
            {
                TempData["ErrorMessage"] = "Registration session expired. Please register again.";
                return RedirectToAction(nameof(Register));
            }

            // Keep the data in TempData in case verification fails
            TempData.Keep(RegistrationDataKey);

            // Verify OTP
            var isValid = await _otpService.VerifyOtpAsync(model.Email, model.OtpCode, OtpPurpose.Registration);

            if (!isValid)
            {
                ModelState.AddModelError("OtpCode", "Invalid or expired verification code. Please try again.");
                return View(model);
            }

            // Parse registration data
            var registrationData = JsonSerializer.Deserialize<JsonElement>(registrationJson);
            var fullName = registrationData.GetProperty("FullName").GetString();
            var email = registrationData.GetProperty("Email").GetString();
            var password = registrationData.GetProperty("Password").GetString();
            var role = registrationData.GetProperty("Role").GetString();

            // Create the user account
            var user = new ApplicationUser 
            { 
                UserName = email, 
                Email = email,
                FullName = fullName ?? "",
                EmailConfirmed = true, // Email is verified via OTP
                Address = "",
                Bio = ""
            };

            var result = await _userManager.CreateAsync(user, password!);

            if (result.Succeeded)
            {
                // Assign the selected role to the user
                await _userManager.AddToRoleAsync(user, role!);
                
                // Clear registration data from TempData
                TempData.Remove(RegistrationDataKey);

                // Sign in the user
                await _signInManager.SignInAsync(user, isPersistent: false);
                
                TempData["SuccessMessage"] = "Your account has been created successfully!";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // POST: /Account/ResendOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string email, string purpose)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email is required.";
                return RedirectToAction(nameof(Register));
            }

            var otpSent = await _otpService.GenerateAndSendOtpAsync(email, purpose);

            if (otpSent)
            {
                TempData["SuccessMessage"] = "A new verification code has been sent to your email.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to send verification code. Please try again.";
            }

            if (purpose == OtpPurpose.Registration)
            {
                // Keep registration data for retry
                TempData.Keep(RegistrationDataKey);
                return RedirectToAction(nameof(VerifyEmail), new { email });
            }
            else if (purpose == OtpPurpose.PasswordReset)
            {
                return RedirectToAction(nameof(ResetPassword), new { email });
            }

            return RedirectToAction(nameof(Register));
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword - Send OTP for password reset
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if user exists
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                TempData["SuccessMessage"] = "If an account with this email exists, a verification code has been sent.";
                return RedirectToAction(nameof(ResetPassword), new { email = model.Email });
            }

            // Generate and send OTP
            var otpSent = await _otpService.GenerateAndSendOtpAsync(model.Email, OtpPurpose.PasswordReset);

            TempData["SuccessMessage"] = "If an account with this email exists, a verification code has been sent.";
            return RedirectToAction(nameof(ResetPassword), new { email = model.Email });
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }

        // POST: /Account/ResetPassword - Verify OTP and reset password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify OTP
            var isValid = await _otpService.VerifyOtpAsync(model.Email, model.OtpCode, OtpPurpose.PasswordReset);

            if (!isValid)
            {
                ModelState.AddModelError("OtpCode", "Invalid or expired verification code. Please try again.");
                return View(model);
            }

            // Find the user
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                TempData["SuccessMessage"] = "Your password has been reset successfully.";
                return RedirectToAction(nameof(Login));
            }

            // Reset the password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in with your new password.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);
            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new UserProfileViewModel
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            return View(model);
        }

        // POST: /Account/Profile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Handle profile picture upload
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ProfilePicture", "Invalid file type. Allowed types: JPG, JPEG, PNG, GIF.");
                    model.ProfilePictureUrl = user.ProfilePictureUrl;
                    return View(model);
                }

                // Validate file size (max 2MB)
                if (model.ProfilePicture.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("ProfilePicture", "File size must be less than 2MB.");
                    model.ProfilePictureUrl = user.ProfilePictureUrl;
                    return View(model);
                }

                // Create the uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Generate unique filename
                var uniqueFileName = $"{user.Id}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(fileStream);
                }

                // Update user's profile picture URL
                user.ProfilePictureUrl = $"/uploads/profiles/{uniqueFileName}";
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address ?? "";
            user.Bio = model.Bio ?? "";

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["StatusMessage"] = "Your profile has been updated";
            return RedirectToAction(nameof(Profile));
        }
    }
}
