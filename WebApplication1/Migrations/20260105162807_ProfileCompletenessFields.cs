using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorHubBD.Web.Migrations
{
    /// <inheritdoc />
    public partial class ProfileCompletenessFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProfileComplete",
                table: "Tutors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreferredClasses",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Tutors",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "IsProfileComplete",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "PreferredClasses",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Tutors");
        }
    }
}
