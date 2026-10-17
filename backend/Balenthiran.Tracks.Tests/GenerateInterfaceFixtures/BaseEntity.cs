using Balenthiran.Tracks.Abstractions.Generation;

namespace Balenthiran.Tracks.Tests.GenerateInterfaceFixtures;

/// <summary>A mirrored base class — see <see cref="DerivedEntity"/>.</summary>
[GenerateInterface]
public class BaseEntity : IBaseEntity
{
    public string Id { get; set; } = "";
}
