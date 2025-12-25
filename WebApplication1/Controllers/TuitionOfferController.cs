using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Models.ViewModels;
using TutorHubBD.Web.Services;

namespace TutorHubBD.Web.Controllers
{
    [AllowAnonymous]
    public class TuitionOfferController : Controller
    {
        private readonly ITuitionOfferService _service;

        public TuitionOfferController(ITuitionOfferService service)
        {
            _service = service;
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
    }
}
