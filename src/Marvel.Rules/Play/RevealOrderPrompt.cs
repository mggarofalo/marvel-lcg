using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
namespace Marvel.Rules.Play;

/// <summary>Describes the next choice among simultaneously pending reveal abilities.</summary>
internal static class RevealOrderPrompt
{
    internal static Prompt Describe(
        World world, ICardFacts facts, PhaseStep step)
    {
        var card = world.Cards[step.Subject];
        var operation = step.Require<RevealAbilityOperation>();
        var distinct = (operation.Abilities ?? []).Select((ability, index) => (ability, index))
            .DistinctBy(entry => entry.ability).ToList();
        var handles = distinct.Select(entry => (operation.Candidates ?? [])[entry.index]).ToList();
        var abilities = distinct.Select(entry => entry.ability).ToList();
        if (handles.Count != abilities.Count || handles.Count < 2)
        {
            throw new InvalidOperationException("reveal ordering has no simultaneous abilities");
        }

        return new Prompt(
            world.FirstPlayer,
            Question.Order,
            TimingPriority.Occurrence,
            Steps.CardRevealed,
            $"order When Revealed abilities on {card.FaceId}",
            false,
            [.. handles.Select((handle, index) => new Affordance(
                handle,
                "Resolve",
                card.ObjectId,
                world.FirstPlayer,
                RevealAbilityLabel(facts, card, abilities[index]))
            {
                DisplayLabel = RevealDisplayLabel(world, facts, card, abilities[index]),
                CommitLabel = $"Resolve {RevealDisplayLabel(world, facts, card, abilities[index])} next",
                Description = abilities[index].Ordinal == Reveal.InciteResolutionOrdinal
                    ? $"Place {StateFields.Modified(world, card, "incite", facts, world.Players)} threat on the main scheme."
                    : abilities[index].Ordinal == Reveal.SurgeResolutionOrdinal
                        ? "Deal an additional encounter card to the revealing player."
                        : RevealChoiceDescription.Printed(facts, card.FaceId, abilities[index].Ordinal),
            })])
        {
            ContextCardIds = [card.ObjectId],
            DisplayQuestion = "Which ability resolves next?",
            // rr:incite-x.1: Incite is equivalent to “When Revealed: Place X threat on the main scheme.”
            // rr:simultaneous-resolution: the first player orders effects with the same bold timing trigger.
            Description = "These effects share the When Revealed timing. The first player chooses which resolves next.",
        };
    }

    private static string RevealDisplayLabel(World world, ICardFacts facts, Card card, PendingAbility ability) =>
        ability.Ordinal == Reveal.InciteResolutionOrdinal
            ? $"Incite {StateFields.Modified(world, card, "incite", facts, world.Players)}"
            : ability.Ordinal == Reveal.SurgeResolutionOrdinal ? "Surge" : "When Revealed";

    private static string RevealAbilityLabel(
        ICardFacts facts, Card card, PendingAbility ability) => ability.Ordinal switch
        {
            Reveal.InciteResolutionOrdinal => "Incite",
            Reveal.SurgeResolutionOrdinal => "Surge",
            _ => $"{facts.Title(card.FaceId)} When Revealed {ability.Ordinal + 1}",
        };

}
