using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleToOrganizationUnitUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "OrganizationUnitUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "OrganizationUnitUsers");
        }
    }
}
