using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MusicLibrary.Api.RequestModels;
using MusicLibrary.Persistence;
using MusicLibrary.Persistence.Models;

namespace MusicLibrary.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ArtistsController(IDbContextFactory<MusicDbContext> dbContextFactory) : ControllerBase
{
    [HttpGet(Name = "GetAllArtists")]
    public async Task<IEnumerable<ArtistResponse>> Get()
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        return await dbContext.Artists
            .Select(a => new ArtistResponse
            {
                Id = a.Id,
                Name = a.Name
            })
            .ToListAsync();
    }

    [HttpGet("{id}", Name = "GetArtistById")]
    public async Task<ActionResult<ArtistResponse>> GetById(int id)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        return await dbContext.Artists
            .Where(a => a.Id == id)
            .Select(a => new ArtistResponse
            {
                Id = a.Id,
                Name = a.Name
            })
            .FirstOrDefaultAsync()
            is ArtistResponse artist
                ? artist
                : NotFound();
    }

    [HttpPost(Name = "CreateArtist")]
    public async Task<ActionResult<ArtistResponse>> Create(CreateArtistRequest artist)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var artistEntity = new Artist { Name = artist.Name };
        dbContext.Artists.Add(artistEntity);
        await dbContext.SaveChangesAsync();

        var artistResponse = new ArtistResponse
        {
            Id = artistEntity.Id,
            Name = artistEntity.Name
        };

        return CreatedAtRoute("GetArtistById", new { id = artistResponse.Id }, artistResponse);
    }

    [HttpGet("{artistId}/albums", Name = "GetAlbumsByArtist")]
    public async Task<ActionResult<IEnumerable<AlbumResponse>>> GetAlbumsByArtist(int artistId)
    {
        // - If the provided artist does not exist, we return not found as the artist's URL path does not exist.
        // - If the provided artist does exist but does not own any albums, we return the expected empty set.
        // It's possible to achieve this behavior with a single SQL query, but allowing ourselves the extra roundtrip
        // greatly simplifies the expression.

        using var dbContext = dbContextFactory.CreateDbContext();
        if (!await dbContext.Artists.AnyAsync(a => a.Id == artistId))
        {
            return NotFound();
        }

        return await dbContext.Albums
            .Where(a => a.ArtistId == artistId)
            .Select(a => new AlbumResponse
            {
                Id = a.Id,
                Title = a.Title,
                ArtistId = a.ArtistId,
            })
            .ToListAsync();
    }

    [HttpPost("{artistId}/albums", Name = "CreateArtistAlbum")]
    public async Task<ActionResult<AlbumResponse>> CreateAlbum(int artistId, CreateAlbumRequest createAlbumRequest)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        if (!await dbContext.Artists.AnyAsync(a => a.Id == artistId))
        {
            return NotFound();
        }

        var albumEntity = new Album
        {
            Title = createAlbumRequest.Title,
            ArtistId = artistId,
        };

        dbContext.Albums.Add(albumEntity);

        await dbContext.SaveChangesAsync();

        var albumResponse = new AlbumResponse
        {
            Id = albumEntity.Id,
            Title = albumEntity.Title,
            ArtistId = albumEntity.ArtistId
        };

        return CreatedAtRoute("GetAlbumById", new { id = albumResponse.Id }, albumResponse);
    }
}
