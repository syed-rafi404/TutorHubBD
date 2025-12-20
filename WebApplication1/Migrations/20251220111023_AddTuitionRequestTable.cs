using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorHubBD.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTuitionRequestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TuitionRequests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudentEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TuitionOfferId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TuitionRequests", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_TuitionRequests_TuitionOffers_TuitionOfferId",
                        column: x => x.TuitionOfferId,
                        principalTable: "TuitionOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TuitionRequests_TuitionOfferId",
                table: "TuitionRequests",
                column: "TuitionOfferId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TuitionRequests");
        }
    }
}
