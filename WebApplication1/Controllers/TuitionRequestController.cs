using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Services;
using System.Threading.Tasks;

namespace TutorHubBD.Web.Controllers
{
    public class TuitionRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TuitionRequestService _tuitionRequestService;

        public TuitionRequestController(ApplicationDbContext context, TuitionRequestService tuitionRequestService)
        {
            _context = context;
            _tuitionRequestService = tuitionRequestService;
        }

        // GET: Show the "Apply Now" form for a specific Job
        public async Task<IActionResult> Apply(int jobId)
        {
            var job = await _context.TuitionOffers.FindAsync(jobId);
            if (job == null) return NotFound();

            // Pre-fill the form with the Job ID so we know which job they are applying for
            var request = new TuitionRequest
            {
                TuitionOfferId = job.Id
            };

            // Pass the Job Title to the view for display
            ViewData["JobTitle"] = job.Title;

            return View(request);
        }
        // GET: List of all applications (Dashboard)
        public async Task<IActionResult> Index()
        {
            var requests = await _context.TuitionRequests
                                         .Include(r => r.TuitionOffer) // Load the Job details too
                                         .ToListAsync();
            return View(requests);
        }

        // POST: Save the application to the database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(TuitionRequest request)
        {
            if (!string.IsNullOrEmpty(request.StudentName) && !string.IsNullOrEmpty(request.StudentEmail))
            {
                _context.TuitionRequests.Add(request);
                await _context.SaveChangesAsync();

                // Redirect to a "Success" page or back to Job List
                return RedirectToAction("Success");
            }
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
            // Security check: In a real app, we would check if the current user owns the job post.
            // For now, we assume the user is authorized as per the prompt's simplified context,
            // but we should at least ensure the request exists.
            
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
