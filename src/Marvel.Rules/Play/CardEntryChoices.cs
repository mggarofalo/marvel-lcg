namespace Marvel.Rules.Play;

/// <summary>Attachment selection and committed payment for one card entry.</summary>
internal sealed record CardEntryChoices(IReadOnlyList<int> Targets, string Paid);
