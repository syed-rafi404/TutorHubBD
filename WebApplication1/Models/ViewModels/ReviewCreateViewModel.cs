using System.ComponentModel.DataAnnotations;

namespace TutorHubBD.Web.Models.ViewModels
{
    public class ReviewCreateViewModel
    {
        public int JobId { get; set; }
        
        public string JobTitle { get; set; }
        
        public string TutorName { get; set; }
        
        public int TutorId { get; set; }

        [Required(ErrorMessage = "Please select a rating")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        [Display(Name = "Rating")]
        public int Rating { get; set; }

        [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        [Display(Name = "Your Review")]
        public string Comment { get; set; }
    }
}
