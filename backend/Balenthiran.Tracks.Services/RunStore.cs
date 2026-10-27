using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Database;
using Balenthiran.Tracks.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.Tracks.Services;

public class RunStore(AppDbContext db, TimeProvider? clock = null) : IRunStore
{
    private DateTimeOffset Now => (clock ?? TimeProvider.System).GetUtcNow();

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
            ContextType = run.ContextType,
            ContextName = run.ContextName,
            Route = RunRoute.Encode(run.Route),
            StreamsReadAt = run.StreamsRead ? Now : null,
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

        return runs.Select(ToRun).ToList();
    }

    public async Task<IReadOnlyList<(Guid UserId, IRun Run)>> GetUnreadStreamsAsync(
        int limit, IReadOnlyCollection<long> skip, CancellationToken cancellationToken = default)
    {
        var runs = await db.Runs.AsNoTracking()
            .Where(r => r.StreamsReadAt == null && !skip.Contains(r.ActivityId))
            .Include(r => r.Songs)
            .OrderByDescending(r => r.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return runs.Select(r => (r.UserId, ToRun(r))).ToList();
    }

    public async Task SaveStreamsAsync(
        Guid userId, long activityId, IReadOnlyList<IRunSong> songs, IReadOnlyList<IRoutePoint> route, CancellationToken cancellationToken = default)
    {
        var run = await db.Runs.Include(r => r.Songs)
            .SingleOrDefaultAsync(r => r.UserId == userId && r.ActivityId == activityId, cancellationToken);
        if (run is null)
            return;

        // Songs are matched by position: the backfill reads them in play order and changes only the flags.
        var ordered = run.Songs.OrderBy(s => s.Position).ToList();
        for (var i = 0; i < ordered.Count && i < songs.Count; i++)
        {
            ordered[i].IsTopSpeed = songs[i].IsTopSpeed;
            ordered[i].IsFastestPace = songs[i].IsFastestPace;
        }

        run.Route = RunRoute.Encode(route);
        run.StreamsReadAt = Now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IRun ToRun(RunEntity r) => new Run
    {
            ActivityId = r.ActivityId,
            Name = r.Name,
            SportType = r.SportType,
            StartedAt = r.StartedAt,
            ElapsedSeconds = r.ElapsedSeconds,
            ContextType = r.ContextType,
            ContextName = r.ContextName,
            Route = RunRoute.Decode(r.Route),
            StreamsRead = r.StreamsReadAt is not null,
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
    };
}
