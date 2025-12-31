using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Models.ViewModels;
using TutorHubBD.Web.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace TutorHubBD.Web.Controllers
{
    public class TuitionRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TuitionRequestService _tuitionRequestService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public TuitionRequestController(
            ApplicationDbContext context, 
            TuitionRequestService tuitionRequestService, 
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _tuitionRequestService = tuitionRequestService;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: Teacher's Applications Dashboard
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> MyApplications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Find the tutor profile for the current user
            var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tutor == null)
            {
                TempData["InfoMessage"] = "You don't have a tutor profile yet. Apply for a job to create one automatically.";
                return View(new List<MyApplicationsViewModel>());
            }

            // Get all applications for this tutor
            var applications = await _context.TuitionRequests
                .AsNoTracking()
                .Include(r => r.TuitionOffer)
                    .ThenInclude(o => o.Guardian)
                .Where(r => r.TutorId == tutor.TutorID)
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new MyApplicationsViewModel
                {
                    RequestId = r.RequestID,
                    JobTitle = r.TuitionOffer.Title ?? "Unknown Job",
                    GuardianName = r.TuitionOffer.Guardian != null ? r.TuitionOffer.Guardian.FullName ?? r.TuitionOffer.Guardian.UserName ?? "Unknown" : "Unknown",
                    Salary = r.TuitionOffer.Salary,
                    AppliedDate = r.RequestDate,
                    Status = r.TuitionOffer.HiredTutorId == tutor.TutorID ? "Hired" : r.Status ?? "Pending"
                })
                .ToListAsync();

            return View(applications);
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
            // Reload job with Guardian info for notifications
            var job = await _context.TuitionOffers
                .Include(j => j.Guardian)
                .FirstOrDefaultAsync(j => j.Id == request.TuitionOfferId);
            
            if (job != null)
                ViewData["JobTitle"] = job.Title;

            if (!string.IsNullOrEmpty(request.StudentName) && !string.IsNullOrEmpty(request.StudentEmail))
            {
                // Retrieve the currently logged-in User's ID
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "You must be logged in to apply for a job.";
                    return RedirectToAction("Login", "Account");
                }

                // Query the Tutors table to find the record where Tutor.UserId matches the logged-in ID
                var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserId == user.Id);

                // If NO Tutor record is found, auto-create one for the user
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

                // Assign TutorId and save the application
                request.TutorId = tutor.TutorID;
                request.Status = "Pending";
                request.RequestDate = DateTime.Now;

                _context.TuitionRequests.Add(request);
                await _context.SaveChangesAsync();

                var applicantName = user.FullName ?? user.UserName ?? "A tutor";
                var guardianName = job?.Guardian?.FullName ?? job?.Guardian?.UserName ?? "The guardian";

                // Send notifications to the GUARDIAN (Job Owner)
                if (job?.GuardianId != null)
                {
                    await _notificationService.SendInAppNotificationAsync(
                        job.GuardianId,
                        "New Application Received",
                        $"{applicantName} has applied for your job: \"{job.Title}\".",
                        "/TuitionOffer/ViewApplicants?jobId=" + job.Id
                    );

                    if (!string.IsNullOrEmpty(job.Guardian?.Email))
                    {
                        var guardianEmailBody = $@"
                            <h2>📩 New Application for Your Tuition Job</h2>
                            <p>Hello {guardianName},</p>
                            <p><strong>{applicantName}</strong> has applied for your tuition job.</p>
                            <table style='border-collapse: collapse; margin: 20px 0; width: 100%;'>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Job Title:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{job.Title}</td></tr>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Applicant Name:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{request.StudentName}</td></tr>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Applicant Email:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{request.StudentEmail}</td></tr>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Message:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{request.Message ?? "No message provided"}</td></tr>
                            </table>
                            <p>Log in to TutorHubBD to review this application and hire a tutor.</p>
                            <p>Best regards,<br/>TutorHubBD Team</p>
                        ";

                        await _notificationService.SendEmailAsync(
                            job.Guardian.Email,
                            $"📩 New Application for \"{job.Title}\" - TutorHubBD",
                            guardianEmailBody
                        );
                    }
                }

                // Send confirmation to the TEACHER (Applicant)
                await _notificationService.SendInAppNotificationAsync(
                    user.Id,
                    "Application Submitted Successfully",
                    $"Your application for \"{job?.Title}\" has been submitted. The guardian will review it soon.",
                    "/TuitionRequest/MyApplications"
                );

                if (!string.IsNullOrEmpty(user.Email))
                {
                    var teacherEmailBody = $@"
                        <h2>✅ Application Submitted Successfully</h2>
                        <p>Hello {user.FullName ?? user.UserName},</p>
                        <p>Your application has been successfully submitted. Here are the details:</p>
                        <table style='border-collapse: collapse; margin: 20px 0; width: 100%;'>
                            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Job Title:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{job?.Title}</td></tr>
                            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Location:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{job?.Location}, {job?.City}</td></tr>
                            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Monthly Salary:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>৳{job?.Salary}</td></tr>
                            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Posted By:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{guardianName}</td></tr>
                            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Status:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>Pending Review</td></tr>
                        </table>
                        <p><strong>What's Next?</strong></p>
                        <ol>
                            <li>The guardian will review your application.</li>
                            <li>If selected, you will receive a hiring notification.</li>
                            <li>Check your dashboard regularly for updates.</li>
                        </ol>
                        <p>Log in to TutorHubBD to track your applications.</p>
                        <p>Best regards,<br/>TutorHubBD Team</p>
                    ";

                    await _notificationService.SendEmailAsync(
                        user.Email,
                        $"✅ Application Submitted for \"{job?.Title}\" - TutorHubBD",
                        teacherEmailBody
                    );
                }

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
                TempData["SuccessMessage"] = $"Application status updated to {status}.";
            else
                TempData["ErrorMessage"] = "Application not found or invalid status.";

            return RedirectToAction(nameof(Index));
        }
    }
}
