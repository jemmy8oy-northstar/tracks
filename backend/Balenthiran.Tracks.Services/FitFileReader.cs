using System.IO.Compression;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Dynastream.Fit;

namespace Balenthiran.Tracks.Services;

/// <summary>Reads a run out of a Garmin FIT file with Garmin's own SDK (BEH-SYNC-7). The upload is only read, never kept.</summary>
public class FitFileReader : IFitFileReader
{
    /// <summary>A day's run is well under a megabyte; this only stops an upload (or a zip bomb) from filling memory.</summary>
    private const int MaxBytes = 20 * 1024 * 1024;

    /// <summary>FIT stores a position as a 32-bit signed fraction of a half turn: 2^31 semicircles is 180 degrees.</summary>
    private const double DegreesPerSemicircle = 180d / 2147483648d;

    public IFitActivity Read(Stream file)
    {
        var fit = new MemoryStream(FitBytes(file));
        var records = new List<RecordMesg>();
        SessionMesg? session = null;

        try
        {
            var decode = new Decode();
            var broadcaster = new MesgBroadcaster();
            decode.MesgEvent += broadcaster.OnMesg;
            decode.MesgDefinitionEvent += broadcaster.OnMesgDefinition;
            broadcaster.RecordMesgEvent += (_, e) => records.Add((RecordMesg)e.mesg);
            broadcaster.SessionMesgEvent += (_, e) => session ??= (SessionMesg)e.mesg;

            if (!decode.IsFIT(fit) || !decode.CheckIntegrity(fit))
                throw new ValidationException("That is not a readable FIT file.");

            fit.Position = 0;
            decode.Read(fit);
        }
        catch (FitException ex)
        {
            throw new ValidationException("That is not a readable FIT file.", ex);
        }

        // Garmin writes no sport as "generic"; only a file that says it is something else is refused.
        if (session?.GetSport() is { } sport && sport != Sport.Running)
            throw new ValidationException($"That activity is {sport.ToString().ToLowerInvariant()}, not a run.");

        var timed = records.Where(r => r.GetTimestamp() is not null).OrderBy(r => r.GetTimestamp().GetTimeStamp()).ToList();
        var started = ToUtc(session?.GetStartTime() ?? timed.FirstOrDefault()?.GetTimestamp())
            ?? throw new ValidationException("That FIT file has no run in it.");
        var elapsed = session?.GetTotalElapsedTime() is { } total
            ? (int)Math.Round(total)
            : timed.Count > 0 ? (int)(ToUtc(timed[^1].GetTimestamp())!.Value - started).TotalSeconds : 0;
        if (elapsed <= 0)
            throw new ValidationException("That FIT file has no run in it.");

        return new FitActivity { StartedAt = started, ElapsedSeconds = elapsed, Streams = StreamsOf(timed, started) };
    }

    /// <summary>The FIT bytes: the upload itself, or the one <c>.fit</c> inside a zip.</summary>
    private static byte[] FitBytes(Stream file)
    {
        var bytes = new MemoryStream();
        file.CopyTo(bytes);
        if (bytes.Length > MaxBytes)
            throw new ValidationException("That file is too big to be a run.");

        var data = bytes.ToArray();
        if (data.Length < 4 || data[0] != 'P' || data[1] != 'K')
            return data;

        try
        {
            using var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);
            var entry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".fit", StringComparison.OrdinalIgnoreCase))
                ?? throw new ValidationException("That zip has no .fit file in it.");
            if (entry.Length > MaxBytes)
                throw new ValidationException("That file is too big to be a run.");

            using var inner = entry.Open();
            var unzipped = new MemoryStream();
            inner.CopyTo(unzipped);
            return unzipped.ToArray();
        }
        catch (InvalidDataException ex)
        {
            throw new ValidationException("That is not a readable zip.", ex);
        }
    }

    /// <summary>
    /// The parallel streams from the records. A watch loses its fix under bridges and tunnels, so a gap in position takes
    /// the nearest known fix; with no fix at all (a treadmill) there is no route, but the rest still gives highlights.
    /// </summary>
    private static ActivityStreams StreamsOf(List<RecordMesg> records, DateTimeOffset started)
    {
        var streams = new ActivityStreams
        {
            TimeSeconds = records.Select(r => (ToUtc(r.GetTimestamp())!.Value - started).TotalSeconds).ToList(),
        };
        if (records.Count == 0)
            return streams;

        var distance = records.Select(r => r.GetDistance()).ToList();
        if (distance.All(d => d is not null))
            streams.DistanceMetres = distance.Select(d => (double)d!.Value).ToList();

        var speed = records.Select(r => r.GetEnhancedSpeed() ?? r.GetSpeed()).ToList();
        if (speed.All(s => s is not null))
            streams.VelocityMetresPerSecond = speed.Select(s => (double)s!.Value).ToList();

        var fixes = records.Select(r => r.GetPositionLat() is { } lat && r.GetPositionLong() is { } lng
            ? new[] { lat * DegreesPerSemicircle, lng * DegreesPerSemicircle }
            : null).ToList();
        var first = fixes.FirstOrDefault(f => f is not null);
        if (first is null)
            return streams;

        var last = first;
        streams.LatLng = fixes.Select(f => (IReadOnlyList<double>)(last = f ?? last)).ToList();
        return streams;
    }

    private static DateTimeOffset? ToUtc(Dynastream.Fit.DateTime? stamp) =>
        stamp is null ? null : new DateTimeOffset(System.DateTime.SpecifyKind(stamp.GetDateTime(), DateTimeKind.Utc));
}
