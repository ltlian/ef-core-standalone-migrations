using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicLibrary.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUserRole() : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateMusicDataOwnerRole();

            // During development, the simplest approach is to generate a script using `dotnet ef migrations script`
            // and replacing the password manually.
            // For automated deployments or production scenarios, see https://learn.microsoft.com/en-us/sql/relational-databases/security/sql-server-security-best-practices?view=sql-server-ver17#identities-and-authentication
            migrationBuilder.CreateUser("music-api", "{API_USER_PASSWORD}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUser("music-api");
            migrationBuilder.DropMusicDataOwnerRole();
        }
    }
}
