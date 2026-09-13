using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>One physical card selected by printed face and zero-based copy number.</summary>
public sealed record SceneCard(string FaceId, int Copy = 0);
