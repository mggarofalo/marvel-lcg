namespace Marvel.View;

/// <summary>Who answers a prompt and what it concerns, as facts a client words for itself.</summary>
/// <param name="Seat">The answering seat.</param>
/// <param name="SeatName">The answering seat's readable name.</param>
/// <param name="Subject">How the decision relates to <paramref name="SubjectTitle"/>.</param>
/// <param name="SubjectTitle">The first readable context card's title, or null when the prompt names none.</param>
/// <param name="Optional">Whether the answering seat may decline.</param>
/// <param name="Causes">Readable titles of the visible cards whose occurrence caused the prompt.</param>
/// <param name="Instruction">The engine's prompt description, or empty when it has none.</param>
public sealed record DecisionFrame(
    int Seat,
    string SeatName,
    DecisionSubject Subject,
    string? SubjectTitle,
    bool Optional,
    IReadOnlyList<string> Causes,
    string Instruction)
{
    /// <summary>A frame with no seat, subject or instruction, for samples and synthetic prompts.</summary>
    public static DecisionFrame Empty { get; } = new(-1, "", DecisionSubject.None, null, false, [], "");
}
