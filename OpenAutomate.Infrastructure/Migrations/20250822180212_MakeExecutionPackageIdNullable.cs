using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeExecutionPackageIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Executions_AutomationPackages_PackageId",
                table: "Executions");

            migrationBuilder.AlterColumn<Guid>(
                name: "PackageId",
                table: "Executions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Executions_AutomationPackages_PackageId",
                table: "Executions",
                column: "PackageId",
                principalTable: "AutomationPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Executions_AutomationPackages_PackageId",
                table: "Executions");

            migrationBuilder.AlterColumn<Guid>(
                name: "PackageId",
                table: "Executions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Executions_AutomationPackages_PackageId",
                table: "Executions",
                column: "PackageId",
                principalTable: "AutomationPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
