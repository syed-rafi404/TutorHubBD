using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorHubBD.Web.Models
{
    public enum InvoiceStatus
    {
        Pending,
        Paid,
        Overdue
    }

    public class CommissionInvoice
    {
        [Key]
        public int InvoiceID { get; set; }

        public int TutorId { get; set; }
        [ForeignKey("TutorId")]
        public Tutor Tutor { get; set; }

        public int JobId { get; set; }
        [ForeignKey("JobId")]
        public TuitionOffer Job { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        public DateTime GeneratedDate { get; set; } = DateTime.Now;
    }
}
