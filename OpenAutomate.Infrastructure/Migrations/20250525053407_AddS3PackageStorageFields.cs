using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddS3PackageStorageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "PackageVersions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "PackageVersions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "PackageVersions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "PackageVersions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "PackageVersions");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "PackageVersions");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "PackageVersions");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "PackageVersions");
        }
    }
}
