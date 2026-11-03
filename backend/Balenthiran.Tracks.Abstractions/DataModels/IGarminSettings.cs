namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>Garmin Personal API configuration (section "Garmin", BEH-SYNC-8). Never log the key.</summary>
public interface IGarminSettings
{
    /// <summary>The personal API key. One key reads one Garmin account.</summary>
    string? ApiKey { get; }

    /// <summary>The Tracks user (a Guid) whose runs that Garmin account supplies.</summary>
    string? UserId { get; }

    string BaseUrl { get; }

    /// <summary>Minutes between passes. Spotify's recent plays only reach back 50 songs, so keep it short.</summary>
    int PollMinutes { get; }

    /// <summary><see cref="UserId"/> as a Guid; null when unset or not a Guid.</summary>
    Guid? ImportUserId { get; }

    /// <summary>True when there is a key and a user to import for. Until then the whole sync does nothing.</summary>
    bool IsConfigured { get; }
}
