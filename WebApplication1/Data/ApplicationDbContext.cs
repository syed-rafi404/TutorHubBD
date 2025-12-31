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

        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<TuitionOffer> TuitionOffers { get; set; }
        public DbSet<TuitionRequest> TuitionRequests { get; set; }
        public DbSet<CommissionInvoice> CommissionInvoices { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TuitionRequest>()
                .HasOne(tr => tr.TuitionOffer)
                .WithMany()
                .HasForeignKey(tr => tr.TuitionOfferId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TuitionRequest>()
                .HasOne(tr => tr.Tutor)
                .WithMany()
                .HasForeignKey(tr => tr.TutorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TuitionOffer>()
                .HasOne(to => to.HiredTutor)
                .WithMany()
                .HasForeignKey(to => to.HiredTutorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TuitionOffer>()
                .HasOne(to => to.Guardian)
                .WithMany()
                .HasForeignKey(to => to.GuardianId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tutor>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommissionInvoice>()
                .HasOne(ci => ci.Tutor)
                .WithMany()
                .HasForeignKey(ci => ci.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommissionInvoice>()
                .HasOne(ci => ci.Job)
                .WithMany()
                .HasForeignKey(ci => ci.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Job)
                .WithMany()
                .HasForeignKey(r => r.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Tutor)
                .WithMany()
                .HasForeignKey(r => r.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
