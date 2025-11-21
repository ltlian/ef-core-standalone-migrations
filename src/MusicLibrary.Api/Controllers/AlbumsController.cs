using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MusicLibrary.Api.RequestModels;
using MusicLibrary.Persistence;

namespace MusicLibrary.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AlbumsController(IDbContextFactory<MusicDbContext> dbContextFactory) : ControllerBase
{
    [HttpGet(Name = "GetAllAlbums")]
    public async Task<IEnumerable<AlbumResponse>> Get()
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        return await dbContext.Albums
            .Select(a => new AlbumResponse
            {
                Id = a.Id,
                Title = a.Title,
                ArtistId = a.ArtistId
            })
            .ToListAsync();
    }

    [HttpGet("{id}", Name = "GetAlbumById")]
    public async Task<ActionResult<AlbumResponse>> GetById(int id)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        return await dbContext.Albums
            .Where(a => a.Id == id)
            .Select(a => new AlbumResponse
            {
                Id = a.Id,
                Title = a.Title,
                ArtistId = a.ArtistId
            })
            .FirstOrDefaultAsync()
            is AlbumResponse album
                ? album
                : NotFound();
    }

    [HttpGet("{albumId}/artist", Name = "GetAlbumArtist")]
    public async Task<ActionResult<ArtistResponse>> GetAlbumArtist(int albumId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        return (await dbContext.Albums
            .Where(a => a.Id == albumId)
            .Select(a => new ArtistResponse
            {
                Id = a.Artist.Id,
                Name = a.Artist.Name
            })
            .FirstOrDefaultAsync())
            is ArtistResponse artist
                ? artist
                : NotFound();
    }
}
