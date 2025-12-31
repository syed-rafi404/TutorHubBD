using System;

namespace TutorHubBD.Web.Models.ViewModels
{
    public class MyApplicationsViewModel
    {
        public int RequestId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string GuardianName { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime AppliedDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
