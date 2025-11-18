using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace MusicLibrary.Persistence.SqlServer;

public static class MigrationBuilderExtensions
{
    private const string MusicOwnerRole = "music_data_owner";

    extension(MigrationBuilder migrationBuilder)
    {
        public OperationBuilder<SqlOperation> CreateMusicDataOwnerRole()
        {
            migrationBuilder.EnsureSqlServerProvider();
            return migrationBuilder.Sql($"""
                IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [type] = 'R' AND name = N'{MusicOwnerRole}')
                BEGIN
                    CREATE ROLE [{MusicOwnerRole}];
                END

                GRANT SELECT, INSERT, UPDATE, DELETE
                    ON SCHEMA :: [dbo]
                    TO [{MusicOwnerRole}];
                """);
        }

        public OperationBuilder<SqlOperation> DropMusicDataOwnerRole()
        {
            migrationBuilder.EnsureSqlServerProvider();
            return migrationBuilder.Sql($"DROP ROLE IF EXISTS [{MusicOwnerRole}];");
        }

        public OperationBuilder<SqlOperation> CreateUser(string login, string password)
        {
            migrationBuilder.EnsureSqlServerProvider();
            return migrationBuilder.Sql($"""
                IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [type] = 'S' AND name = N'{login}')
                BEGIN
                    CREATE LOGIN [{login}] WITH PASSWORD = N'{password}';
                    CREATE USER [{login}] FOR LOGIN [{login}];
                END

                ALTER ROLE [{MusicOwnerRole}] ADD MEMBER [{login}];
                """);
        }

        public OperationBuilder<SqlOperation> DropUser(string login)
        {
            migrationBuilder.EnsureSqlServerProvider();
            return migrationBuilder.Sql($"""
                IF EXISTS (SELECT 1 FROM sys.database_principals WHERE [type] = 'S' AND name = N'{login}')
                BEGIN
                    ALTER ROLE [{MusicOwnerRole}] DROP MEMBER [{login}];
                    DROP USER [{login}];
                    DROP LOGIN [{login}];
                END
                """);
        }

        private void EnsureSqlServerProvider()
        {
            if (!migrationBuilder.IsSqlServer())
            {
                throw new Exception("Unexpected provider.");
            }
        }
    }
}
