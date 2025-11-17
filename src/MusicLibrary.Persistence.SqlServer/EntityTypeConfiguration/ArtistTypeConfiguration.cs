using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MusicLibrary.Persistence.Models;

namespace MusicLibrary.Persistence.SqlServer.EntityTypeConfiguration;

public class ArtistTypeConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.ToTable("Artists");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired();
    }
}
