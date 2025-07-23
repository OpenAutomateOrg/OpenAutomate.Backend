using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOUDeletetion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "OrganizationUnits",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DeletionJobId",
                table: "OrganizationUnits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDeletionAt",
                table: "OrganizationUnits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_ScheduledDeletionAt",
                table: "OrganizationUnits",
                column: "ScheduledDeletionAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationUnits_ScheduledDeletionAt",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "DeletionJobId",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "ScheduledDeletionAt",
                table: "OrganizationUnits");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "OrganizationUnits",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }
    }
}
