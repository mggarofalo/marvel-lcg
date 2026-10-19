using Marvel.Rules.Campaign;
using Marvel.Rules.Setup;

namespace Marvel.Session;

/// <summary>One accepted campaign transition; game-derived changes carry independently replayable proof.</summary>
public sealed record CampaignJournalEntry(
    string Kind,
    int Seat = -1,
    string? Choice = null,
    bool Expert = false,
    PlayerDeckRecipe? Deck = null,
    SessionSave? Proof = null,
    CampaignGameProgress? Progress = null);
