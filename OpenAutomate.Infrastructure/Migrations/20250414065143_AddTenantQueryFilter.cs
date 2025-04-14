using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantQueryFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutomationPackages_OrganizationUnits_OrganizationUnitId",
                table: "AutomationPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_BotAgents_OrganizationUnits_OrganizationUnitId",
                table: "BotAgents");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationUnitId",
                table: "BotAgents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationUnitId",
                table: "AutomationPackages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AutomationPackages_OrganizationUnits_OrganizationUnitId",
                table: "AutomationPackages",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BotAgents_OrganizationUnits_OrganizationUnitId",
                table: "BotAgents",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationUnitId",
                table: "BotAgents",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationUnitId",
                table: "AutomationPackages",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

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
        }
    }
}
