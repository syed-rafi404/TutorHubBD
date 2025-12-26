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
                    IsVerified = false
                };
                _context.Tutors.Add(tutor);
                await _context.SaveChangesAsync();
            }

            var viewModel = new TutorProfileViewModel
            {
                TutorID = tutor.TutorID,
                Education = tutor.Education,
                Subjects = tutor.Subjects,
                Rating = tutor.Rating,
                IsVerified = tutor.IsVerified,
                VerificationDocumentPath = tutor.VerificationDocumentPath,
                VerificationRequestDate = tutor.VerificationRequestDate
            };

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

            tutor.Education = model.Education;
            tutor.Subjects = model.Subjects;

            _context.Update(tutor);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Your tutor profile has been updated.";
            return RedirectToAction(nameof(EditProfile));
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

            _context.Update(tutor);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Verification document uploaded successfully. Please wait for admin approval.";
            return RedirectToAction(nameof(EditProfile));
        }
    }
}
