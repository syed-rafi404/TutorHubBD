using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Models.ViewModels;
using TutorHubBD.Web.Services;

namespace TutorHubBD.Web.Controllers
{
    public class TuitionOfferController : Controller
    {
        private readonly ITuitionOfferService _service;
        private readonly ApplicationDbContext _context;
        private readonly ICommissionService _commissionService;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TuitionOfferController(
            ITuitionOfferService service, 
            ApplicationDbContext context, 
            ICommissionService commissionService,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _service = service;
            _context = context;
            _commissionService = commissionService;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [Authorize(Roles = "Guardian, Teacher")]
        public async Task<IActionResult> Index(string searchCity, string searchMedium, string searchClass)
        {
            var jobs = await _service.SearchOffersAsync(searchCity, searchMedium, searchClass);

            ViewData["CurrentCity"] = searchCity;
            ViewData["CurrentMedium"] = searchMedium;
            ViewData["CurrentClass"] = searchClass;

            return View(jobs);
        }

        [Authorize(Roles = "Guardian")]
        public async Task<IActionResult> MyJobs()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var myJobs = await _context.TuitionOffers
                .Include(j => j.HiredTutor)
                    .ThenInclude(t => t.User)
                .Where(j => j.GuardianId == user.Id)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            var jobIds = myJobs.Select(j => j.Id).ToList();
            var reviewedJobIds = await _context.Reviews
                .Where(r => jobIds.Contains(r.JobId))
                .Select(r => r.JobId)
                .ToListAsync();

            ViewData["ReviewedJobIds"] = reviewedJobIds;

            return View(myJobs);
        }

        [Authorize(Roles = "Guardian")]
        public async Task<IActionResult> ViewApplicants(int jobId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var job = await _context.TuitionOffers
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
                return NotFound();

            if (job.GuardianId != user.Id)
                return Forbid();

            var applicants = await _context.TuitionRequests
                .Include(tr => tr.Tutor)
                    .ThenInclude(t => t.User)
                .Where(tr => tr.TuitionOfferId == jobId && tr.TutorId != null)
                .Select(tr => new ApplicantDetail
                {
                    TutorId = tr.TutorId ?? 0,
                    RequestId = tr.RequestID,
                    Name = tr.Tutor != null && tr.Tutor.User != null 
                        ? tr.Tutor.User.FullName 
                        : tr.StudentName,
                    ProfilePictureUrl = tr.Tutor != null && tr.Tutor.User != null 
                        ? tr.Tutor.User.ProfilePictureUrl 
                        : null,
                    Experience = tr.Tutor != null ? tr.Tutor.Experience : null,
                    Subjects = tr.Tutor != null ? tr.Tutor.Subjects : null,
                    Education = tr.Tutor != null ? tr.Tutor.Education : null,
                    Status = tr.Status,
                    IsVerified = tr.Tutor != null && tr.Tutor.IsVerified
                })
                .ToListAsync();

            var viewModel = new JobApplicantsViewModel
            {
                JobId = job.Id,
                JobTitle = job.Title,
                JobStatus = job.Status,
                Applicants = applicants
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Guardian")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Guardian")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TuitionOfferCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

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
                GenderPreference = "Any",
                GuardianId = user?.Id
            };

            await _service.CreateOfferAsync(offer);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Guardian")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _service.DeleteOfferAsync(id);
            return RedirectToAction(nameof(MyJobs));
        }

        [HttpPost]
        [Authorize(Roles = "Guardian")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmHiring(int jobId, int tutorId)
        {
            var job = await _context.TuitionOffers
                .Include(j => j.Guardian)
                .FirstOrDefaultAsync(j => j.Id == jobId);
                
            if (job == null)
                return NotFound();

            if (tutorId <= 0)
            {
                TempData["ErrorMessage"] = "Cannot hire: The applicant does not have a valid tutor profile linked to their application.";
                return RedirectToAction("Index", "TuitionRequest");
            }

            var tutor = await _context.Tutors
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TutorID == tutorId);
                
            if (tutor == null)
            {
                TempData["ErrorMessage"] = "Cannot hire: The specified tutor profile does not exist.";
                return RedirectToAction("Index", "TuitionRequest");
            }

            if (job.Status == JobStatus.Open)
            {
                job.HiredTutorId = tutorId;
                job.Status = JobStatus.Filled;

                _context.TuitionOffers.Update(job);
                await _context.SaveChangesAsync();
                await _commissionService.CreateInvoiceAsync(job.Id, job.Salary);

                var commissionAmount = job.Salary * 0.40m;

                var tutorUser = await _userManager.FindByIdAsync(tutor.UserId);
                var tutorEmail = tutorUser?.Email;
                var tutorName = tutorUser?.FullName ?? tutorUser?.UserName ?? "A tutor";
                var tutorPhone = tutorUser?.PhoneNumber;

                var guardianName = job.Guardian?.FullName ?? job.Guardian?.UserName ?? "Guardian";
                var guardianEmail = job.Guardian?.Email;

                // Send notifications to the TUTOR (Teacher)
                if (!string.IsNullOrEmpty(tutor.UserId))
                {
                    await _notificationService.SendInAppNotificationAsync(
                        tutor.UserId,
                        "Congratulations! You've Been Hired!",
                        $"You have been hired for the job: \"{job.Title}\". Salary: ৳{job.Salary}. Please pay the commission of ৳{commissionAmount:F2}.",
                        "/TuitionRequest/MyApplications"
                    );

                    if (!string.IsNullOrEmpty(tutorEmail))
                    {
                        var tutorEmailBody = $@"
                            <h2>🎉 Congratulations! You've Been Hired!</h2>
                            <p>Hello {tutorName},</p>
                            <p>Great news! <strong>{guardianName}</strong> has hired you for the following tuition job:</p>
                            <table style='border-collapse: collapse; margin: 20px 0; width: 100%;'>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Job Title:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{job.Title}</td></tr>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Location:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{job.Location}, {job.City}</td></tr>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Monthly Salary:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>৳{job.Salary}</td></tr>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Commission (40%):</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>৳{commissionAmount:F2}</td></tr>
                            </table>
                            <p><strong>Next Steps:</strong></p>
                            <ol>
                                <li>Contact the guardian to arrange your first session.</li>
                                <li>Pay the platform commission of ৳{commissionAmount:F2} to complete the process.</li>
                            </ol>
                            <p>Log in to TutorHubBD to view your applications and invoices.</p>
                            <p>Best regards,<br/>TutorHubBD Team</p>
                        ";

                        await _notificationService.SendEmailAsync(
                            tutorEmail,
                            $"🎉 You've Been Hired for \"{job.Title}\" - TutorHubBD",
                            tutorEmailBody
                        );
                    }

                    if (!string.IsNullOrEmpty(tutorPhone))
                    {
                        await _notificationService.SendSmsAsync(
                            tutorPhone,
                            $"TutorHubBD: Congratulations! You've been hired for \"{job.Title}\". Salary: Tk{job.Salary}. Commission: Tk{commissionAmount:F2}. Login to view details."
                        );
                    }
                }

                // Send notifications to the GUARDIAN (Job Owner)
                if (!string.IsNullOrEmpty(job.GuardianId))
                {
                    await _notificationService.SendInAppNotificationAsync(
                        job.GuardianId,
                        "Tutor Hired Successfully!",
                        $"You have successfully hired {tutorName} for \"{job.Title}\". The job is now marked as Filled.",
                        "/TuitionOffer/MyJobs"
                    );

                    if (!string.IsNullOrEmpty(guardianEmail))
                    {
                        var guardianEmailBody = $@"
                            <h2>✅ Tutor Hired Successfully!</h2>
                            <p>Hello {guardianName},</p>
                            <p>You have successfully hired a tutor for your job. Here are the details:</p>
                            <table style='border-collapse: collapse; margin: 20px 0; width: 100%;'>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Job Title:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{job.Title}</td></tr>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Hired Tutor:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{tutorName}</td></tr>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Tutor Email:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{tutorEmail ?? "N/A"}</td></tr>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Tutor Phone:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{tutorPhone ?? "N/A"}</td></tr>
                                <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Monthly Salary:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>৳{job.Salary}</td></tr>
                            </table>
                            <p><strong>Next Steps:</strong></p>
                            <ol>
                                <li>Contact the tutor to arrange the first session.</li>
                                <li>You can leave a review after the tuition begins.</li>
                            </ol>
                            <p>Log in to TutorHubBD to manage your jobs.</p>
                            <p>Best regards,<br/>TutorHubBD Team</p>
                        ";

                        await _notificationService.SendEmailAsync(
                            guardianEmail,
                            $"✅ Tutor Hired for \"{job.Title}\" - TutorHubBD",
                            guardianEmailBody
                        );
                    }
                }

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
