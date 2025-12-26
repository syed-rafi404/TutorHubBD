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
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Review/Create?jobId=5
        [HttpGet]
        public async Task<IActionResult> Create(int jobId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var job = await _context.TuitionOffers
                .Include(j => j.HiredTutor)
                    .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                return NotFound("Job not found.");
            }

            // Check if current user is the Guardian (owner) of this job
            if (job.GuardianId != user.Id)
            {
                TempData["ErrorMessage"] = "You are not authorized to review this job.";
                return RedirectToAction("MyJobs", "TuitionOffer");
            }

            // Check if job status is Filled
            if (job.Status != JobStatus.Filled)
            {
                TempData["ErrorMessage"] = "You can only review a job that has been filled.";
                return RedirectToAction("MyJobs", "TuitionOffer");
            }

            // Check if a review already exists for this job
            var existingReview = await _context.Reviews
                .AnyAsync(r => r.JobId == jobId);

            if (existingReview)
            {
                TempData["ErrorMessage"] = "You have already reviewed this tutor for this job.";
                return RedirectToAction("MyJobs", "TuitionOffer");
            }

            // Check if there's a hired tutor
            if (job.HiredTutorId == null || job.HiredTutor == null)
            {
                TempData["ErrorMessage"] = "No tutor has been hired for this job yet.";
                return RedirectToAction("MyJobs", "TuitionOffer");
            }

            var viewModel = new ReviewCreateViewModel
            {
                JobId = job.Id,
                JobTitle = job.Title,
                TutorId = job.HiredTutorId.Value,
                TutorName = job.HiredTutor.User?.FullName ?? job.HiredTutor.User?.UserName ?? "Unknown Tutor"
            };

            return View(viewModel);
        }

        // POST: Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReviewCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var job = await _context.TuitionOffers
                .Include(j => j.HiredTutor)
                .FirstOrDefaultAsync(j => j.Id == model.JobId);

            if (job == null)
            {
                return NotFound("Job not found.");
            }

            // Re-validate authorization
            if (job.GuardianId != user.Id)
            {
                TempData["ErrorMessage"] = "You are not authorized to review this job.";
                return RedirectToAction("MyJobs", "TuitionOffer");
            }

            if (job.Status != JobStatus.Filled)
            {
                TempData["ErrorMessage"] = "You can only review a job that has been filled.";
                return RedirectToAction("MyJobs", "TuitionOffer");
            }

            // Check for existing review
            var existingReview = await _context.Reviews
                .AnyAsync(r => r.JobId == model.JobId);

            if (existingReview)
            {
                TempData["ErrorMessage"] = "You have already reviewed this tutor for this job.";
                return RedirectToAction("MyJobs", "TuitionOffer");
            }

            if (!ModelState.IsValid)
            {
                model.JobTitle = job.Title;
                model.TutorName = job.HiredTutor?.User?.FullName ?? "Unknown Tutor";
                return View(model);
            }

            var review = new Review
            {
                JobId = model.JobId,
                Rating = model.Rating,
                Comment = model.Comment,
                ReviewerId = user.Id,
                TutorId = job.HiredTutorId,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Update tutor's average rating
            await UpdateTutorRating(job.HiredTutorId.Value);

            TempData["SuccessMessage"] = "Thank you for your review!";
            return RedirectToAction("MyJobs", "TuitionOffer");
        }

        private async Task UpdateTutorRating(int tutorId)
        {
            var tutor = await _context.Tutors.FindAsync(tutorId);
            if (tutor == null) return;

            var reviews = await _context.Reviews
                .Where(r => r.TutorId == tutorId)
                .ToListAsync();

            if (reviews.Any())
            {
                tutor.Rating = (float)reviews.Average(r => r.Rating);
                _context.Update(tutor);
                await _context.SaveChangesAsync();
            }
        }
    }
}
