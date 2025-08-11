using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptUrlAndCustomerPortalUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerPortalUrl",
                table: "Subscriptions",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptUrl",
                table: "Payments",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerPortalUrl",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ReceiptUrl",
                table: "Payments");
        }
    }
}
