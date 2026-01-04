using Stripe.Checkout;

namespace TutorHubBD.Web.Services
{
    public interface IPaymentService
    {
        Task<Session> CreateCheckoutSessionAsync(
            int invoiceId, 
            decimal amount, 
            string jobTitle, 
            string successUrl, 
            string cancelUrl);

        Task<bool> VerifyPaymentAsync(string sessionId);
    }
}
