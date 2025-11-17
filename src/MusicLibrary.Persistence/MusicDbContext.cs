using Microsoft.EntityFrameworkCore;

using MusicLibrary.Persistence.Models;

namespace MusicLibrary.Persistence;

public class MusicDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Album> Albums { get; set; }
}
