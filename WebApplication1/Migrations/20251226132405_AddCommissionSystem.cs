using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorHubBD.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TuitionOffers_Tutors_HiredTutorId",
                table: "TuitionOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_TuitionRequests_Tutors_TutorId",
                table: "TuitionRequests");

            migrationBuilder.CreateTable(
                name: "CommissionInvoices",
                columns: table => new
                {
                    InvoiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TutorId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionInvoices", x => x.InvoiceID);
                    table.ForeignKey(
                        name: "FK_CommissionInvoices_TuitionOffers_JobId",
                        column: x => x.JobId,
                        principalTable: "TuitionOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommissionInvoices_Tutors_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Tutors",
                        principalColumn: "TutorID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionInvoices_JobId",
                table: "CommissionInvoices",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionInvoices_TutorId",
                table: "CommissionInvoices",
                column: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_TuitionOffers_Tutors_HiredTutorId",
                table: "TuitionOffers",
                column: "HiredTutorId",
                principalTable: "Tutors",
                principalColumn: "TutorID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TuitionRequests_Tutors_TutorId",
                table: "TuitionRequests",
                column: "TutorId",
                principalTable: "Tutors",
                principalColumn: "TutorID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TuitionOffers_Tutors_HiredTutorId",
                table: "TuitionOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_TuitionRequests_Tutors_TutorId",
                table: "TuitionRequests");

            migrationBuilder.DropTable(
                name: "CommissionInvoices");

            migrationBuilder.AddForeignKey(
                name: "FK_TuitionOffers_Tutors_HiredTutorId",
                table: "TuitionOffers",
                column: "HiredTutorId",
                principalTable: "Tutors",
                principalColumn: "TutorID");

            migrationBuilder.AddForeignKey(
                name: "FK_TuitionRequests_Tutors_TutorId",
                table: "TuitionRequests",
                column: "TutorId",
                principalTable: "Tutors",
                principalColumn: "TutorID");
        }
    }
}
