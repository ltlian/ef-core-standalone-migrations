# Separating Migrations from the Application

An example project structure inspired by, and based on, <https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects>.

## Overview and motivation

In this structure:

- The application (a web API) is not aware of migrations.
- Migrations can be created and applied without needing to point to a startup project.

Not addressed in this structure:

- The application still references `DbContext` and its `DbSet`s. For further separation to prevent the application from being able to directly interact with these database types, separate projects would be made to hide these behind repositories and DTOs.
- The domain models (`Artist.cs` and `Album.cs`) are tailored to MSSQL to some extent, mainly by their `int` primary keys. Creating another layer of separation by further abstracting these models out into a completely agnostic contract which `MusicLibrary.Persistence.csproj` imports is possible, but is not useful until you need it.

This application is targeted towards an Sql Server database. For other providers (eg., PostgreSQL), an equivalent `MusicLibrary.Persistence.PostgreSQL` would be created in order to implement `IDesignTimeDbContextFactory<TContext>` with the `Npgsql.EntityFrameworkCore.PostgreSQL` provider.

## Getting started

> _In case you don't have an MSSQL development database available but access to docker, I've written some notes on launching an MSSQL container over in [this gist](https://gist.github.com/ltlian/caa1b92330b31809dc6d67a625b8d7cf)._

### Create database

This is the only step needed to apply the migrations using the scripting approach.

The database name has no requirements aside from being what we provide to the connection string later.

```sql

CREATE DATABASE Music_Main;
GO

```

### Create user (optional)

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

### Add migrations

Navigate to `/src/MusicLibrary.Persistence.SqlServer` and enter the following command to create a new migration.

```shell

dotnet ef migrations add InitialCreate

```

### Apply migrations

We can then generate the migrations script.

Since we just built the project in the previous step, we can skip this here.

```shell

dotnet ef migrations script --no-build

```

This script can be run in the newly created `Music_Main` database. The database should now be in a state usable by the API.

> _By default, the database state is not validated on startup. If the database and application are not in the same migration state, errors will not manifest until an invalid query is made._

For more notes on applying migrations using this approach, check out [/src/MusicLibrary.Persistence.SqlServer/README.md](/src/MusicLibrary.Persistence.SqlServer/README.md)

### Running the API

#### Set database connection string

The API expects a connection string named `DefaultConnection` which can be configured via user secrets or otherwise. As per the usual disclaimer, never store secrets in code.

Via CLI, from the `/src/MusicLibrary.Api` directory:

```shell

dotnet user-secrets init

dotnet user-secrets set "ConnectionStrings:DefaultConnectxion" "Server=<HOST-OR-IP>;Database=Music_Main;UID=music-api;PWD=<a_very_strong_password>;Trust Server Certificate=True;"

```

Or by right-clicking the `MusicLibrary.Api` Visual Studio and selecting `Manage User Secrets`:

```json

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<HOST-OR-IP>;Database=Music_Main;UID=music-api;PWD=<a_very_strong_password>;Trust Server Certificate=True;"
  }
}

```

> _For notes on formatting connection strings for EF Core, check out [this gist](https://gist.github.com/ltlian/247329a81206a0a39007806f78d62422)._

#### Verify

Start the `MusicLibrary.Api` application. It should now be possible to insert and read data from the newly migrated database using the HTTP endpoints.

Create new artist:

```shell

curl -iX POST "https://localhost:7042/artists" -H 'Content-Type: application/json' -d '{"name":"New Artist"}'

```

Getting all artists should return the one we just created:

```shell

curl "https://localhost:7042/artists"

```

> _`.http` files are not included in this example as it appears these rely on the `dotnet-interactive` tool which has not yet been updated to NET10[^net10-interactive-tool-issue]._

## Project Structure

### Class library: MusicLibrary.Persistence.csproj

Only defines the DbContext and its models. This is a database-agnostic implementation and should work for any provider.

**Package dependencies**:

- `Microsoft.EntityFrameworkCore`
  - Provides  `Microsoft.EntityFrameworkCore.DbContext`

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

#### Providing a migration-specific `DbContextFactory`

Normally, the migrations toolset needs to find your `DbContext` from a `IHostApplicationBuilder` or DI in the application's entry point, alongside the `Microsoft.EntityFrameworkCore.Design` package in the same project. This is the point that these two dependencies are tied together and which we would like to avoid.

To make the migrations project stand-alone and to enable migration operations without depending on a startup project, we need to define a factory specific to the migrations project by implementing `IDesignTimeDbContextFactory<TContext>`. This is what the migrations toolset will use to find our `DbContext` along with the required `Microsoft.EntityFrameworkCore.Design` package.

Implementing `IDesignTimeDbContextFactory<TContext>` is straightforward. Here's where we define how the `DbContext` connects to our database, which is in line with how a particular database provider defines its migrations separately. We can define configuration specific to this `DbContext`, and pass parameters to `CreateDbContext` from the command line.

See <https://learn.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli#from-a-design-time-factory>

For a basic implementation where we are only interested in implementing `CreateDbContext` for a particular database provider, it should looks something like this:

```cs

using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MusicLibrary.Persistence.MsSql;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseSqlServer(b => b.MigrationsAssembly(Assembly.GetExecutingAssembly()));

        return new AppDbContext(optionsBuilder.Options);
    }
}

```

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
