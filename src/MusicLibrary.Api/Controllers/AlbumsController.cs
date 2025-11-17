using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MusicLibrary.Persistence;
using MusicLibrary.Persistence.Models;

namespace MusicLibrary.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AlbumsController(IDbContextFactory<MusicDbContext> dbContextFactory) : ControllerBase
{
    [HttpGet(Name = "GetAllAlbums")]
    public async Task<IEnumerable<Album>> Get()
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        return await dbContext.Albums.ToListAsync();
    }

    [HttpGet("{id}", Name = "GetAlbumById")]
    public async Task<ActionResult<Album>> GetById(int id)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var album = await dbContext.Albums.FindAsync(id);
        if (album == null)
        {
            return NotFound();
        }

        return album;
    }

    [HttpPost(Name = "CreateAlbum")]
    public async Task<ActionResult<Album>> Create(Album album)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.Albums.Add(album);
        await dbContext.SaveChangesAsync();
        return CreatedAtRoute("GetAlbumById", new { id = album.Id }, album);
    }
}
