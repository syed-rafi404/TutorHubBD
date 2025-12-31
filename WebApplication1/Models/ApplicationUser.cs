using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TutorHubBD.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        [StringLength(100)]
        public string FullName { get; set; }

        [PersonalData]
        public string Address { get; set; }

        [PersonalData]
        public string Bio { get; set; }

        [PersonalData]
        public string? ProfilePictureUrl { get; set; }
    }
}
