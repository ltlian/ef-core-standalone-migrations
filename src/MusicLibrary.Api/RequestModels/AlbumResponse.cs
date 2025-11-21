namespace MusicLibrary.Api.RequestModels;

public class  AlbumResponse
{
    public required string Title { get; set; }
    public required int Id { get; set; }
    public required int ArtistId { get; set; }
}