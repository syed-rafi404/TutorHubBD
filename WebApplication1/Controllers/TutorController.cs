using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Models.ViewModels;

namespace TutorHubBD.Web.Controllers
{
    [Authorize]
    public class TutorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TutorController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Tutor/Browse - Browse all tutors (Guardian and Admin only)
        [HttpGet]
        [Authorize(Roles = "Guardian, Admin")]
        public async Task<IActionResult> Browse(string? searchSubject, string? searchLocation, string? searchClass)
        {
            var query = _context.Tutors
                .Include(t => t.User)
                .Where(t => t.IsVerified && t.IsProfileComplete)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchSubject))
            {
                query = query.Where(t => t.Subjects != null && 
                    t.Subjects.ToLower().Contains(searchSubject.ToLower()));
            }

            if (!string.IsNullOrEmpty(searchLocation))
            {
                query = query.Where(t => t.PreferredLocations != null && 
                    t.PreferredLocations.ToLower().Contains(searchLocation.ToLower()));
            }

            if (!string.IsNullOrEmpty(searchClass))
            {
                query = query.Where(t => t.PreferredClasses != null && 
                    t.PreferredClasses.ToLower().Contains(searchClass.ToLower()));
            }

            var tutors = await query
                .OrderByDescending(t => t.Rating)
                .ThenByDescending(t => t.Experience ?? 0)
                .Select(t => new BrowseTutorViewModel
                {
                    TutorId = t.TutorID,
                    FullName = t.User != null ? t.User.FullName ?? t.User.UserName ?? "Unknown" : "Unknown",
                    ProfilePictureUrl = t.ProfilePictureUrl ?? t.User!.ProfilePictureUrl,
                    Education = t.Education,
                    Subjects = t.Subjects,
                    PreferredClasses = t.PreferredClasses,
                    PreferredLocations = t.PreferredLocations,
                    Bio = t.Bio,
                    Experience = t.Experience,
                    Rating = t.Rating,
                    IsVerified = t.IsVerified,
                    Email = t.User != null ? t.User.Email : null,
                    PhoneNumber = t.User != null ? t.User.PhoneNumber : null
                })
                .ToListAsync();

            ViewData["CurrentSubject"] = searchSubject;
            ViewData["CurrentLocation"] = searchLocation;
            ViewData["CurrentClass"] = searchClass;

            return View(tutors);
        }

        // GET: /Tutor/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var tutor = await _context.Tutors
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tutor == null)
            {
                // Create a new tutor profile if one doesn't exist
                tutor = new Tutor
                {
                    UserId = user.Id,
                    Education = "",
                    Subjects = "",
                    Rating = 0,
                    IsVerified = false,
                    Experience = null,
                    PreferredLocations = "",
                    Bio = "",
                    PreferredClasses = "",
                    IsProfileComplete = false
                };
                _context.Tutors.Add(tutor);
                await _context.SaveChangesAsync();
            }

            var viewModel = new TutorProfileViewModel
            {
                TutorID = tutor.TutorID,
                Education = tutor.Education,
                Subjects = tutor.Subjects,
                Experience = tutor.Experience,
                PreferredLocations = tutor.PreferredLocations,
                Bio = tutor.Bio,
                ProfilePictureUrl = tutor.ProfilePictureUrl,
                Rating = tutor.Rating,
                IsVerified = tutor.IsVerified,
                IsProfileComplete = tutor.IsProfileComplete,
                VerificationDocumentPath = tutor.VerificationDocumentPath,
                VerificationRequestDate = tutor.VerificationRequestDate
            };
            
            // Set selected classes from comma-separated string
            viewModel.SetSelectedClassesFromString(tutor.PreferredClasses);

            return View(viewModel);
        }

        // POST: /Tutor/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(TutorProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var tutor = await _context.Tutors
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tutor == null)
            {
                return NotFound("Tutor profile not found.");
            }

            // Handle profile picture upload
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var uploadResult = await UploadProfilePicture(model.ProfilePicture, tutor);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError("ProfilePicture", uploadResult.ErrorMessage!);
                    model.ProfilePictureUrl = tutor.ProfilePictureUrl;
                    return View(model);
                }
                tutor.ProfilePictureUrl = uploadResult.FilePath;
            }

            tutor.Education = model.Education ?? "";
            tutor.Subjects = model.Subjects ?? "";
            tutor.Experience = model.Experience;
            tutor.PreferredLocations = model.PreferredLocations ?? "";
            tutor.Bio = model.Bio ?? "";
            tutor.PreferredClasses = model.GetPreferredClassesString();
            
            // Update profile completeness status
            tutor.IsProfileComplete = tutor.CheckProfileCompleteness();

            _context.Update(tutor);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Your tutor profile has been updated.";
            return RedirectToAction(nameof(EditProfile));
        }

        // Helper method to upload profile picture
        private async Task<(bool Success, string? FilePath, string? ErrorMessage)> UploadProfilePicture(IFormFile file, Tutor tutor)
        {
            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return (false, null, "Invalid file type. Allowed types: JPG, JPEG, PNG, GIF.");
            }

            // Validate file size (max 2MB)
            if (file.Length > 2 * 1024 * 1024)
            {
                return (false, null, "File size must be less than 2MB.");
            }

            // Create the uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tutor-profiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Delete old profile picture if exists
            if (!string.IsNullOrEmpty(tutor.ProfilePictureUrl))
            {
                var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, tutor.ProfilePictureUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // Generate unique filename
            var uniqueFileName = $"tutor_{tutor.TutorID}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return (true, $"/uploads/tutor-profiles/{uniqueFileName}", null);
        }

        // GET: /Tutor/ProfileIncomplete
        [HttpGet]
        public IActionResult ProfileIncomplete()
        {
            return View();
        }

        // POST: /Tutor/UploadVerification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadVerification(IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var tutor = await _context.Tutors
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tutor == null)
            {
                return NotFound("Tutor profile not found.");
            }

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction(nameof(EditProfile));
            }

            // Validate file type (allow common document types)
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Invalid file type. Allowed types: PDF, JPG, PNG, DOC, DOCX.";
                return RedirectToAction(nameof(EditProfile));
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File size must be less than 5MB.";
                return RedirectToAction(nameof(EditProfile));
            }

            // Create the uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "docs");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Delete old file if exists
            if (!string.IsNullOrEmpty(tutor.VerificationDocumentPath))
            {
                var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, tutor.VerificationDocumentPath.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // Generate unique filename
            var uniqueFileName = $"{tutor.TutorID}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Update tutor record
            tutor.VerificationDocumentPath = $"/uploads/docs/{uniqueFileName}";
            tutor.VerificationRequestDate = DateTime.Now;
            
            // Reset verification status when new document is uploaded
            tutor.IsVerified = false;
            tutor.IsProfileComplete = tutor.CheckProfileCompleteness();

            _context.Update(tutor);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Verification document uploaded successfully. Please wait for admin approval.";
            return RedirectToAction(nameof(EditProfile));
        }
    }
}
