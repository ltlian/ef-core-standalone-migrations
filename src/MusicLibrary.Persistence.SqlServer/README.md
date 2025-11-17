# MusicLibrary.Persistence.SqlServer

This project is responsible for managing migrations for the `MusicLibrary.Persistence` project.

## `IDesignTimeDbContextFactory<TContext>` Implementation

Normally, the migrations toolset needs to find our `DbContext` from an `IHostApplicationBuilder` or DI in the application's entry point, alongside the `Microsoft.EntityFrameworkCore.Design` package in the same project. This is the point that these two dependencies are tied together and which we would like to avoid.

To make the migrations project stand-alone and to enable migration operations without depending on a startup project, we need to define a factory usable by the tools by implementing `IDesignTimeDbContextFactory<TContext>`. This allows the toolset to find our `DbContext` along with the required `Microsoft.EntityFrameworkCore.Design` package.

See <https://learn.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli#from-a-design-time-factory>

## Common migration operations

To create a new migration based on the current `MusicDbContext`, the following command should work out of the box if executed from the `MusicLibrary.Persistence.SqlServer.csproj` directory:

```shell

dotnet ef migrations add InitialCreate

```

This will create a `/Migrations` folder if it doesn't already exist and insert a new set of files.

You can now generate the script for applying the migration:

```shell

dotnet ef migrations script

```

Alternatively, to apply the migration automatically:

```shell

dotnet ef database update --connection "$ConnectionString"

```

## Other `dotnet-ef` examples and notes

By default, the current working directory is checked for a `.csproj` file. To specify the file from a different directory, use `--project <path/to/project.csproj>`.

For brevity, the following examples are executed from the `MusicLibrary.Persistence.MsSql.csproj` directory with the minimal set of parameters. For additional parameters, see <https://learn.microsoft.com/en-us/ef/core/cli/dotnet>.

> For automation scenarios, the `--no-build` parameter can be useful, though ensure that the application is actually rebuilt both before creating and applying migrations, as any changes made to the model since the last build may not be picked up by the tools.

### Scripting vs. automatic

There are a few different approaches to take for applying the migration to the database. These are described over at microsoft's docs: <https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli>.

Only the scripting (`ef migrations script`) and automatic (`ef database update`) approaches are shown here.

The scripting approach avoids the need for the application to contact the database, potentially simplifying the migrations project. An automatic approach will also need additional permissions, whether these are granted to the application user or handled by a separate migrations user.

Conversely, when using `ef migrations script`, since no call is made to the database to know its current state, you need to provide the start and end range of migrations. By default, all migrations are included.

Update to the latest migration:

```sh

dotnet ef database update --connection "$ConnectionString"

```

Bring the database to a specific migration:

```sh

dotnet ef database update 20180904195021_InitialCreate --connection "$ConnectionString"

```

Revert all migrations:

```sh

dotnet ef database update 0 --connection "$ConnectionString"

```
