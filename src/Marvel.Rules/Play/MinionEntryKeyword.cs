namespace Marvel.Rules.Play;

/// <summary>A Quickstrike or Teamwork activation triggered by one batched minion's engagement.</summary>
public sealed record MinionEntryKeyword(MinionEntryBatchEntry Entry, bool Quickstrike);
