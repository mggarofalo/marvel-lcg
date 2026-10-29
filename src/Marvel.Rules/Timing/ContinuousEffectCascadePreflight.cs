using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Plans and preflights the cascade caused by departing continuous-effect sources.</summary>
internal sealed class ContinuousEffectCascadePreflight(ContinuousEffects effects)
{
    internal ContinuousEffectDeparture PreflightDepartures(
        Card[] sources, bool includeHostedCards, bool moveRoots,
        IReadOnlySet<int>? attachmentPreflightExemptions = null)
    {
        if (sources.Length == 0)
        {
            return new ContinuousEffectDeparture(effects, [], []);
        }

        int firstSuppressed = effects.suppressedConstants.Count;
        bool simulationEnded = false;
        try
        {
            var planned = new List<Card>();
            var plannedIds = new HashSet<int>();
            foreach (var source in sources)
            {
                effects.AddDeparture(source, includeHostedCards, planned, plannedIds);
            }
            var definiteIds = moveRoots
                ? new HashSet<int>()
                : [.. plannedIds];

            var restored = moveRoots ? sources.ToList() : [];
            var restoredIds = restored.Select(card => card.ObjectId).ToHashSet();
            AddLostUsesDepartures(definiteIds, restored, restoredIds, planned, plannedIds);
            ExpandDepartureCascade(definiteIds, restored, restoredIds, planned, plannedIds);

            var roots = restored.Distinct().ToArray();
            var rootTrees = roots.ToDictionary(
                root => root.ObjectId,
                root => planned
                    .Where(card => card.ObjectId == root.ObjectId
                        || effects.HasHostedAncestor(card, [root.ObjectId]))
                    .Select(card => card.ObjectId)
                    .ToArray());
            var definiteSources = planned
                .Where(source => definiteIds.Contains(source.ObjectId))
                .ToArray();

            // The derived-effect simulation has found every tentative cascade
            // root. End it before projecting physical absence, so constants
            // are now derived from the projected board itself.
            effects.suppressedConstants.RemoveRange(
                firstSuppressed, effects.suppressedConstants.Count - firstSuppressed);
            simulationEnded = true;

            var selected = PreflightSelectedDepartures(
                roots, definiteIds, definiteSources, rootTrees,
                attachmentPreflightExemptions ?? new HashSet<int>());
            var departures = definiteIds
                .Concat(selected.SelectMany(card => rootTrees[card.ObjectId]))
                .Distinct()
                .ToArray();
            return new ContinuousEffectDeparture(effects, selected, departures);
        }
        finally
        {
            if (!simulationEnded)
            {
                effects.suppressedConstants.RemoveRange(
                    firstSuppressed, effects.suppressedConstants.Count - firstSuppressed);
            }
        }
    }

    private void AddLostUsesDepartures(
        HashSet<int> definiteIds, List<Card> restored, HashSet<int> restoredIds,
        List<Card> planned, HashSet<int> plannedIds)
    {
        foreach (var card in effects.LostUsesCandidates())
        {
            if (definiteIds.Contains(card.ObjectId)) continue;
            if (restoredIds.Add(card.ObjectId)) restored.Add(card);
            if (!plannedIds.Contains(card.ObjectId))
            {
                effects.AddDeparture(card, includeHostedCards: true, planned, plannedIds);
            }
        }
    }

    private void ExpandDepartureCascade(
        HashSet<int> definiteIds, List<Card> restored, HashSet<int> restoredIds,
        List<Card> planned, HashSet<int> plannedIds)
    {
        var pending = new Queue<Card>(planned);
        while (pending.Count > 0)
        {
            var layer = Drain(pending);
            var ending = layer
                .SelectMany(card => effects.world.ConstantAbilities.Constant(effects.world, card))
                .ToArray();
            var candidates = effects.LostUsesCandidates();
            var healthBefore = effects.CaptureCharacterHealthCore();
            var blanksBefore = effects.CapturePrintedBlanks();
            effects.suppressedConstants.AddRange(ending);
            var lethal = effects.LethalAfterDerivedChange(healthBefore);
            if (lethal.Length > 0 && effects.world.Agenda.Occurrence is null
                && !blanksBefore.SequenceEqual(effects.CapturePrintedBlanks()))
                throw new RulesNotImplementedException(
                    "printed blanking health defeat requires an enclosing occurrence");
            AddCascadeCandidates(
                effects.RestoredUsesAfter(candidates), definiteIds, restored, restoredIds,
                planned, plannedIds, pending);
            AddCascadeCandidates(
                lethal, definiteIds, restored, restoredIds,
                planned, plannedIds, pending);
        }
    }

    private static List<Card> Drain(Queue<Card> pending)
    {
        var layer = new List<Card>();
        while (pending.TryDequeue(out var leaving)) layer.Add(leaving);
        return layer;
    }

    private void AddCascadeCandidates(
        IEnumerable<Card> candidates, HashSet<int> definiteIds,
        List<Card> restored, HashSet<int> restoredIds,
        List<Card> planned, HashSet<int> plannedIds, Queue<Card> pending)
    {
        foreach (var card in candidates)
        {
            if (!definiteIds.Contains(card.ObjectId) && restoredIds.Add(card.ObjectId))
            {
                restored.Add(card);
            }
            if (plannedIds.Contains(card.ObjectId)) continue;
            int before = planned.Count;
            effects.AddDeparture(card, includeHostedCards: true, planned, plannedIds);
            foreach (var added in planned.Skip(before)) pending.Enqueue(added);
        }
    }

    private Card[] PreflightSelectedDepartures(
        Card[] roots,
        IReadOnlySet<int> definiteIds,
        IReadOnlyList<Card> definiteSources,
        Dictionary<int, int[]> rootTrees,
        IReadOnlySet<int> attachmentPreflightExemptions)
    {
        var selected = new HashSet<int>();
        while (true)
        {
            var projected = definiteIds
                .Concat(selected.SelectMany(id => rootTrees[id]))
                .Distinct()
                .ToArray();
            int[] newlyEligible;
            using (ProjectOut(projected))
            {
                newlyEligible = roots.Where(card =>
                        !selected.Contains(card.ObjectId)
                        && DeckTypes.IsInPlay(card.Area.Type)
                        && (!Characteristics.IsLost(effects.world, card, "uses")
                            || CardKinds.IsCharacter(EffectiveCards.Kind(card, effects.world.Facts))
                                && StateFields.Modified(effects.world, card, "is_infinite_health",
                                    effects.world.Facts, effects.world.Players) <= 0
                                && card.Damage >= DamagePlacement.Health(effects.world, effects.world.Facts, card)))
                    .Select(card => card.ObjectId)
                    .ToArray();
            }
            if (newlyEligible.Length > 0)
            {
                selected.UnionWith(newlyEligible);
                continue;
            }

            var selectedRoots = roots.Where(card => selected.Contains(card.ObjectId))
                .Where(card => !effects.HasHostedAncestor(card, selected))
                .ToArray();

            PreflightDefiniteAttachments(
                definiteSources, definiteIds, attachmentPreflightExemptions);

            using (ProjectOut([.. definiteIds]))
            {
                foreach (var root in selectedRoots)
                {
                    Discard.PreflightProjectedAttachments(
                        effects.world,
                        root,
                        rootTrees[root.ObjectId].Skip(1).Select(id => effects.world.Cards[id]));
                }
            }

            return selectedRoots;
        }
    }

    /// <summary>Checks each hosted card at the board where its direct host departs.</summary>
    private void PreflightDefiniteAttachments(
        IReadOnlyList<Card> sources,
        IReadOnlySet<int> definiteIds,
        IReadOnlySet<int> attachmentPreflightExemptions)
    {
        foreach (var source in sources)
        {
            var direct = effects.world.Areas
                .Where(area => area.Host == source.ObjectId)
                .SelectMany(area => area.Cards)
                .Where(card => definiteIds.Contains(card.ObjectId))
                .Where(card => !attachmentPreflightExemptions.Contains(card.ObjectId))
                .ToArray();
            if (direct.Length == 0)
            {
                continue;
            }

            // Project only the direct host and earlier ancestors. The hosted
            // cards remain in play, so their own constants can react to the
            // host's absence before Permanent is read. A captured Victory root
            // begins a separate earlier departure group while the defeated
            // host's constants are still active.
            var projected = new List<int>();
            var leaving = source;
            while (definiteIds.Contains(leaving.ObjectId))
            {
                projected.Add(leaving.ObjectId);
                if (attachmentPreflightExemptions.Contains(leaving.ObjectId)
                    || leaving.Area.Host < 0
                    || leaving.Area.Host >= effects.world.Cards.Count)
                {
                    break;
                }
                leaving = effects.world.Cards[leaving.Area.Host];
            }

            using (ProjectOut(projected))
            {
                Discard.PreflightProjectedAttachments(effects.world, source, direct);
            }
        }
    }

    private ProjectionScope ProjectOut(IReadOnlyList<int> ids)
    {
        var cards = ids.Select(id => effects.world.Cards[id]).Distinct().ToArray();
        var orders = cards.Select(card => card.Area)
            .Distinct()
            .ToDictionary(area => area, area => area.Cards.ToArray());
        var detached = new Area(-1, DeckType.RemovedArea, -1, PlayArea.Villains, -1);
        foreach (var card in cards)
        {
            card.Area.Remove(card);
            card.ProjectTo(detached);
        }
        return new ProjectionScope(orders);
    }



}
