namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>Spotify configuration (section "Spotify"). Never log any of these values.</summary>
public interface ISpotifySettings
{
    string? ClientId { get; }
    string? ClientSecret { get; }
    string? RefreshToken { get; }

    /// <summary>True when the credentials needed to call the Spotify API are all present.</summary>
    bool IsConfigured { get; }
}
