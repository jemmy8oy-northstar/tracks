using System.IO.Compression;
using Dynastream.Fit;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>Real FIT files, made with Garmin's own encoder: a run that goes north from Hyde Park at 3 m/s with one burst at 6.</summary>
public static class FitFixture
{
    public static readonly DateTimeOffset Start = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    public const double StartLat = 51.5;
    public const double Lng = -0.16;

    /// <summary>One record every 10 seconds; the burst is at 1000 s.</summary>
    public const int Records = 120;
    public const int BurstSecond = 1000;

    /// <summary>A watch run of 1,200 s. <paramref name="gps"/> false is a treadmill; <paramref name="gapFrom"/> to <paramref name="gapTo"/> (record indices) lose their fix.</summary>
    public static byte[] Fit(DateTimeOffset? start = null, bool gps = true, Sport? sport = Sport.Running, int gapFrom = 0, int gapTo = 0)
    {
        var at = start ?? Start;
        var output = new MemoryStream();
        var encode = new Encode(ProtocolVersion.V20);
        encode.Open(output);

        var id = new FileIdMesg();
        id.SetType(Dynastream.Fit.File.Activity);
        encode.Write(id);

        for (var i = 0; i < Records; i++)
        {
            var second = i * 10;
            var record = new RecordMesg();
            record.SetTimestamp(new Dynastream.Fit.DateTime(at.UtcDateTime.AddSeconds(second)));
            record.SetDistance(second * 3f);
            record.SetSpeed(second == BurstSecond ? 6f : 3f);
            if (gps && !(i >= gapFrom && i < gapTo))
            {
                record.SetPositionLat(Semicircles(StartLat + (second * 3 / 111_000d)));
                record.SetPositionLong(Semicircles(Lng));
            }

            encode.Write(record);
        }

        var session = new SessionMesg();
        session.SetStartTime(new Dynastream.Fit.DateTime(at.UtcDateTime));
        session.SetTotalElapsedTime(1200f);
        if (sport is { } s)
            session.SetSport(s);
        encode.Write(session);

        encode.Close();
        return output.ToArray();
    }

    /// <summary>The way Garmin Connect's "Export Original" ships an activity.</summary>
    public static byte[] Zip(byte[] fit, string name = "21345678901_ACTIVITY.fit")
    {
        var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = zip.CreateEntry(name).Open();
            entry.Write(fit);
        }

        return output.ToArray();
    }

    private static int Semicircles(double degrees) => (int)Math.Round(degrees * 2147483648d / 180d);
}
