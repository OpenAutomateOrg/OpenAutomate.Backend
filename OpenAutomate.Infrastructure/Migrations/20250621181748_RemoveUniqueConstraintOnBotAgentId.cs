using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintOnBotAgentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_BotAgentId_Unique",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_BotAgentId",
                table: "Schedules",
                column: "BotAgentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_BotAgentId",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_BotAgentId_Unique",
                table: "Schedules",
                column: "BotAgentId",
                unique: true);
        }
    }
}
