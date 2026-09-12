using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <content>Visibility-safe history facts collected during verified replay.</content>
public static partial class SessionReplay
{
    private sealed class ReplayHistory
    {
        private readonly List<string> resources = [];
        private readonly List<int> resourceIdsInOrder = [];
        private readonly HashSet<int> resourceIds = [];
        private readonly List<GameEvent> events = [];
        private int? actor;
        private string? actorName;
        private string? verb;
        private string? action;
        private int? subject;

        public void ObserveRoot(
            JournalUnit unit,
            JournalStep step,
            Game game,
            Prompt prompt,
            Decision decision)
        {
            Affordance? selected = decision.IsDecline
                ? null
                : prompt.Affordances.Single(option => option.Id == decision.Affordance);
            Card? anchorCard = AnchorCard(game, selected);
            actor = step.Decision.Actor;
            actorName = game.State.Seats[step.Decision.Actor].Name;
            verb = HistoryVerb(game, selected, anchorCard, decision);
            action = HistoryAction(game, unit, prompt, selected, anchorCard);
            subject = anchorCard?.ObjectId;
        }

        public void ObserveResources(JournalStep step, Decision decision, Game game)
        {
            foreach (int generator in decision.Spent.Where(resourceIds.Add))
            {
                resourceIdsInOrder.Add(generator);
                resources.Add(game.State.ResourceAbilities.ResourceGeneratorName(
                    game.State, step.Decision.Actor, generator));
            }
        }

        public void ObserveEvents(IReadOnlyList<GameEvent> resolvedEvents) =>
            events.AddRange(resolvedEvents);

        public void Append(
            List<HistoryUnitInspection> history,
            JournalUnit unit,
            int unitIndex) =>
            history.Add(new HistoryUnitInspection(
                unitIndex,
                actor ?? throw new ReplayDivergenceException(
                    $"unit {unitIndex} has no history actor"),
                actorName ?? throw new ReplayDivergenceException(
                    $"unit {unitIndex} has no history actor name"),
                unit.Role,
                unit.Phase,
                verb,
                action ?? throw new ReplayDivergenceException(
                    $"unit {unitIndex} has no history action"),
                subject,
                resourceIdsInOrder,
                resources,
                events,
                unit.Decisions[^1].Result?.Outcome));

        private static Card? AnchorCard(Game game, Affordance? selected)
        {
            if (selected?.AnchorId is not int anchor
                || anchor < 0
                || anchor >= game.State.Cards.Count)
            {
                return null;
            }

            return game.State.Cards[anchor];
        }

        private static string HistoryAction(
            Game game,
            JournalUnit unit,
            Prompt prompt,
            Affordance? selected,
            Card? anchorCard)
        {
            if (unit.Role == "phase_step")
            {
                return selected?.Label ?? prompt.Label;
            }

            return anchorCard is not null
                ? game.State.Facts.Title(anchorCard.FaceId)
                : selected?.Label ?? prompt.Label;
        }

        private static string? HistoryVerb(
            Game game,
            Affordance? selected,
            Card? anchorCard,
            Decision decision)
        {
            if (selected is not null
                && string.Equals(selected.Verb, Game.ActionVerb, StringComparison.Ordinal)
                && anchorCard is not null
                && game.State.Facts.Kind(anchorCard.FaceId) == CardKind.Event)
            {
                return CardPlay.Verb;
            }

            return decision.IsDecline && game.Phase == GamePhase.PlayerTurn
                ? Game.EndPhaseVerb
                : selected?.Verb;
        }
    }
}
