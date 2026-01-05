using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorHubBD.Web.Models
{
    public class Tutor
    {
        [Key]
        public int TutorID { get; set; }
        
        public string Education { get; set; }
        
        public string Subjects { get; set; }
        
        public float Rating { get; set; }
        
        public bool IsVerified { get; set; }
        
        // Experience in years
        [Display(Name = "Years of Experience")]
        public int? Experience { get; set; }
        
        // Preferred teaching locations (e.g., "Dhaka, Chittagong, Online")
        [Display(Name = "Preferred Locations")]
        public string? PreferredLocations { get; set; }
        
        // Bio with minimum length requirement
        [Display(Name = "Bio")]
        [MinLength(100, ErrorMessage = "Bio must be at least 100 characters")]
        public string? Bio { get; set; }
        
        // Profile picture URL (required for profile completeness)
        [Display(Name = "Profile Picture")]
        public string? ProfilePictureUrl { get; set; }
        
        // Preferred classes stored as comma-separated string (e.g., "Class 5,Class 8,HSC")
        [Display(Name = "Preferred Classes")]
        public string? PreferredClasses { get; set; }
        
        // Profile completeness flag
        public bool IsProfileComplete { get; set; }
        
        // Verification properties
        public string? VerificationDocumentPath { get; set; }
        public DateTime? VerificationRequestDate { get; set; }
        
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        
        // Helper method to check profile completeness
        public bool CheckProfileCompleteness()
        {
            return IsVerified &&
                   !string.IsNullOrEmpty(ProfilePictureUrl) &&
                   !string.IsNullOrEmpty(Bio) && Bio.Length >= 100 &&
                   !string.IsNullOrEmpty(PreferredClasses);
        }
        
        // Static list of available class options
        public static readonly string[] AvailableClasses = new[]
        {
            "Nursery", "KG", "Class 1", "Class 2", "Class 3", "Class 4", "Class 5",
            "Class 6", "Class 7", "Class 8", "Class 9", "Class 10", "HSC", "A-Level"
        };
    }
}
