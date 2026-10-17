using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class User : IUser
{
    public Guid Id { get; set; }
    public long StravaAthleteId { get; set; }
    public bool SpotifyConnected { get; set; }
}
