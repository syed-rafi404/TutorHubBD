using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorHubBD.Web.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int JobId { get; set; }

        [ForeignKey("JobId")]
        public TuitionOffer Job { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // The guardian who wrote the review
        public string? ReviewerId { get; set; }

        [ForeignKey("ReviewerId")]
        public ApplicationUser Reviewer { get; set; }

        // The tutor being reviewed
        public int? TutorId { get; set; }

        [ForeignKey("TutorId")]
        public Tutor Tutor { get; set; }
    }
}
