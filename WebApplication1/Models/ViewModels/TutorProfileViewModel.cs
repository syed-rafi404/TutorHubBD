using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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
        
        [Display(Name = "Bio")]
        [MinLength(100, ErrorMessage = "Bio must be at least 100 characters")]
        public string? Bio { get; set; }
        
        // For file upload
        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }
        
        // Current profile picture URL
        public string? ProfilePictureUrl { get; set; }
        
        // For multi-select checkboxes - selected classes
        [Display(Name = "Preferred Classes")]
        public List<string> SelectedClasses { get; set; } = new List<string>();
        
        // Available class options for the checkboxes
        public string[] AvailableClasses => Tutor.AvailableClasses;
        
        public float Rating { get; set; }
        
        public bool IsVerified { get; set; }
        
        public bool IsProfileComplete { get; set; }
        
        public string? VerificationDocumentPath { get; set; }
        
        public DateTime? VerificationRequestDate { get; set; }
        
        public bool HasPendingVerification => 
            !string.IsNullOrEmpty(VerificationDocumentPath) && 
            VerificationRequestDate.HasValue && 
            !IsVerified;
        
        // Helper to get PreferredClasses as comma-separated string
        public string GetPreferredClassesString()
        {
            return string.Join(",", SelectedClasses);
        }
        
        // Helper to set SelectedClasses from comma-separated string
        public void SetSelectedClassesFromString(string? preferredClasses)
        {
            if (!string.IsNullOrEmpty(preferredClasses))
            {
                SelectedClasses = preferredClasses.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .ToList();
            }
        }
    }
}
