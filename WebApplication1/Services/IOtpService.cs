using System.Threading.Tasks;

namespace TutorHubBD.Web.Services
{
    public interface IOtpService
    {
        Task<bool> GenerateAndSendOtpAsync(string email, string purpose);
        Task<bool> VerifyOtpAsync(string email, string code, string purpose);
        Task InvalidateExistingOtpsAsync(string email, string purpose);
    }
}
