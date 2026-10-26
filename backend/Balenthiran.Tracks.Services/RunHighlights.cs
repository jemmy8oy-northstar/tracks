using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.Services;

public class RunHighlights : IRunHighlights
{
    /// <summary>Samples either side of the centre in the moving average (2 + 1 + 2 = a 5-sample window).</summary>
    private const int SmoothingRadius = 2;

    /// <summary>A song covering less of the run than this cannot win fastest pace (a 10 s tail would be noise).</summary>
    private const double MinPaceSpanSeconds = 30;

    public IRunHighlightPicks Pick(IReadOnlyList<IRunSong> songs, int elapsedSeconds, IActivityStreams? streams)
    {
        var picks = new RunHighlightPicks();
        if (streams is null || songs.Count == 0 || streams.TimeSeconds.Count < 2
            || streams.DistanceMetres.Count != streams.TimeSeconds.Count)
            return picks;

        var runEnd = Math.Min(elapsedSeconds, streams.TimeSeconds[^1]);
        var spans = songs
            .Select(s => (Start: Math.Max(0d, s.OffsetSeconds), End: Math.Min(runEnd, s.OffsetSeconds + (s.PlayedTo - s.PlayedFrom).TotalSeconds)))
            .ToList();

        picks.TopSpeedIndex = TopSpeed(spans, streams, runEnd);
        picks.FastestPaceIndex = FastestPace(spans, streams);
        return picks;
    }

    private static int? TopSpeed(List<(double Start, double End)> spans, IActivityStreams streams, double runEnd)
    {
        var velocity = streams.VelocityMetresPerSecond;
        if (velocity.Count != streams.TimeSeconds.Count)
            return null;

        var best = -1;
        var bestSpeed = 0.0;
        for (var i = 0; i < velocity.Count; i++)
        {
            var from = Math.Max(0, i - SmoothingRadius);
            var to = Math.Min(velocity.Count - 1, i + SmoothingRadius);
            var sum = 0.0;
            for (var j = from; j <= to; j++)
                sum += velocity[j];

            var speed = sum / (to - from + 1);
            if (speed > bestSpeed)
            {
                best = i;
                bestSpeed = speed;
            }
        }

        if (best < 0)
            return null;

        var t = streams.TimeSeconds[best];
        for (var i = 0; i < spans.Count; i++)
        {
            if (t >= spans[i].Start && (t < spans[i].End || (t == spans[i].End && t == runEnd)))
                return i;
        }

        return null;
    }

    private static int? FastestPace(List<(double Start, double End)> spans, IActivityStreams streams)
    {
        int? best = null;
        var bestPace = 0.0;
        for (var i = 0; i < spans.Count; i++)
        {
            var seconds = spans[i].End - spans[i].Start;
            if (seconds < MinPaceSpanSeconds)
                continue;

            var pace = (DistanceAt(streams, spans[i].End) - DistanceAt(streams, spans[i].Start)) / seconds;
            if (pace > bestPace)
            {
                best = i;
                bestPace = pace;
            }
        }

        return best;
    }

    /// <summary>Cumulative distance at a moment, interpolated between the two samples around it.</summary>
    private static double DistanceAt(IActivityStreams streams, double seconds)
    {
        var time = streams.TimeSeconds;
        var distance = streams.DistanceMetres;
        if (seconds <= time[0])
            return distance[0];
        if (seconds >= time[^1])
            return distance[^1];

        var hi = 1;
        while (time[hi] < seconds)
            hi++;

        var span = time[hi] - time[hi - 1];
        return span <= 0 ? distance[hi] : distance[hi - 1] + ((distance[hi] - distance[hi - 1]) * (seconds - time[hi - 1]) / span);
    }
}
