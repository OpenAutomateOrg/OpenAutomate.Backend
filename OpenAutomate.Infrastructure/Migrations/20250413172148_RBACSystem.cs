using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RBACSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorities_Authorities_AuthorityID",
                table: "UserAuthorities");

            migrationBuilder.DropIndex(
                name: "IX_PackageVersions_PackageId",
                table: "PackageVersions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AutomationPackages");

            migrationBuilder.RenameColumn(
                name: "AuthorityID",
                table: "UserAuthorities",
                newName: "AuthorityId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuthorities_AuthorityID",
                table: "UserAuthorities",
                newName: "IX_UserAuthorities_AuthorityId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserAuthorities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UserAuthorities",
                type: "nvarchar(max)",
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

            migrationBuilder.AddColumn<string>(
                name: "LastModifyBy",
                table: "UserAuthorities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationUnitId",
                table: "UserAuthorities",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "CronExpression",
                table: "Schedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "VersionNumber",
                table: "PackageVersions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "OrganizationUnits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OrganizationUnits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Executions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BotAgents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "BotAgents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AutomationPackages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Authorities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AuthorityResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Permission = table.Column<int>(type: "int", nullable: false),
                    AuthorityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifyBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorityResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorityResources_Authorities_AuthorityId",
                        column: x => x.AuthorityId,
                        principalTable: "Authorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorityResources_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Authorities",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "LastModifyAt", "LastModifyBy", "Name" },
                values: new object[,]
                {
                    { new Guid("374379f0-c9c8-4c1d-8de2-84486736ba95"), new DateTime(2025, 4, 13, 17, 21, 47, 87, DateTimeKind.Utc).AddTicks(7814), null, null, null, "DEVELOPER" },
                    { new Guid("60577220-01c4-4e4e-9ddf-9da1dc8e70f8"), new DateTime(2025, 4, 13, 17, 21, 47, 87, DateTimeKind.Utc).AddTicks(7780), null, null, null, "ADMIN" },
                    { new Guid("c55809b6-1c75-4a41-be9e-94e0fea45797"), new DateTime(2025, 4, 13, 17, 21, 47, 87, DateTimeKind.Utc).AddTicks(7811), null, null, null, "OPERATOR" },
                    { new Guid("d70e21d9-ba7e-4238-8c2c-601000afe166"), new DateTime(2025, 4, 13, 17, 21, 47, 87, DateTimeKind.Utc).AddTicks(7808), null, null, null, "USER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthorities_OrganizationUnitId",
                table: "UserAuthorities",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthorities_UserId_AuthorityId",
                table: "UserAuthorities",
                columns: new[] { "UserId", "AuthorityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_IsActive",
                table: "Schedules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_PackageVersions_PackageId_VersionNumber",
                table: "PackageVersions",
                columns: new[] { "PackageId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_Slug",
                table: "OrganizationUnits",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Executions_StartTime",
                table: "Executions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_Status",
                table: "Executions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BotAgents_Name",
                table: "BotAgents",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BotAgents_Status",
                table: "BotAgents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationPackages_Name",
                table: "AutomationPackages",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorityResources_AuthorityId_ResourceName",
                table: "AuthorityResources",
                columns: new[] { "AuthorityId", "ResourceName" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorityResources_OrganizationUnitId",
                table: "AuthorityResources",
                column: "OrganizationUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorities_Authorities_AuthorityId",
                table: "UserAuthorities",
                column: "AuthorityId",
                principalTable: "Authorities",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorities_Authorities_AuthorityId",
                table: "UserAuthorities");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorities_OrganizationUnits_OrganizationUnitId",
                table: "UserAuthorities");

            migrationBuilder.DropTable(
                name: "AuthorityResources");

            migrationBuilder.DropIndex(
                name: "IX_UserAuthorities_OrganizationUnitId",
                table: "UserAuthorities");

            migrationBuilder.DropIndex(
                name: "IX_UserAuthorities_UserId_AuthorityId",
                table: "UserAuthorities");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_IsActive",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_PackageVersions_PackageId_VersionNumber",
                table: "PackageVersions");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUnits_Slug",
                table: "OrganizationUnits");

            migrationBuilder.DropIndex(
                name: "IX_Executions_StartTime",
                table: "Executions");

            migrationBuilder.DropIndex(
                name: "IX_Executions_Status",
                table: "Executions");

            migrationBuilder.DropIndex(
                name: "IX_BotAgents_Name",
                table: "BotAgents");

            migrationBuilder.DropIndex(
                name: "IX_BotAgents_Status",
                table: "BotAgents");

            migrationBuilder.DropIndex(
                name: "IX_AutomationPackages_Name",
                table: "AutomationPackages");

            migrationBuilder.DeleteData(
                table: "Authorities",
                keyColumn: "Id",
                keyValue: new Guid("374379f0-c9c8-4c1d-8de2-84486736ba95"));

            migrationBuilder.DeleteData(
                table: "Authorities",
                keyColumn: "Id",
                keyValue: new Guid("60577220-01c4-4e4e-9ddf-9da1dc8e70f8"));

            migrationBuilder.DeleteData(
                table: "Authorities",
                keyColumn: "Id",
                keyValue: new Guid("c55809b6-1c75-4a41-be9e-94e0fea45797"));

            migrationBuilder.DeleteData(
                table: "Authorities",
                keyColumn: "Id",
                keyValue: new Guid("d70e21d9-ba7e-4238-8c2c-601000afe166"));

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
                name: "OrganizationUnitId",
                table: "UserAuthorities");

            migrationBuilder.RenameColumn(
                name: "AuthorityId",
                table: "UserAuthorities",
                newName: "AuthorityID");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuthorities_AuthorityId",
                table: "UserAuthorities",
                newName: "IX_UserAuthorities_AuthorityID");

            migrationBuilder.AlterColumn<string>(
                name: "CronExpression",
                table: "Schedules",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "VersionNumber",
                table: "PackageVersions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "OrganizationUnits",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OrganizationUnits",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Executions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BotAgents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "BotAgents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AutomationPackages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AutomationPackages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Authorities",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_PackageVersions_PackageId",
                table: "PackageVersions",
                column: "PackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorities_Authorities_AuthorityID",
                table: "UserAuthorities",
                column: "AuthorityID",
                principalTable: "Authorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
