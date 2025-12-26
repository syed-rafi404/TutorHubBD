using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Models.ViewModels;

namespace TutorHubBD.Web.Controllers
{
    ///[Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalJobs = await _context.TuitionOffers.CountAsync(),
                TotalRevenue = await _context.CommissionInvoices.SumAsync(i => i.Amount),
                PendingInvoices = await _context.CommissionInvoices
                    .Include(i => i.Tutor)
                    .ThenInclude(t => t.User)
                    .Where(i => i.Status == InvoiceStatus.Pending)
                    .ToListAsync(),
                // Get tutors who have uploaded verification documents but are not yet verified
                PendingVerifications = await _context.Tutors
                    .Include(t => t.User)
                    .Where(t => !t.IsVerified && 
                                t.VerificationDocumentPath != null && 
                                t.VerificationRequestDate != null)
                    .OrderByDescending(t => t.VerificationRequestDate)
                    .ToListAsync(),
                // Recently verified tutors
                RecentVerifications = await _context.Tutors
                    .Include(t => t.User)
                    .Where(t => t.IsVerified)
                    .OrderByDescending(t => t.TutorID)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var invoice = await _context.CommissionInvoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            invoice.Status = InvoiceStatus.Paid;
            _context.Update(invoice);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTutor(int tutorId)
        {
            var tutor = await _context.Tutors.FindAsync(tutorId);
            if (tutor == null)
            {
                return NotFound();
            }

            tutor.IsVerified = true;
            _context.Update(tutor);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Tutor has been verified successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVerification(int tutorId)
        {
            var tutor = await _context.Tutors.FindAsync(tutorId);
            if (tutor == null)
            {
                return NotFound();
            }

            // Clear verification request
            tutor.VerificationDocumentPath = null;
            tutor.VerificationRequestDate = null;
            tutor.IsVerified = false;
            
            _context.Update(tutor);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Verification request has been rejected.";
            return RedirectToAction(nameof(Index));
        }
    }
}
