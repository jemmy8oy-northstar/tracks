using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Database;
using Balenthiran.Tracks.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.Tracks.Services;

public class RunStore(AppDbContext db) : IRunStore
{
    public async Task<bool> SaveAsync(Guid userId, IRun run, CancellationToken cancellationToken = default)
    {
        if (await db.Runs.AnyAsync(r => r.UserId == userId && r.ActivityId == run.ActivityId, cancellationToken))
            return false;

        db.Runs.Add(new RunEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityId = run.ActivityId,
            Name = run.Name,
            SportType = run.SportType,
            StartedAt = run.StartedAt.ToUniversalTime(), // Postgres timestamptz only takes offset 0.
            ElapsedSeconds = run.ElapsedSeconds,
            Songs = run.Songs.Select((song, position) => new RunSongEntity
            {
                Id = Guid.NewGuid(),
                Position = position,
                Name = song.Name,
                Artists = song.Artists.ToList(),
                AlbumArtUrl = song.AlbumArtUrl,
                PlayedFrom = song.PlayedFrom.ToUniversalTime(),
                PlayedTo = song.PlayedTo.ToUniversalTime(),
                OffsetSeconds = song.OffsetSeconds,
                IsTopSpeed = song.IsTopSpeed,
                IsFastestPace = song.IsFastestPace,
            }).ToList(),
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<IRun>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var runs = await db.Runs.AsNoTracking()
            .Where(r => r.UserId == userId)
            .Include(r => r.Songs)
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync(cancellationToken);

        return runs.Select(r => (IRun)new Run
        {
            ActivityId = r.ActivityId,
            Name = r.Name,
            SportType = r.SportType,
            StartedAt = r.StartedAt,
            ElapsedSeconds = r.ElapsedSeconds,
            Songs = r.Songs.OrderBy(s => s.Position).Select(s => (IRunSong)new RunSong
            {
                Name = s.Name,
                Artists = s.Artists,
                AlbumArtUrl = s.AlbumArtUrl,
                PlayedFrom = s.PlayedFrom,
                PlayedTo = s.PlayedTo,
                OffsetSeconds = s.OffsetSeconds,
                IsTopSpeed = s.IsTopSpeed,
                IsFastestPace = s.IsFastestPace,
            }).ToList(),
        }).ToList();
    }
}
