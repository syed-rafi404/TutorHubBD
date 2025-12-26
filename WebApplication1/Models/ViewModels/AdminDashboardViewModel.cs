using System.Collections.Generic;
using TutorHubBD.Web.Models;

namespace TutorHubBD.Web.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalJobs { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<CommissionInvoice> PendingInvoices { get; set; } = new List<CommissionInvoice>();
        public List<Tutor> RecentVerifications { get; set; } = new List<Tutor>();
        public List<Tutor> PendingVerifications { get; set; } = new List<Tutor>();
    }
}
