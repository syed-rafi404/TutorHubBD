using System;
using System.Threading.Tasks;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace TutorHubBD.Web.Services
{
    public class CommissionService : ICommissionService
    {
        private readonly ApplicationDbContext _context;

        public CommissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateInvoiceAsync(int jobId, decimal salary)
        {
            decimal amount = salary * 0.40m;

            var job = await _context.TuitionOffers
                .Include(j => j.HiredTutor)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
                throw new ArgumentException($"Job with ID {jobId} not found.");

            if (job.HiredTutorId == null)
                throw new InvalidOperationException($"Job with ID {jobId} does not have a hired tutor.");

            var invoice = new CommissionInvoice
            {
                TutorId = job.HiredTutorId.Value,
                JobId = jobId,
                Amount = amount,
                Status = InvoiceStatus.Pending,
                GeneratedDate = DateTime.Now
            };

            _context.Add(invoice);
            await _context.SaveChangesAsync();
        }
    }
}
