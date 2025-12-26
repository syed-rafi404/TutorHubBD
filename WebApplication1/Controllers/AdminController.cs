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
    [Authorize(Roles = "Admin")]
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
                // Assuming there is a way to identify pending verifications, for now just getting unverified tutors
                RecentVerifications = await _context.Tutors
                    .Include(t => t.User)
                    .Where(t => !t.IsVerified)
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
    }
}
