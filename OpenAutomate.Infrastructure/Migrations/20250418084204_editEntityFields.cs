using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class editEntityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Authorities_OrganizationUnits_OrganizationUnitId",
                table: "Authorities");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorityResources_OrganizationUnits_OrganizationUnitId",
                table: "AuthorityResources");

            migrationBuilder.DropForeignKey(
                name: "FK_AutomationPackages_OrganizationUnits_OrganizationUnitId",
                table: "AutomationPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_BotAgents_OrganizationUnits_OrganizationUnitId",
                table: "BotAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationUnitUsers_OrganizationUnits_OrganizationUnitId",
                table: "OrganizationUnitUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorities_OrganizationUnits_OrganizationUnitId",
                table: "UserAuthorities");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationUnitId",
                table: "Schedules",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationUnitId",
                table: "PackageVersions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "OrganizationUnitUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "OrganizationUnitUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "OrganizationUnitUsers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifyAt",
                table: "OrganizationUnitUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifyBy",
                table: "OrganizationUnitUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationUnitId",
                table: "Executions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_PackageVersions_OrganizationUnitId",
                table: "PackageVersions",
                column: "OrganizationUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Authorities_OrganizationUnits_OrganizationUnitId",
                table: "Authorities",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorityResources_OrganizationUnits_OrganizationUnitId",
                table: "AuthorityResources",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id");

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
                name: "FK_OrganizationUnitUsers_OrganizationUnits_OrganizationUnitId",
                table: "OrganizationUnitUsers",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PackageVersions_OrganizationUnits_OrganizationUnitId",
                table: "PackageVersions",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorities_OrganizationUnits_OrganizationUnitId",
                table: "UserAuthorities",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Authorities_OrganizationUnits_OrganizationUnitId",
                table: "Authorities");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorityResources_OrganizationUnits_OrganizationUnitId",
                table: "AuthorityResources");

            migrationBuilder.DropForeignKey(
                name: "FK_AutomationPackages_OrganizationUnits_OrganizationUnitId",
                table: "AutomationPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_BotAgents_OrganizationUnits_OrganizationUnitId",
                table: "BotAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationUnitUsers_OrganizationUnits_OrganizationUnitId",
                table: "OrganizationUnitUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageVersions_OrganizationUnits_OrganizationUnitId",
                table: "PackageVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorities_OrganizationUnits_OrganizationUnitId",
                table: "UserAuthorities");

            migrationBuilder.DropIndex(
                name: "IX_PackageVersions_OrganizationUnitId",
                table: "PackageVersions");

            migrationBuilder.DropColumn(
                name: "OrganizationUnitId",
                table: "PackageVersions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "OrganizationUnitUsers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "OrganizationUnitUsers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "OrganizationUnitUsers");

            migrationBuilder.DropColumn(
                name: "LastModifyAt",
                table: "OrganizationUnitUsers");

            migrationBuilder.DropColumn(
                name: "LastModifyBy",
                table: "OrganizationUnitUsers");

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
                table: "Schedules",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationUnitId",
                table: "Executions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Authorities_OrganizationUnits_OrganizationUnitId",
                table: "Authorities",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorityResources_OrganizationUnits_OrganizationUnitId",
                table: "AuthorityResources",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationUnitUsers_OrganizationUnits_OrganizationUnitId",
                table: "OrganizationUnitUsers",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorities_OrganizationUnits_OrganizationUnitId",
                table: "UserAuthorities",
                column: "OrganizationUnitId",
                principalTable: "OrganizationUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
