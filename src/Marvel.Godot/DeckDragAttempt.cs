using Marvel.Client;

namespace Marvel.Godot;

/// <summary>A captured drag intent and its read-only validation, independent of pointer lifetime.</summary>
internal sealed record DeckDragAttempt(string Token, long Revision, string[] Faces, Task<DeckEditProposal?> Check)
{
    internal bool Dropped { get; set; }
}
