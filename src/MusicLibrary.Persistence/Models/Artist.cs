namespace MusicLibrary.Persistence.Models;

public class Artist
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Album> Albums { get; set; } = [];
}
