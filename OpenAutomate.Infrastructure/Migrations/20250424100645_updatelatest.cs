using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatelatest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration is intentionally empty as it serves as a consolidated snapshot
            // of the current database schema after cleaning up previous migration history.
            // The Designer.cs file contains the complete schema representation.
            // No database changes are needed at this point as all required schema objects 
            // already exist in the database.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration is a consolidated snapshot migration and cannot be reverted
            // through this method as it would require undoing all previous migrations.
            // If a rollback is needed, use a database backup or create a new migration.
            throw new NotSupportedException(
                "This consolidated migration cannot be reverted. Restore from a backup instead.");
        }
    }
}
