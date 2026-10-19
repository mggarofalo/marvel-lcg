namespace Marvel.Godot;

/// <summary>One explicit current offer, optional target and commitment intent.</summary>
internal sealed record CardMenuChoice(int Offer, int? Target, bool Commit);
