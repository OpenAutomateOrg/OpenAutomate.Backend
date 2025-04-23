using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTableOUInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationUnitInvitation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUnitInvitation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationUnitInvitation_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganizationUnitInvitation_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnitInvitation_OrganizationUnitId",
                table: "OrganizationUnitInvitation",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnitInvitation_UserId",
                table: "OrganizationUnitInvitation",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationUnitInvitation");
        }
    }
}
