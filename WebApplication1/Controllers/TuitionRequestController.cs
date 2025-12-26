using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace TutorHubBD.Web.Controllers
{
    public class TuitionRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TuitionRequestService _tuitionRequestService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TuitionRequestController(ApplicationDbContext context, TuitionRequestService tuitionRequestService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _tuitionRequestService = tuitionRequestService;
            _userManager = userManager;
        }

        // GET: Show the "Apply Now" form for a specific Job
        [Authorize]
        public async Task<IActionResult> Apply(int jobId)
        {
            var job = await _context.TuitionOffers.FindAsync(jobId);
            if (job == null) return NotFound();

            // Check if job is still open
            if (job.Status != JobStatus.Open)
            {
                TempData["ErrorMessage"] = "This job is no longer accepting applications.";
                return RedirectToAction("Index", "TuitionOffer");
            }

            // Get current user and pre-fill their info
            var user = await _userManager.GetUserAsync(User);
            
            var request = new TuitionRequest
            {
                TuitionOfferId = job.Id,
                StudentName = user?.FullName ?? "",
                StudentEmail = user?.Email ?? ""
            };

            ViewData["JobTitle"] = job.Title;

            return View(request);
        }

        // GET: List of all applications (Dashboard)
        public async Task<IActionResult> Index()
        {
            var requests = await _context.TuitionRequests
                                         .AsNoTracking()
                                         .Include(r => r.TuitionOffer)
                                         .Include(r => r.Tutor)
                                         .ToListAsync();
            return View(requests);
        }

        // POST: Save the application to the database
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(TuitionRequest request)
        {
            // Reload job title for the view in case we need to return
            var job = await _context.TuitionOffers.FindAsync(request.TuitionOfferId);
            if (job != null)
            {
                ViewData["JobTitle"] = job.Title;
            }

            if (!string.IsNullOrEmpty(request.StudentName) && !string.IsNullOrEmpty(request.StudentEmail))
            {
                // Step 1: Retrieve the currently logged-in User's ID
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "You must be logged in to apply for a job.";
                    return RedirectToAction("Login", "Account");
                }

                // Step 2: Query the Tutors table to find the record where Tutor.UserId matches the logged-in ID
                var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserId == user.Id);

                // Step 3: If NO Tutor record is found, auto-create one for the user
                if (tutor == null)
                {
                    // Auto-create a basic Tutor profile for this user
                    tutor = new Tutor
                    {
                        UserId = user.Id,
                        Education = "Not specified",
                        Subjects = "Not specified",
                        Rating = 0,
                        IsVerified = false
                    };

                    _context.Tutors.Add(tutor);
                    await _context.SaveChangesAsync();

                    TempData["InfoMessage"] = "A tutor profile has been automatically created for you. You can update it later in your profile settings.";
                }

                // Step 4: Assign TutorId and save the application
                request.TutorId = tutor.TutorID;
                request.Status = "Pending";
                request.RequestDate = DateTime.Now;

                _context.TuitionRequests.Add(request);
                await _context.SaveChangesAsync();

                return RedirectToAction("Success");
            }

            ModelState.AddModelError("", "Please fill in all required fields.");
            return View(request);
        }

        public IActionResult Success()
        {
            return View();
        }

        // POST: Update the status of an application
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var success = await _tuitionRequestService.UpdateStatusAsync(id, status);

            if (success)
            {
                TempData["SuccessMessage"] = $"Application status updated to {status}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Application not found or invalid status.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
