using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fixdynamicseeddata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.InsertData(
                table: "Authorities",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "LastModifyAt", "LastModifyBy", "Name" },
                values: new object[,]
                {
                    { new Guid("1a89f6f4-3c29-4fe1-9483-5de6676cc3f7"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "ADMIN" },
                    { new Guid("7e4ea7df-5f1c-4234-8c7a-83d0c9ca2018"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "USER" },
                    { new Guid("cfe55508-5a24-4f84-b436-36b1b4395436"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "DEVELOPER" },
                    { new Guid("e87a7aee-848a-46d4-b9f5-1e28c2571b3a"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "OPERATOR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Authorities",
                keyColumn: "Id",
                keyValue: new Guid("1a89f6f4-3c29-4fe1-9483-5de6676cc3f7"));

            migrationBuilder.DeleteData(
                table: "Authorities",
                keyColumn: "Id",
                keyValue: new Guid("7e4ea7df-5f1c-4234-8c7a-83d0c9ca2018"));

            migrationBuilder.DeleteData(
                table: "Authorities",
                keyColumn: "Id",
                keyValue: new Guid("cfe55508-5a24-4f84-b436-36b1b4395436"));

            migrationBuilder.DeleteData(
                table: "Authorities",
                keyColumn: "Id",
                keyValue: new Guid("e87a7aee-848a-46d4-b9f5-1e28c2571b3a"));

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
        }
    }
}
