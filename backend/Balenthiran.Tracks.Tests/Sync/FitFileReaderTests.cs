using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Services;
using Dynastream.Fit;

namespace Balenthiran.Tracks.Tests.Sync;

public class FitFileReaderTests
{
    private static Balenthiran.Tracks.Abstractions.DataModels.IFitActivity Read(byte[] bytes) =>
        new FitFileReader().Read(new MemoryStream(bytes));

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public void a_fit_file_gives_the_start_the_duration_and_the_route_in_degrees()
    {
        var run = Read(FitFixture.Fit());

        Assert.Equal(FitFixture.Start, run.StartedAt);
        Assert.Equal(1200, run.ElapsedSeconds);
        Assert.Equal(FitFixture.Records, run.Streams.TimeSeconds.Count);
        Assert.Equal(FitFixture.Records, run.Streams.LatLng.Count);
        Assert.Equal(FitFixture.StartLat, run.Streams.LatLng[0][0], 5);
        Assert.Equal(FitFixture.Lng, run.Streams.LatLng[0][1], 5);
        Assert.True(run.Streams.LatLng[^1][0] > run.Streams.LatLng[0][0]);
        Assert.Equal(6d, run.Streams.VelocityMetresPerSecond[FitFixture.BurstSecond / 10], 3);
        Assert.Equal(3570d, run.Streams.DistanceMetres[^1], 3);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public void the_zip_garmin_connect_exports_works_like_the_fit_inside_it()
    {
        var plain = Read(FitFixture.Fit());
        var zipped = Read(FitFixture.Zip(FitFixture.Fit()));

        Assert.Equal(plain.StartedAt, zipped.StartedAt);
        Assert.Equal(plain.ElapsedSeconds, zipped.ElapsedSeconds);
        Assert.Equal(plain.Streams.LatLng.Count, zipped.Streams.LatLng.Count);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public void a_file_with_no_gps_still_gives_a_run_but_no_route()
    {
        var run = Read(FitFixture.Fit(gps: false));

        Assert.Equal(1200, run.ElapsedSeconds);
        Assert.Empty(run.Streams.LatLng);
        Assert.Equal(FitFixture.Records, run.Streams.VelocityMetresPerSecond.Count);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public void a_lost_fix_takes_the_nearest_known_position_so_the_route_stays_in_step()
    {
        var run = Read(FitFixture.Fit(gapFrom: 0, gapTo: 3));

        Assert.Equal(FitFixture.Records, run.Streams.LatLng.Count);
        Assert.Equal(run.Streams.LatLng[3][0], run.Streams.LatLng[0][0]);
    }

    [Theory]
    [Trait("Behaviour", "BEH-SYNC-7")]
    [InlineData("not a fit file at all")]
    [InlineData("")]
    [InlineData("PK\u0003\u0004 and then nothing a zip would hold")]
    public void anything_else_is_refused(string text)
    {
        var bytes = System.Text.Encoding.Latin1.GetBytes(text);

        Assert.Throws<ValidationException>(() => Read(bytes));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public void a_damaged_fit_file_is_refused()
    {
        var fit = FitFixture.Fit();
        fit[fit.Length / 2] ^= 0xFF;

        Assert.Throws<ValidationException>(() => Read(fit));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public void a_zip_with_no_fit_in_it_is_refused()
    {
        Assert.Throws<ValidationException>(() => Read(FitFixture.Zip(FitFixture.Fit(), "notes.txt")));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public void a_ride_is_not_a_run()
    {
        Assert.Throws<ValidationException>(() => Read(FitFixture.Fit(sport: Sport.Cycling)));
    }
}
