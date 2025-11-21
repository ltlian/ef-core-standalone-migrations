using System.Diagnostics.CodeAnalysis;

namespace MusicLibrary.Persistence.Models;

public class Album
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required int ArtistId { get; set; }

    /* "All domain models are wrong"
     *
     * In our imagined world domain, an album must have one artist. In a technical context (also a domain), we need to
     * be able to create instances of Album without having to provide an instance of Artist.
     *
     * Setting the Artist property to nullable along with the [NotNull] attribute allows us to express this constraint.
     */

    [NotNull]
    public Artist? Artist { get; set; }
}