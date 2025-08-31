using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleIdToExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleId",
                table: "Executions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Executions_ScheduleId",
                table: "Executions",
                column: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Executions_Schedules_ScheduleId",
                table: "Executions",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Executions_Schedules_ScheduleId",
                table: "Executions");

            migrationBuilder.DropIndex(
                name: "IX_Executions_ScheduleId",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "Executions");
        }
    }
}
