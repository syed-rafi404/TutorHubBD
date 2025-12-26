using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TutorHubBD.Web.Models; 

namespace TutorHubBD.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Register the Tables here
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<TuitionOffer> TuitionOffers { get; set; }
        public DbSet<TuitionRequest> TuitionRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure TuitionRequest -> TuitionOffer relationship
            modelBuilder.Entity<TuitionRequest>()
                .HasOne(tr => tr.TuitionOffer)
                .WithMany()
                .HasForeignKey(tr => tr.TuitionOfferId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure TuitionRequest -> Tutor relationship
            modelBuilder.Entity<TuitionRequest>()
                .HasOne(tr => tr.Tutor)
                .WithMany()
                .HasForeignKey(tr => tr.TutorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure TuitionOffer -> HiredTutor relationship
            modelBuilder.Entity<TuitionOffer>()
                .HasOne(to => to.HiredTutor)
                .WithMany()
                .HasForeignKey(to => to.HiredTutorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Tutor -> User relationship
            modelBuilder.Entity<Tutor>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
