using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Describes the first player's choices during a batched minion entry.</summary>
internal static class MinionEntryBatchPrompts
{
    internal static Prompt EngagedPlayer(World world, PhaseStep step, MinionEntryBatchOperation operation)
    {
        var minion = world.Cards[operation.Minion
            ?? throw new InvalidOperationException("the engaged-player choice has no minion")];
        string title = EffectiveCards.Title(minion, world.Facts);
        return new Prompt(world.FirstPlayer, Question.Element, TimingPriority.Untimed, step.What,
            $"{world.Seats[world.FirstPlayer].Name} chooses which player {minion.FaceId} engages", false,
            [.. operation.Tied.Select(seat => new Affordance(world.Seats[seat].IdentityCard.ObjectId, "Engage",
                world.Seats[seat].IdentityCard.ObjectId, seat, world.Seats[seat].Name)
            {
                DisplayLabel = $"Engage {world.Seats[seat].Name}",
                CommitLabel = $"{title} engages {world.Seats[seat].Name}",
                Description = $"Put {title} into play engaged with {world.Seats[seat].Name}.",
            })])
        {
            ContextCardIds = [minion.ObjectId],
            DisplayQuestion = $"Which player does {title} engage?",
            // rr:first-player.1: the first player selects among multiple eligible targets.
            Description = "These players are tied for the fewest engaged minions. The first player chooses.",
        };
    }

    internal static Prompt Keywords(World world, PhaseStep step, MinionEntryBatchOperation operation,
        IReadOnlyList<int> eligible) =>
        new(world.FirstPlayer, Question.Order, TimingPriority.ForcedResponse, step.What,
            "order Quickstrike and Teamwork after the minions engaged", false,
            [.. eligible.Select(index =>
            {
                var keyword = operation.Pending[index];
                var card = world.Cards[keyword.Entry.Card];
                string title = EffectiveCards.Title(card, world.Facts);
                string name = keyword.Quickstrike ? "Quickstrike" : "Teamwork";
                return new Affordance(index + 1, "Resolve", card.ObjectId, world.FirstPlayer, name)
                {
                    DisplayLabel = $"{title}: {name}",
                    CommitLabel = $"Resolve {title}'s {name} next",
                    Description = keyword.Quickstrike
                        ? $"{title} attacks {world.Seats[keyword.Entry.Player].Name}."
                        : $"{title} activates against {world.Seats[keyword.Entry.Player].Name}.",
                };
            })])
        {
            ContextCardIds = [.. eligible.Select(index => operation.Pending[index].Entry.Card).Distinct()],
            DisplayQuestion = "Which forced response resolves next?",
            // rr:forced.5: the first player orders simultaneous forced abilities.
            Description = "Every minion has engaged. The first player chooses which forced response resolves next.",
        };
}
