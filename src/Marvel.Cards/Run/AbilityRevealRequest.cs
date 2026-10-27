using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal sealed record AbilityRevealRequest(int Card, int Player, EncounterRevealOrigin Origin);
