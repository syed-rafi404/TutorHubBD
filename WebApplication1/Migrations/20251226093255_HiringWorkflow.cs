using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorHubBD.Web.Migrations
{
    /// <inheritdoc />
    public partial class HiringWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TutorId",
                table: "TuitionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HiredTutorId",
                table: "TuitionOffers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TuitionOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TuitionRequests_TutorId",
                table: "TuitionRequests",
                column: "TutorId");

            migrationBuilder.CreateIndex(
                name: "IX_TuitionOffers_HiredTutorId",
                table: "TuitionOffers",
                column: "HiredTutorId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TuitionOffers_Tutors_HiredTutorId",
                table: "TuitionOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_TuitionRequests_Tutors_TutorId",
                table: "TuitionRequests");

            migrationBuilder.DropIndex(
                name: "IX_TuitionRequests_TutorId",
                table: "TuitionRequests");

            migrationBuilder.DropIndex(
                name: "IX_TuitionOffers_HiredTutorId",
                table: "TuitionOffers");

            migrationBuilder.DropColumn(
                name: "TutorId",
                table: "TuitionRequests");

            migrationBuilder.DropColumn(
                name: "HiredTutorId",
                table: "TuitionOffers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TuitionOffers");
        }
    }
}
