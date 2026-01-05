using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;

namespace TutorHubBD.Web.Services
{
    public class TuitionRequestService
    {
        private readonly ApplicationDbContext _context;
        private const int MaxShortlistLimit = 5;

        public TuitionRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateStatusWithValidationAsync(int requestId, string newStatus)
        {
            var request = await _context.TuitionRequests
                .Include(r => r.TuitionOffer)
                .FirstOrDefaultAsync(r => r.RequestID == requestId);
                
            if (request == null)
            {
                return (false, "Application not found.");
            }

            if (newStatus != "Accepted" && newStatus != "Rejected" && newStatus != "Pending")
            {
                return (false, "Invalid status.");
            }

            if (newStatus == "Accepted")
            {
                var currentShortlistCount = await _context.TuitionRequests
                    .CountAsync(r => r.TuitionOfferId == request.TuitionOfferId && r.Status == "Accepted");

                if (currentShortlistCount >= MaxShortlistLimit)
                {
                    return (false, $"You have already shortlisted {MaxShortlistLimit} applicants for this job. Please reject or hire from your current shortlist before adding more.");
                }
            }

            request.Status = newStatus;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> UpdateStatusAsync(int requestId, string newStatus)
        {
            var result = await UpdateStatusWithValidationAsync(requestId, newStatus);
            return result.Success;
        }
        
        public async Task<int> GetShortlistCountAsync(int jobId)
        {
            return await _context.TuitionRequests
                .CountAsync(r => r.TuitionOfferId == jobId && r.Status == "Accepted");
        }
        
        public async Task<int> GetRemainingShortlistSlotsAsync(int jobId)
        {
            var currentCount = await GetShortlistCountAsync(jobId);
            return Math.Max(0, MaxShortlistLimit - currentCount);
        }
    }
}
