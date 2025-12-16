using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TutorHubBD.Web.Models; 

namespace TutorHubBD.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Register the Tables here
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<TuitionOffer> TuitionOffers { get; set; }
    }
}
