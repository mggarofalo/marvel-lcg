namespace Marvel.Rules.Play;

/// <summary>Explicit placement instructions when a card enters the table.</summary>
/// <remarks>The event verb is engine wire vocabulary, not printed card text.</remarks>
public sealed record CardEntryPlacement(
    int? AttachmentTarget = null, int? SetupController = null, string Verb = "Reveal");
