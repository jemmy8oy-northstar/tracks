namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>Spotify app configuration (section "Spotify"). Never log any of these values.</summary>
public interface ISpotifySettings
{
    string? ClientId { get; }
    string? ClientSecret { get; }

    /// <summary>True when the app credentials needed to call the Spotify API are present.</summary>
    bool IsConfigured { get; }
}
