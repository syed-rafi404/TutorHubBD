using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using System.Threading.Tasks;

namespace TutorHubBD.Web.Controllers
{
    public class TuitionRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TuitionRequestController(ApplicationDbContext context)
        {
            _context = context;
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
    }
}
