using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TutorHubBD.Web.Models.ViewModels
{
    public class UserProfileViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Invalid Phone Number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Bio")]
        public string? Bio { get; set; }

        // Profile Picture
        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }

        public string? ProfilePictureUrl { get; set; }
    }
}
