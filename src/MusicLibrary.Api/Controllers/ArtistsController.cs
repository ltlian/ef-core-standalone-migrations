using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MusicLibrary.Persistence;
using MusicLibrary.Persistence.Models;

namespace MusicLibrary.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ArtistsController(IDbContextFactory<MusicDbContext> dbContextFactory) : ControllerBase
{
    [HttpGet(Name = "GetAllArtists")]
    public async Task<IEnumerable<Artist>> Get()
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        return await dbContext.Artists.ToListAsync();
    }

    [HttpGet("{id}", Name = "GetArtistById")]
    public async Task<ActionResult<Artist>> GetById(int id)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var artist = await dbContext.Artists.FindAsync(id);
        if (artist == null)
        {
            return NotFound();
        }

        return artist;
    }

    [HttpPost(Name = "CreateArtist")]
    public async Task<ActionResult<Artist>> Create(Artist artist)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.Artists.Add(artist);
        await dbContext.SaveChangesAsync();
        return CreatedAtRoute("GetArtistById", new { id = artist.Id }, artist);
    }
}
