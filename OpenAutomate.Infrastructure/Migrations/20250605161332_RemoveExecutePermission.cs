using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenAutomate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExecutePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing permission levels to new model (0-4)
            // Execute (3) stays as Update (3)
            // Update (4) becomes Update (3) 
            // Delete (5) becomes Delete (4)
            
            migrationBuilder.Sql(@"
                UPDATE AuthorityResources 
                SET Permission = 3 
                WHERE Permission = 4;
            ");
            
            migrationBuilder.Sql(@"
                UPDATE AuthorityResources 
                SET Permission = 4 
                WHERE Permission = 5;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the permission level changes
            // Delete (4) becomes Delete (5)
            // Update (3) becomes Update (4)
            // Note: Cannot distinguish between original Execute (3) and Update (4) that became Update (3)
            
            migrationBuilder.Sql(@"
                UPDATE AuthorityResources 
                SET Permission = 5 
                WHERE Permission = 4;
            ");
            
            migrationBuilder.Sql(@"
                UPDATE AuthorityResources 
                SET Permission = 4 
                WHERE Permission = 3;
            ");
        }
    }
}
