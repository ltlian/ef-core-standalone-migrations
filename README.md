# Separating Migrations from the Application

An example project structure inspired by, and based on, <https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects>.

## Overview and motivation

In this structure:

- The application (a web API) is not aware of migrations.
- Migrations can be created and applied without needing to point to a startup project.

Not addressed in this structure:

- The application still references `DbContext` and its `DbSet`s. For further separation to prevent the application from being able to directly interact with these database types, separate projects would be made to hide these behind repositories and DTOs.
- The domain models [Artist.cs](./src/MusicLibrary.Persistence/Models/Artist.cs) and [Album.cs](./src/MusicLibrary.Persistence/Models/Album.cs) are tailored to MSSQL to some extent, mainly by their `int` primary keys. Creating another layer of separation by further abstracting these models out into a completely agnostic contract which `MusicLibrary.Persistence.csproj` imports is possible, but is not useful until you need it. The comment found in the [Album.cs](./src/MusicLibrary.Persistence/Models/Album.cs) file shows one way of handling an inherent conflict between the technical model schema and the real-world domain that it represents.

This application is targeted towards an Sql Server database. For other providers (eg., PostgreSQL), an equivalent `MusicLibrary.Persistence.PostgreSQL` would be created in order to implement `IDesignTimeDbContextFactory<TContext>` with the `Npgsql.EntityFrameworkCore.PostgreSQL` provider.

## Getting started

> _In case you don't have an MSSQL development database available but access to docker, I've written some notes on launching an MSSQL container over in [this gist](https://gist.github.com/ltlian/caa1b92330b31809dc6d67a625b8d7cf)._

### Create database

An empty, default database is all we need if we only intend to generate and apply the migrations using the scripting approach.

The database name has no requirements aside from being what we provide to the connection string later.

```sql

CREATE DATABASE Music_Main;
GO

```

### Create API user (optional)

This step is only necessary if we intend to run the API.

```sql

BEGIN TRANSACTION

USE [Music_Main];

CREATE ROLE [music_data_owner];

GRANT SELECT, INSERT, UPDATE, DELETE
  ON SCHEMA :: [dbo]
  TO [music_data_owner];

CREATE LOGIN [music-api]
    WITH PASSWORD = N'a_very_strong_password';

CREATE USER [music-app] FOR LOGIN [music-api];

ALTER ROLE [music_data_owner] ADD MEMBER [music-app];

COMMIT TRANSACTION

```

> _This user will not have the necessary permissions to perform migrations which is usually what we want. If we intend to run automatic migrations, we either need to give this user the `db_owner` role, or create a separate migrations user._

It's technically possible to make user creation part of the migrations, like in the [add-user-migration](https://github.com/ltlian/ef-core-standalone-migrations/tree/add-user-migration) branch, but this introduces a number of challenges for securely providing the initial password.

### Add migrations

Navigate to `/src/MusicLibrary.Persistence.SqlServer` and build the project:

```shell

dotnet build

```

> _Running `ef migrations add` for the first time tends to fail if the project has never been built due to the design-time dependencies being needed for the tool to run, so we do it manually here._

Then we can create our migration:

```shell

dotnet ef migrations add InitialCreate --no-build

```

### Apply migrations

We can then generate the migrations script.

> _It might be tempting to skip the build again here, but doing so will ignore the migrations that were just added in the previous step._

```shell

dotnet ef migrations script

```

If everything goes well, we should see a script similar to this:

```sql

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Artists] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Artists] PRIMARY KEY ([Id])
);

CREATE TABLE [Albums] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [ArtistId] int NOT NULL,
    CONSTRAINT [PK_Albums] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Albums_Artists_ArtistId] FOREIGN KEY ([ArtistId]) REFERENCES [Artists] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Albums_ArtistId] ON [Albums] ([ArtistId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251118002529_InitialCreate', N'10.0.0');

COMMIT;
GO

```

This script can be run in the newly created `Music_Main` database. The database should now be in a state usable by the API.

> _By default, the database state is not validated on startup. If the database and application are not in the same migration state, errors will not manifest until an invalid query is made. Since our runtime application no longer has a reference to the migrations assembly, it is also not able to compare its state during startup._

For more notes on applying migrations using this approach, check out [/src/MusicLibrary.Persistence.SqlServer/README.md](/src/MusicLibrary.Persistence.SqlServer/README.md)

### Running the API

#### Set database connection string

The API expects a connection string named `DefaultConnection` which can be configured via user secrets or otherwise. As per the usual disclaimer, never store secrets in code.

Via CLI, from the `/src/MusicLibrary.Api` directory:

```shell

dotnet user-secrets init

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=<HOST-OR-IP>;Database=Music_Main;UID=music-api;PWD=<a_very_strong_password>;Trust Server Certificate=True;"

```

Or by right-clicking the `MusicLibrary.Api` project in Visual Studio and selecting `Manage User Secrets`:

```json

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<HOST-OR-IP>;Database=Music_Main;UID=music-api;PWD=<a_very_strong_password>;Trust Server Certificate=True;"
  }
}

```

> _For notes on formatting connection strings for EF Core, check out [this gist](https://gist.github.com/ltlian/247329a81206a0a39007806f78d62422)._

#### Verify

Start the `MusicLibrary.Api` application, either from Visual Studio or via CLI from the `\src\MusicLibrary.Api` directory:

```shell

dotnet run

```

It should now be possible to insert and read data from the newly migrated database using the HTTP endpoints.

Note the assumed base path of `http://localhost:5195` for the following examples. Adjust as needed.

Create new artist:

```shell

curl -iX POST "http://localhost:5195/artists" -H 'Content-Type: application/json' -d '{"name":"New Artist"}'

```

Get all artists and confirm the one we just created:

```shell

curl "http://localhost:5195/artists"

```

Add albums to our artist:

```shell

curl -iX POST "http://localhost:5195/artists/1/albums" -H 'Content-Type: application/json' -d '{"title":"The First Album"}'

curl -iX POST "http://localhost:5195/artists/1/albums" -H 'Content-Type: application/json' -d '{"title":"The Second Album"}'

```

Get our newly created artist's albums:

```shell

curl "http://localhost:5195/artists/1/albums"

```

> _`.http` files are not included in this example as it appears these rely on the `dotnet-interactive` tool which has not yet been updated to NET10[^net10-interactive-tool-issue]._

## Project Structure

### Class library: MusicLibrary.Persistence.csproj

Defines the DbContext, its models, and how they map to the database. This is a database-agnostic implementation and should work for any provider.

A key dependency in this project is the call to `modelBuilder.ApplyConfigurationsFromAssembly(typeof(MusicDbContext).Assembly);` from [/src/MusicLibrary.Persistence/MusicDbContext.cs](/src/MusicLibrary.Persistence/MusicDbContext.cs).

For other means of registering entity type configurations, see Microsoft's docs: <https://learn.microsoft.com/en-us/ef/core/modeling/>

**Package dependencies**:

- `Microsoft.EntityFrameworkCore`
  - Provides  `Microsoft.EntityFrameworkCore.DbContext`
- `Microsoft.EntityFrameworkCore.Relational`
  - Provides additional entity type configuration methods, such as `ToTable`. Only needed if the project does not reference `Microsoft.EntityFrameworkCore.Design`.

### Class library: MusicLibrary.Persistence.MsSql.csproj

Manages migrations for changes to the domain model.

Needs the domain model, how to connect to the database, how the domain model maps to it, and the design tools package.

This is inherently specific to a given database provider, hence the project name.

Without this project, the `MusicLibrary.Persistence` project would need to manage migrations specific to MsSql, further requiring the below package dependencies where `Microsoft.EntityFrameworkCore.Design` is only needed for handling migrations.

**Project dependencies**:

- `MusicLibrary.Persistence`
  - Provides the application's `DbContext` implementation and entity type mapping.

**Package dependencies**:

- `Microsoft.EntityFrameworkCore.Design`
  - Manages migrations
- `Microsoft.EntityFrameworkCore.SqlServer`
  - Provides `SqlServerDbContextOptionsExtensions.UseSqlServer`

#### `Microsoft.EntityFrameworkCore.Design` quirks

- If the `Microsoft.EntityFrameworkCore.Design` package is not referenced by the code, Visual Studio may suggest to remove it.
- There is a long-standing issue causing unneccessary files to be copied to the output. See <https://github.com/dotnet/sdk/issues/952>.

### Runtime application: MusicLibrary.Api.csproj

Only needs the domain model and how to connect to the database. Is not aware of migrations.

**Project dependencies**:

- `MusicLibrary.Persistence`
  - Provides the application's `DbContext` implementation.

**Package dependencies**:

- `Microsoft.EntityFrameworkCore.SqlServer`
  - Provides `SqlServerDbContextOptionsExtensions.UseSqlServer`

[^net10-interactive-tool-issue]: .NET10 interactive tool issue

    <https://www.nuget.org/packages/Microsoft.dotnet-interactive/>

    <https://github.com/dotnet/interactive/issues/3233>

    <https://github.com/dotnet/interactive/issues/4086>

    <https://github.com/dotnet/interactive/pull/4093>
