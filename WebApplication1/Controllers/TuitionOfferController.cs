using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Models.ViewModels;
using TutorHubBD.Web.Services;

namespace TutorHubBD.Web.Controllers
{
    [AllowAnonymous]
    public class TuitionOfferController : Controller
    {
        private readonly ITuitionOfferService _service;
        private readonly ApplicationDbContext _context;
        private readonly ICommissionService _commissionService;

        public TuitionOfferController(ITuitionOfferService service, ApplicationDbContext context, ICommissionService commissionService)
        {
            _service = service;
            _context = context;
            _commissionService = commissionService;
        }

        // GET: TuitionOffer/Index
        public async Task<IActionResult> Index(string searchCity, string searchMedium, string searchClass)
        {
            var jobs = await _service.SearchOffersAsync(searchCity, searchMedium, searchClass);

            ViewData["CurrentCity"] = searchCity;
            ViewData["CurrentMedium"] = searchMedium;
            ViewData["CurrentClass"] = searchClass;

            return View(jobs);
        }

        // GET: TuitionOffer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TuitionOffer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TuitionOfferCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var offer = new TuitionOffer
            {
                Title = model.Title,
                Description = model.Description,
                Salary = model.Salary,
                City = model.City,
                Location = model.Location,
                Medium = model.Medium,
                StudentClass = model.StudentClass,
                Subject = "General",
                DaysPerWeek = "Negotiable",
                GenderPreference = "Any"
            };

            await _service.CreateOfferAsync(offer);
            return RedirectToAction(nameof(Index));
        }

        // POST: TuitionOffer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _service.DeleteOfferAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // POST: TuitionOffer/ConfirmHiring
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmHiring(int jobId, int tutorId)
        {
            var job = await _context.TuitionOffers.FindAsync(jobId);
            if (job == null)
            {
                return NotFound();
            }

            // Validate that tutorId is valid
            if (tutorId <= 0)
            {
                TempData["ErrorMessage"] = "Cannot hire: The applicant does not have a valid tutor profile linked to their application.";
                return RedirectToAction("Index", "TuitionRequest");
            }

            // Verify the tutor exists in the database
            var tutor = await _context.Tutors.FindAsync(tutorId);
            if (tutor == null)
            {
                TempData["ErrorMessage"] = "Cannot hire: The specified tutor profile does not exist.";
                return RedirectToAction("Index", "TuitionRequest");
            }

            // In a real scenario, verify that the current user is the owner of the job.
            // For now, we proceed assuming authorization is handled or simplified.

            if (job.Status == JobStatus.Open)
            {
                job.HiredTutorId = tutorId;
                job.Status = JobStatus.Filled;

                _context.TuitionOffers.Update(job);
                await _context.SaveChangesAsync();
                await _commissionService.CreateInvoiceAsync(job.Id, job.Salary);

                TempData["SuccessMessage"] = "Tutor hired successfully! The job is now marked as Filled.";
            }
            else
            {
                TempData["ErrorMessage"] = "This job is no longer open.";
            }

            return RedirectToAction("Index", "TuitionRequest");
        }
    }
}
