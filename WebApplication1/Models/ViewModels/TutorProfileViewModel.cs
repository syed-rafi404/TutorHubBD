using System.ComponentModel.DataAnnotations;

namespace TutorHubBD.Web.Models.ViewModels
{
    public class TutorProfileViewModel
    {
        public int TutorID { get; set; }
        
        [Display(Name = "Education")]
        public string? Education { get; set; }
        
        [Display(Name = "Subjects")]
        public string? Subjects { get; set; }
        
        [Display(Name = "Years of Experience")]
        [Range(0, 50, ErrorMessage = "Experience must be between 0 and 50 years")]
        public int? Experience { get; set; }
        
        [Display(Name = "Preferred Locations")]
        public string? PreferredLocations { get; set; }
        
        public float Rating { get; set; }
        
        public bool IsVerified { get; set; }
        
        public string? VerificationDocumentPath { get; set; }
        
        public DateTime? VerificationRequestDate { get; set; }
        
        public bool HasPendingVerification => 
            !string.IsNullOrEmpty(VerificationDocumentPath) && 
            VerificationRequestDate.HasValue && 
            !IsVerified;
    }
}
