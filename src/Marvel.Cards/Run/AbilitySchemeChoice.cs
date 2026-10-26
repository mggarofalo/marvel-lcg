using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Uses the engine scheme relation for pre-arrow physical commitment.</summary>
internal static class AbilitySchemeChoice
{
    internal static IReadOnlyList<Card> Candidates(World world, Card source, int player) =>
        new CardQueryEvaluator(new AbilityQueryContext(world, source,
            new Occurrence(0, [Steps.TurnAction], Subject: source.ObjectId, Player: player),
            player, source.Incarnation, null, null, null, []), null).Evaluate(AbilityCardQuery.Schemes);

    internal static void Validate(World world, Card source, int player, IReadOnlyList<int> chosen)
    {
        if (chosen.Count != 1 || !Candidates(world, source, player).Any(card => card.ObjectId == chosen[0]))
            throw new RulesNotImplementedException("chooseScheme requires one current in-play scheme");
    }

    internal static AbilityAdmissionResult? Admit(CompiledCardAbility ability,
        AbilityAdmissionContext context, IReadOnlyList<int>? selected)
    {
        var cards = Candidates(context.World, context.Source, context.Query.Player);
        if (selected is not null && (selected.Count != 1 || !cards.Any(card => card.ObjectId == selected[0])))
            return null;
        foreach (var card in cards.Where(card => selected is null || card.ObjectId == selected[0]))
        {
            var result = AbilityEntryAdmission.Admit(ability.Effect, context.WithChosen(card));
            if (result.IsAdmissible) return result;
        }
        return null;
    }
}
