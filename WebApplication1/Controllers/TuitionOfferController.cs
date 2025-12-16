using Microsoft.AspNetCore.Mvc;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using System.Linq;

namespace TutorHubBD.Web.Controllers
{
    public class TuitionOfferController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TuitionOfferController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TuitionOffer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TuitionOffer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TuitionOffer offer)
        {
            if (ModelState.IsValid)
            {
                // offer.TutorID = User.Identity.GetUserId(); 

                _context.Add(offer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(offer);
        }

        // GET: TuitionOffer/Index (List of Jobs)
        public IActionResult Index()
        {
            return View(_context.TuitionOffers.ToList());
        }
        // POST: TuitionOffer/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
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
