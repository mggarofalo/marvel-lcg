using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Visibility-safe history facts collected during verified replay.</summary>
internal sealed class ReplayHistory
{
        private readonly List<string> resources = [];
        private readonly List<int> resourceIdsInOrder = [];
        private readonly HashSet<int> resourceIds = [];
        private readonly List<GameEvent> events = [];
        private int? actor;
        private string? actorName;
        private string? verb;
        private string? action;
        private string? phaseStepAction;
        private int? subject;

        /// <summary>
        /// Records the root decision before it resolves. The unit's role is read
        /// when the unit completes, because a terminal decision renames it.
        /// </summary>
        public void ObserveRoot(
            int decidingSeat,
            Game game,
            Prompt prompt,
            Decision decision)
        {
            Affordance? selected = decision.IsDecline
                ? null
                : prompt.Affordances.Single(option => option.Id == decision.Affordance);
            Card? anchorCard = AnchorCard(game, selected);
            actor = decidingSeat;
            actorName = game.State.Seats[decidingSeat].Name;
            verb = HistoryVerb(game, selected, anchorCard, decision);
            phaseStepAction = selected?.Label ?? prompt.Label;
            action = anchorCard is not null
                ? game.State.Facts.Title(anchorCard.FaceId)
                : phaseStepAction;
            subject = anchorCard?.ObjectId;
        }

        public void ObserveResources(int decidingSeat, Decision decision, Game game)
        {
            foreach (int generator in decision.Spent.Where(resourceIds.Add))
            {
                resourceIdsInOrder.Add(generator);
                resources.Add(game.State.ResourceAbilities.ResourceGeneratorName(
                    game.State, decidingSeat, generator));
            }
        }

        public void ObserveEvents(IReadOnlyList<GameEvent> resolvedEvents) =>
            ActivityBatches.Append(events, resolvedEvents);

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
                (unit.Role == "phase_step" ? phaseStepAction : action)
                    ?? throw new ReplayDivergenceException(
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
