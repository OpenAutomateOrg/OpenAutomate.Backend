using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrganizationToOrganizationUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutomationPackages_Organization_OrganizationId",
                table: "AutomationPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_BotAgents_Organization_OrganizationId",
                table: "BotAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_Executions_Organization_OrganizationId",
                table: "Executions");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Organization_OrganizationId",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "OrganizationUsers");

            migrationBuilder.DropTable(
                name: "Organization");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Schedules",
                newName: "OrganizationUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_OrganizationId",
                table: "Schedules",
                newName: "IX_Schedules_OrganizationUnitId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Executions",
                newName: "OrganizationUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Executions_OrganizationId",
                table: "Executions",
                newName: "IX_Executions_OrganizationUnitId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "BotAgents",
                newName: "OrganizationUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_BotAgents_OrganizationId",
                table: "BotAgents",
                newName: "IX_BotAgents_OrganizationUnitId");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "AutomationPackages",
                newName: "OrganizationUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_AutomationPackages_OrganizationId",
                table: "AutomationPackages",
                newName: "IX_AutomationPackages_OrganizationUnitId");

            migrationBuilder.CreateTable(
                name: "OrganizationUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifyBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationUnitUsers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUnitUsers", x => new { x.UserId, x.OrganizationUnitId });
                    table.ForeignKey(
                        name: "FK_OrganizationUnitUsers_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationUnitUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnitUsers_OrganizationUnitId",
                table: "OrganizationUnitUsers",
                column: "OrganizationUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_AutomationPackages_OrganizationUnits_OrganizationUnitId",
                table: "AutomationPackages",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BotAgents_OrganizationUnits_OrganizationUnitId",
                table: "BotAgents",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Executions_OrganizationUnits_OrganizationUnitId",
                table: "Executions",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_OrganizationUnits_OrganizationUnitId",
                table: "Schedules",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutomationPackages_OrganizationUnits_OrganizationUnitId",
                table: "AutomationPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_BotAgents_OrganizationUnits_OrganizationUnitId",
                table: "BotAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_Executions_OrganizationUnits_OrganizationUnitId",
                table: "Executions");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_OrganizationUnits_OrganizationUnitId",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "OrganizationUnitUsers");

            migrationBuilder.DropTable(
                name: "OrganizationUnits");

            migrationBuilder.RenameColumn(
                name: "OrganizationUnitId",
                table: "Schedules",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_OrganizationUnitId",
                table: "Schedules",
                newName: "IX_Schedules_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "OrganizationUnitId",
                table: "Executions",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Executions_OrganizationUnitId",
                table: "Executions",
                newName: "IX_Executions_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "OrganizationUnitId",
                table: "BotAgents",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_BotAgents_OrganizationUnitId",
                table: "BotAgents",
                newName: "IX_BotAgents_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "OrganizationUnitId",
                table: "AutomationPackages",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_AutomationPackages_OrganizationUnitId",
                table: "AutomationPackages",
                newName: "IX_AutomationPackages_OrganizationId");

            migrationBuilder.CreateTable(
                name: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastModifyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifyBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationUsers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUsers", x => new { x.UserId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_OrganizationUsers_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_OrganizationId",
                table: "OrganizationUsers",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AutomationPackages_Organization_OrganizationId",
                table: "AutomationPackages",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BotAgents_Organization_OrganizationId",
                table: "BotAgents",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Executions_Organization_OrganizationId",
                table: "Executions",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Organization_OrganizationId",
                table: "Schedules",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");
        }
    }
}
