using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Checks the post-arrow effect with an exact pre-arrow discard selection.</summary>
internal static class AbilityDiscardChoiceAdmission
{
    internal static AbilityAdmissionResult? Admit(CompiledCardAbility ability,
        AbilityAdmissionContext context, AbilityCost.ChooseFromDiscard choice, IReadOnlyList<int>? selected)
    {
        var cards = AbilityCostSelection.DiscardCandidates(context.World, context.Query.Player, choice);
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
