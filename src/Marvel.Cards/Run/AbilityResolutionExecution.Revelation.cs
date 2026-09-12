using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityPaymentRules;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionRevelation
{
    internal static IReadOnlyList<GameEvent> WhenRevealed(this AbilityResolutionExecution execution, World world, Card card, int player) =>
        execution.WhenRevealed(
            world, card, player,
            new Occurrence(0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player));

    /// <inheritdoc/>
    internal static IReadOnlyList<PendingAbility> WhenRevealedAbilities(this AbilityResolutionExecution execution,
        World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        if (!execution.program.KnowsWhenRevealed(card.FaceId))
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was revealed and no ability data is written for it; "
                + $"this engine has {execution.program.Authored.Count} authored card(s)");
        }

        return [.. execution.On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenRevealed)
            .Where(ability => string.Equals(
                ability.Trigger.Event, Steps.CardRevealed, StringComparison.Ordinal))
            .Select((_, ordinal) => new PendingAbility(
                card.ObjectId, AbilityType.WhenRevealed, player, ordinal))];
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> WhenRevealed(this AbilityResolutionExecution execution,
        World world, Card card, int player, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(occurrence);

        if (!execution.program.KnowsWhenRevealed(card.FaceId))
        {
            // Authored-and-does-nothing is a different thing from nobody having
            // read the card, and only one of them is safe to treat as silence.
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was revealed and no ability data is written for it; "
                + $"this engine has {execution.program.Authored.Count} authored card(s)");
        }

        var reveals = execution.On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenRevealed)
            .Select((ability, ordinal) => (Ability: ability, Ordinal: ordinal))
            .Where(entry => string.Equals(
                entry.Ability.Trigger.Event, Steps.CardRevealed,
                StringComparison.Ordinal))
            .ToList();
        var addresses = reveals.Select(entry => new PendingAbility(
            card.ObjectId, AbilityType.WhenRevealed, player, entry.Ordinal)).ToList();
        if (world.Facts.Kind(card.FaceId) == CardKind.Treachery)
        {
            occurrence.BeginCard(card.ObjectId, addresses);
        }

        var events = new List<GameEvent>();
        if (execution.CancelWhenRevealed(world, card, player, occurrence))
        {
            return events;
        }

        // One reveal can contain several authored abilities. A non-numeric
        // keyword gained by more than one of them is still one keyword, so the
        // casts share which keyword grants have already resolved.
        var gainedKeywords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (ability, ordinal) in reveals)
        {
            // `rr:ability.step.3` -- "When Revealed" *is* the occurrence, not a
            // window around it. An interrupt or a response to a card being
            // revealed is a different ability and reaches the board through
            // `Waiting`, so matching on the condition alone would run it twice.
            var cast = new AbilityResolutionState(world, card, occurrence, player, events)
            {
                Tier = ability.Trigger.Timing,
                GainedKeywords = gainedKeywords,
            };
            cast.RestoreAbility(ordinal, []);
            cast.TrackResolution(ordinal);
            execution.Run(ability, cast);
            cast.CompleteResolution();
        }

        return events;
    }

    /// <inheritdoc/>
    internal static bool CancelWhenRevealed(this AbilityResolutionExecution execution,
        World world, Card card, int player, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(occurrence);

        var authored = execution.On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenRevealed)
            .Select((ability, ordinal) => (ability, ordinal))
            .Where(entry => string.Equals(
                entry.ability.Trigger.Event, Steps.CardRevealed,
                StringComparison.Ordinal))
            .Select(entry => new PendingAbility(
                card.ObjectId, AbilityType.WhenRevealed, player, entry.ordinal));
        var addresses = authored
            .Concat(RevealKeywords.KeywordAbilities(world, world.Facts, card, player))
            .ToList();
        var cancellation = world.Effects.Active().FirstOrDefault(effect =>
            string.Equals(effect.Kind, "cancelWhenRevealed", StringComparison.Ordinal)
            && effect.Affects == card.ObjectId);
        var kind = world.Facts.Kind(card.FaceId);
        bool mayBeCanceled = !CardKinds.IsVillain(kind) && kind != CardKind.MainScheme;
        if (!mayBeCanceled || cancellation is null || !world.Effects.Use(cancellation))
        {
            return false;
        }

        if (world.Facts.Kind(card.FaceId) == CardKind.Treachery)
        {
            occurrence.BeginCard(card.ObjectId, addresses);
        }
        foreach (var address in addresses)
        {
            occurrence.Cancel(address);
        }
        return true;
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> Boost(this AbilityResolutionExecution execution, World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        // **Not "is the card authored" but "is this half of it".** A card with
        // two abilities at two tiers -- `01168` Sweeping Swoop has a "When
        // Revealed" and a "Boost" -- would otherwise pass on the strength of
        // the half somebody had written, and the other half would go back to
        // being silent.
        var boosts = execution.On(card)
            .Where(ability => ability.Trigger.Timing == AbilityType.Boost)
            .ToList();

        if (boosts.Count == 0)
        {
            // **The star gates the complaint, not the run.** The printed
            // `Boost` attribute counts icons and `rr:boost-boost-icon.1` says a
            // star is not one, so a card with an ability and a card without
            // carry the same number and only the text box can tell them apart.
            // Asked here rather than first, so that the text box cannot veto
            // authored data.
            return world.Facts.HasBoostAbility(card.FaceId)
                ? throw new RulesNotImplementedException(
                    $"card '{card.FaceId}' was turned faceup as a boost card and prints a "
                    + "'Boost' ability that no ability data is written for")
                : [];
        }

        var events = new List<GameEvent>();
        var occurrence = new Occurrence(
            0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player);

        foreach (var (ability, ordinal) in boosts.Select((ability, ordinal) =>
                     (ability, ordinal)))
        {
            // `rr:ability` puts a "Boost" ability at the occurrence tier, like
            // "When Revealed": it is the thing happening rather than a window
            // around it, so there is nothing to offer and nothing to decline.
            var cast = new AbilityResolutionState(world, card, occurrence, player, events)
            {
                Tier = ability.Trigger.Timing,
            };
            cast.RestoreAbility(ordinal, []);
            cast.TrackResolution(ordinal);
            execution.Run(ability, cast);
            cast.CompleteResolution();
        }

        return events;
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> ResolveSpecial(this AbilityResolutionExecution execution,
        World world, Card card, int player, bool finalStep)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        var ability = execution.On(card).SingleOrDefault(candidate =>
            candidate.Trigger.Timing == AbilityType.Special)
            ?? throw new RulesNotImplementedException(
                $"card '{card.FaceId}' has no authored Special ability");
        var events = new List<GameEvent>();
        var cast = new AbilityResolutionState(
            world, card,
            new Occurrence(0, [Steps.ResolveSpecial], Subject: card.ObjectId, Player: player),
            player, events)
        {
            Tier = AbilityType.Special,
            FinalStep = finalStep,
        };
        if (execution.CanInitiate(ability, cast))
        {
            cast.RestoreAbility(0, []);
            cast.TrackResolution(0);
            execution.Run(ability, cast);
            cast.CompleteResolution();
        }
        return events;
    }
}
