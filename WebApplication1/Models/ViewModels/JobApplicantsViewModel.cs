using System.Collections.Generic;

namespace TutorHubBD.Web.Models.ViewModels
{
    public class JobApplicantsViewModel
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public JobStatus JobStatus { get; set; }
        public List<ApplicantDetail> Applicants { get; set; } = new();
    }

    public class ApplicantDetail
    {
        public int TutorId { get; set; }
        public int RequestId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public int? Experience { get; set; }
        public string? Subjects { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Education { get; set; }
        public bool IsVerified { get; set; }
    }
}
