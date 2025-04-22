using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantQueryFilters : Migration
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

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserAuthorities");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UserAuthorities");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserAuthorities");

            migrationBuilder.DropColumn(
                name: "LastModifyAt",
                table: "UserAuthorities");

            migrationBuilder.DropColumn(
                name: "LastModifyBy",
                table: "UserAuthorities");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AuthorityResources");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AuthorityResources");

            migrationBuilder.DropColumn(
                name: "LastModifyAt",
                table: "AuthorityResources");

            migrationBuilder.DropColumn(
                name: "LastModifyBy",
                table: "AuthorityResources");

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

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AutomationPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Authorities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AutomationPackages");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Authorities");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserAuthorities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "UserAuthorities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "UserAuthorities",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifyAt",
                table: "UserAuthorities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifyBy",
                table: "UserAuthorities",
                type: "uniqueidentifier",
                nullable: true);

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

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AuthorityResources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "AuthorityResources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifyAt",
                table: "AuthorityResources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifyBy",
                table: "AuthorityResources",
                type: "uniqueidentifier",
                nullable: true);

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
