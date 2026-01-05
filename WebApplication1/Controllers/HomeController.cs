using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Models.ViewModels;
using TutorHubBD.Web.Services;

namespace TutorHubBD.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAiSearchService _aiSearchService;

        public HomeController(
            ILogger<HomeController> logger, 
            UserManager<ApplicationUser> userManager, 
            ApplicationDbContext context,
            IAiSearchService aiSearchService)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _aiSearchService = aiSearchService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // POST: /Home/AiSearch - For Guardians to find tutors
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AiSearch(string prompt)
        {
            var viewModel = new AiSearchResultsViewModel
            {
                OriginalQuery = prompt
            };

            if (string.IsNullOrWhiteSpace(prompt))
            {
                viewModel.ErrorMessage = "Please enter a description of the tutor you're looking for.";
                return View("AiSearchResults", viewModel);
            }

            try
            {
                // Extract search criteria using AI
                var criteria = await _aiSearchService.ExtractSearchCriteriaAsync(prompt);
                viewModel.ExtractedCriteria = criteria;

                // Search for matching tutors
                var results = await _aiSearchService.SearchTutorsAsync(criteria);
                viewModel.Results = results;

                if (!results.Any())
                {
                    viewModel.ErrorMessage = "No tutors found matching your criteria. Try broadening your search.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI search");
                viewModel.ErrorMessage = "An error occurred while searching. Please try again.";
                viewModel.UsedFallback = true;
            }

            return View("AiSearchResults", viewModel);
        }

        // GET: /Home/AiSearch (for direct access)
        [HttpGet]
        public IActionResult AiSearch()
        {
            return View("AiSearchResults", new AiSearchResultsViewModel());
        }

        // POST: /Home/AiJobSearch - For Teachers to find jobs
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AiJobSearch(string prompt)
        {
            var viewModel = new AiJobSearchResultsViewModel
            {
                OriginalQuery = prompt
            };

            if (string.IsNullOrWhiteSpace(prompt))
            {
                viewModel.ErrorMessage = "Please describe what kind of tuition job you're looking for.";
                return View("AiJobSearchResults", viewModel);
            }

            try
            {
                // Extract job search criteria using AI
                var criteria = await _aiSearchService.ExtractJobSearchCriteriaAsync(prompt);
                viewModel.ExtractedCriteria = criteria;

                // Search for matching jobs
                var results = await _aiSearchService.SearchJobsAsync(criteria);
                viewModel.Results = results;

                if (!results.Any())
                {
                    viewModel.ErrorMessage = "No jobs found matching your criteria. Try broadening your search or check back later for new postings.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI job search");
                viewModel.ErrorMessage = "An error occurred while searching. Please try again.";
                viewModel.UsedFallback = true;
            }

            return View("AiJobSearchResults", viewModel);
        }

        // GET: /Home/AiJobSearch (for direct access by Teachers)
        [HttpGet]
        [Authorize(Roles = "Teacher")]
        public IActionResult AiJobSearch()
        {
            return View("AiJobSearchResults", new AiJobSearchResultsViewModel());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
