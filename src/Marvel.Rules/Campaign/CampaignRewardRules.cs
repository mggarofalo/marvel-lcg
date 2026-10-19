using System.Collections.Immutable;

namespace Marvel.Rules.Campaign;

/// <summary>Engine-owned ordered and physically bounded campaign reward choices.</summary>
public static class CampaignRewardRules
{
    /// <summary>Returns the first unresolved seat's choice without changing any state.</summary>
    public static CampaignRewardChoice? Choice(CampaignState state, CampaignDefinition definition)
    {
        if (state.Phase != CampaignPhase.Rewards || state.RewardSeats.IsEmpty) return null;
        int seat = state.RewardSeats[0];
        return state.Stage switch
        {
            CampaignStage.Crossbones => new(seat, CampaignRewardKind.Tech, false,
                Available(state, definition.Tech)),
            CampaignStage.AbsorbingMan => new(seat, CampaignRewardKind.Condition, true,
                Available(state, [.. definition.Conditions.Select(condition => condition.Basic)])),
            CampaignStage.Zola => Improvement(state, definition, seat),
            _ => throw new InvalidOperationException("this scenario has no pending reward choice"),
        };
    }

    /// <summary>Accepts one exact offered physical reward, or an explicitly permitted decline.</summary>
    public static CampaignState Choose(
        CampaignState state, CampaignDefinition definition, int seat, string? key)
    {
        CampaignRules.RequirePhase(state, CampaignPhase.Rewards);
        CampaignRewardChoice choice = Choice(state, definition)
            ?? throw new InvalidOperationException("there is no campaign reward to choose");
        if (seat != choice.Seat) throw new ArgumentException("another seat owns this campaign choice", nameof(seat));
        if (key is null && !choice.Optional)
            throw new ArgumentException("this reward choice cannot be declined", nameof(key));
        CampaignCard? selected = key is null ? null : choice.Options.SingleOrDefault(card => card.Key == key)
            ?? throw new ArgumentException("the selected campaign reward is not offered", nameof(key));
        if (selected is not null) state = Install(state, definition, choice, selected);
        state = state with { RewardSeats = state.RewardSeats.RemoveAt(0) };
        return state.RewardSeats.IsEmpty ? CampaignOutcomeRules.Advance(state) : state;
    }

    private static CampaignRewardChoice Improvement(CampaignState state, CampaignDefinition definition, int seat)
    {
        var cards = state.Players[seat].Cards;
        var eligible = definition.Conditions.Where(condition => cards.Contains(condition.Basic));
        return new(seat, CampaignRewardKind.ImproveCondition, true,
            [.. eligible.Select(condition => condition.Basic with { Face = condition.ImprovedFace })]);
    }

    private static ImmutableArray<CampaignCard> Available(
        CampaignState state, ImmutableArray<CampaignCard> cards)
    {
        var used = state.Players.SelectMany(player => player.Cards).Select(card => card.Key)
            .Concat(state.RemovedCardKeys).ToHashSet(StringComparer.Ordinal);
        return [.. cards.Where(card => !used.Contains(card.Key))];
    }

    private static CampaignState Install(
        CampaignState state, CampaignDefinition definition, CampaignRewardChoice choice, CampaignCard card)
    {
        CampaignPlayerState player = state.Players[choice.Seat];
        var cards = choice.Kind == CampaignRewardKind.ImproveCondition
            ? player.Cards.Select(existing => existing.Key == card.Key ? card : existing).ToImmutableArray()
            : player.Cards.Add(card);
        int? hp = player.RecordedHitPoints;
        if (state.Expert && choice.Kind == CampaignRewardKind.Condition)
        {
            // pack:mc10:absorbing-man-campaign-instructions.victory attaches the Condition before recording HP.
            int bonus = definition.Conditions.Single(condition => condition.Basic.Key == card.Key).HitPointBonus;
            hp = Math.Min(player.BaseHitPoints, checked(hp!.Value + bonus));
        }
        return state with { Players = state.Players.SetItem(choice.Seat,
            player with { Cards = cards, RecordedHitPoints = hp }) };
    }
}
