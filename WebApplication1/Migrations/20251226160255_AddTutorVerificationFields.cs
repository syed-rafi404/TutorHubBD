using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorHubBD.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationDocumentPath",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationRequestDate",
                table: "Tutors",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationDocumentPath",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "VerificationRequestDate",
                table: "Tutors");
        }
    }
}
