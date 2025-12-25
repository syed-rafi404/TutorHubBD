using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;

namespace TutorHubBD.Web.Services
{
    public class TuitionRequestService
    {
        private readonly ApplicationDbContext _context;

        public TuitionRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> UpdateStatusAsync(int requestId, string newStatus)
        {
            var request = await _context.TuitionRequests.FindAsync(requestId);
            if (request == null)
            {
                return false;
            }

            if (newStatus == "Accepted" || newStatus == "Rejected" || newStatus == "Pending")
            {
                request.Status = newStatus;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}
