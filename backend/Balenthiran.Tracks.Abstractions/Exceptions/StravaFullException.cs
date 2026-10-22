namespace Balenthiran.Tracks.Abstractions.Exceptions;

/// <summary>
/// Strava refused a new athlete because this Strava app is at its connected-athlete cap.
/// Maps to HTTP 503 — Tracks cannot take another runner right now; nothing the caller did wrong.
/// </summary>
public sealed class StravaFullException : AppException
{
    public StravaFullException(string message)
        : base(message)
    {
    }

    public override string ErrorCode => "strava_full";
}
