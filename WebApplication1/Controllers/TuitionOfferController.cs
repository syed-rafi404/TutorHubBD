using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;

namespace TutorHubBD.Web.Controllers
{
    [AllowAnonymous]
    public class TuitionOfferController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TuitionOfferController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TuitionOffer/Index (Search Engine)
        public async Task<IActionResult> Index(string searchCity, string searchMedium, string searchClass)
        {
            var jobs = from j in _context.TuitionOffers
                       select j;

            if (!string.IsNullOrEmpty(searchCity))
            {
                jobs = jobs.Where(s => s.City.Contains(searchCity));
            }

            if (!string.IsNullOrEmpty(searchMedium))
            {
                jobs = jobs.Where(s => s.Medium.Contains(searchMedium));
            }

            if (!string.IsNullOrEmpty(searchClass))
            {
                jobs = jobs.Where(s => s.StudentClass.Contains(searchClass));
            }

            ViewData["CurrentCity"] = searchCity;
            ViewData["CurrentMedium"] = searchMedium;
            ViewData["CurrentClass"] = searchClass;

            return View(await jobs.ToListAsync());
        }

        // GET: TuitionOffer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TuitionOffer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Salary,City,Location,Medium,StudentClass")] TuitionOffer offer)
        {
            // FIX 1: Remove validation for fields not in the form
            ModelState.Remove("Subject");
            ModelState.Remove("DaysPerWeek");
            ModelState.Remove("GenderPreference");

            // FIX 2: Assign DEFAULT values for database columns that cannot be null
            // This prevents the SqlException
            offer.Subject = "General"; 
            offer.DaysPerWeek = "Negotiable";
            offer.GenderPreference = "Any";

            if (!ModelState.IsValid)
            {
                return View(offer);
            }

            _context.Add(offer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: TuitionOffer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var offer = await _context.TuitionOffers.FindAsync(id);
            if (offer != null)
            {
                _context.TuitionOffers.Remove(offer);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
