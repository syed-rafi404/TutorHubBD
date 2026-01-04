using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TutorHubBD.Web.Configuration;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Services;

namespace TutorHubBD.Web.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StripeSettings _stripeSettings;

        public PaymentController(
            ApplicationDbContext context,
            IPaymentService paymentService,
            UserManager<ApplicationUser> userManager,
            IOptions<StripeSettings> stripeSettings)
        {
            _context = context;
            _paymentService = paymentService;
            _userManager = userManager;
            _stripeSettings = stripeSettings.Value;
        }

        // GET: Payment/MyInvoices - Shows all invoices for the current tutor
        public async Task<IActionResult> MyInvoices()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var tutor = await _context.Tutors
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tutor == null)
            {
                TempData["ErrorMessage"] = "You don't have a tutor profile yet.";
                return RedirectToAction("Index", "Home");
            }

            var invoices = await _context.CommissionInvoices
                .Include(i => i.Job)
                .Where(i => i.TutorId == tutor.TutorID)
                .OrderByDescending(i => i.GeneratedDate)
                .ToListAsync();

            // Pass the publishable key to the view for client-side Stripe usage if needed
            ViewData["StripePublishableKey"] = _stripeSettings.PublishableKey;

            return View(invoices);
        }

        // POST: Payment/Pay - Initiates a Stripe Checkout Session
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int invoiceId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var tutor = await _context.Tutors
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tutor == null)
            {
                TempData["ErrorMessage"] = "You don't have a tutor profile.";
                return RedirectToAction("MyInvoices");
            }

            var invoice = await _context.CommissionInvoices
                .Include(i => i.Job)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);

            if (invoice == null)
            {
                TempData["ErrorMessage"] = "Invoice not found.";
                return RedirectToAction("MyInvoices");
            }

            // Security: Verify the invoice belongs to the current tutor
            if (invoice.TutorId != tutor.TutorID)
            {
                TempData["ErrorMessage"] = "You are not authorized to pay this invoice.";
                return RedirectToAction("MyInvoices");
            }

            // Check if already paid
            if (invoice.Status == InvoiceStatus.Paid)
            {
                TempData["InfoMessage"] = "This invoice has already been paid.";
                return RedirectToAction("MyInvoices");
            }

            // Build the success and cancel URLs
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/Payment/Success?invoiceId={invoiceId}&session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{baseUrl}/Payment/Cancel?invoiceId={invoiceId}";

            try
            {
                var session = await _paymentService.CreateCheckoutSessionAsync(
                    invoiceId,
                    invoice.Amount,
                    invoice.Job?.Title ?? "Tuition Job",
                    successUrl,
                    cancelUrl
                );

                // Redirect to Stripe's hosted checkout page
                return Redirect(session.Url);
            }
            catch (Stripe.StripeException ex)
            {
                TempData["ErrorMessage"] = $"Payment initialization failed: {ex.Message}";
                return RedirectToAction("MyInvoices");
            }
        }

        // GET: Payment/Success - Handles successful payment redirect
        public async Task<IActionResult> Success(int invoiceId, string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
            {
                TempData["ErrorMessage"] = "Invalid payment session.";
                return RedirectToAction("MyInvoices");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var tutor = await _context.Tutors
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tutor == null)
            {
                TempData["ErrorMessage"] = "Tutor profile not found.";
                return RedirectToAction("MyInvoices");
            }

            var invoice = await _context.CommissionInvoices
                .Include(i => i.Job)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId);

            if (invoice == null)
            {
                TempData["ErrorMessage"] = "Invoice not found.";
                return RedirectToAction("MyInvoices");
            }

            // Security: Verify the invoice belongs to the current tutor
            if (invoice.TutorId != tutor.TutorID)
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("MyInvoices");
            }

            // Verify the payment was successful with Stripe
            try
            {
                var paymentSuccessful = await _paymentService.VerifyPaymentAsync(session_id);

                if (paymentSuccessful)
                {
                    // Update the invoice status to Paid
                    invoice.Status = InvoiceStatus.Paid;
                    _context.Update(invoice);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Payment successful! Invoice #{invoiceId} for BDT {invoice.Amount:F2} has been marked as Paid.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Payment verification failed. Please contact support if you were charged.";
                }
            }
            catch (Stripe.StripeException ex)
            {
                TempData["ErrorMessage"] = $"Error verifying payment: {ex.Message}";
            }

            return RedirectToAction("MyInvoices");
        }

        // GET: Payment/Cancel - Handles cancelled payment
        public IActionResult Cancel(int invoiceId)
        {
            TempData["InfoMessage"] = $"Payment for Invoice #{invoiceId} was cancelled. You can try again anytime.";
            return RedirectToAction("MyInvoices");
        }
    }
}
