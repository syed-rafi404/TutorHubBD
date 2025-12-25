using System.ComponentModel.DataAnnotations;

namespace TutorHubBD.Web.Models.ViewModels
{
    public class TuitionOfferCreateViewModel
    {
        [Required(ErrorMessage = "Please enter a title")]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(1000, 50000, ErrorMessage = "Salary must be between 1000 and 50000")]
        public int Salary { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string Medium { get; set; }

        [Required]
        public string StudentClass { get; set; }
    }
}
