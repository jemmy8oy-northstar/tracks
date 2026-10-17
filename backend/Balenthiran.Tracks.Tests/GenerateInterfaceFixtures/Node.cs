using Balenthiran.Tracks.Abstractions.Generation;

namespace Balenthiran.Tracks.Tests.GenerateInterfaceFixtures;

/// <summary>A tree node whose children are nodes of the same layer.</summary>
[GenerateInterface]
public class Node : NodeBase<Node>, INode
{
}
