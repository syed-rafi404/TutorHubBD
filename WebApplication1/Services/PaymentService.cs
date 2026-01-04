using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using TutorHubBD.Web.Configuration;

namespace TutorHubBD.Web.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly StripeSettings _stripeSettings;

        public PaymentService(IOptions<StripeSettings> stripeSettings)
        {
            _stripeSettings = stripeSettings.Value;
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        public async Task<Session> CreateCheckoutSessionAsync(
            int invoiceId,
            decimal amount,
            string jobTitle,
            string successUrl,
            string cancelUrl)
        {
            // Convert BDT to the smallest currency unit (paisa = BDT * 100)
            // Stripe expects amounts in the smallest currency unit
            var amountInPaisa = (long)(amount * 100);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = amountInPaisa,
                            Currency = "bdt", // Bangladeshi Taka
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Commission Payment - Invoice #{invoiceId}",
                                Description = $"Platform commission for tuition job: {jobTitle}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "invoiceId", invoiceId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return session;
        }

        public async Task<bool> VerifyPaymentAsync(string sessionId)
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            // Check if the payment was successful
            return session.PaymentStatus == "paid";
        }
    }
}
