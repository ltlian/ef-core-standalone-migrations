namespace MusicLibrary.Persistence.Models;

public class Album
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public int ArtistId { get; set; }
    public required Artist Artist { get; set; }
}