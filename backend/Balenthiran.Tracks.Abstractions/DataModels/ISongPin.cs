namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>A song that played during a workout, with where it sits on the route.</summary>
public interface ISongPin
{
    string Title { get; set; }
    string Artist { get; set; }
    string CoverColour { get; set; }
    DateTimeOffset StartedAt { get; set; }

    /// <summary>Whole seconds from the workout's start to the song's start.</summary>
    int ElapsedSeconds { get; set; }

    /// <summary>Metres covered while the song played. Null when the workout has no route.</summary>
    double? DistanceMetres { get; set; }

    /// <summary>The route point nearest the song's start. Null when the workout has no route.</summary>
    IRoutePoint? Pin { get; set; }
}
