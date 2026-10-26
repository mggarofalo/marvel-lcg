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
    internal static void RequireWhenRevealed(this AbilityResolutionExecution execution, string face)
    {
        if (!execution.program.KnowsWhenRevealed(face))
            throw new RulesNotImplementedException(
                $"card '{face}' was revealed and no ability data is written for it; "
                + $"this engine has {execution.program.Authored.Count} authored card(s)");
    }

    internal static IReadOnlyList<GameEvent> WhenRevealed(this AbilityResolutionExecution execution, World world, Card card, int player) =>
        execution.WhenRevealed(
            world, card, player,
            new Occurrence(0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player));

    private static bool RevealedInForm(World world, int player, string? form) =>
        form is null || player >= 0 && player < world.Seats.Count
            && Forms.In(world, world.Seats[player], world.Facts, form);

    /// <inheritdoc/>
    internal static IReadOnlyList<PendingAbility> WhenRevealedAbilities(this AbilityResolutionExecution execution,
        World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        execution.RequireWhenRevealed(card.FaceId);

        return [.. AbilityProgramQueries.ActiveOn(world, execution.program, card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenRevealed)
            .Select((ability, ordinal) => (Ability: ability, Ordinal: ordinal))
            .Where(entry => string.Equals(entry.Ability.Trigger.Event, Steps.CardRevealed, StringComparison.Ordinal)
                && RevealedInForm(world, player, entry.Ability.Trigger.Form))
            .Select(entry => new PendingAbility(
                card.ObjectId, AbilityType.WhenRevealed, player, entry.Ordinal))];
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> WhenRevealed(this AbilityResolutionExecution execution,
        World world, Card card, int player, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(occurrence);

        // Authored-and-does-nothing is distinct from an unread face.
        execution.RequireWhenRevealed(card.FaceId);

        var reveals = AbilityProgramQueries.ActiveOn(world, execution.program, card)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenRevealed)
            .Select((ability, ordinal) => (Ability: ability, Ordinal: ordinal))
            .Where(entry => string.Equals(
                entry.Ability.Trigger.Event, Steps.CardRevealed,
                StringComparison.Ordinal)
                && RevealedInForm(world, player, entry.Ability.Trigger.Form))
            .ToList();
        var addresses = reveals.Select(entry => new PendingAbility(
            card.ObjectId, AbilityType.WhenRevealed, player, entry.Ordinal)).ToList();
        if (EffectiveCards.Kind(card, world.Facts) == CardKind.Treachery)
        {
            occurrence.Resolution.BeginCard(card.ObjectId, addresses);
        }

        var events = new List<GameEvent>();
        if (execution.CancelWhenRevealed(world, card, player, occurrence) is { } canceled)
        {
            events.Add(canceled);
            return events;
        }

        var gainedKeywords = new HashSet<string>(StringComparer.Ordinal);
        execution.RunOrdered(reveals.Select(entry => entry.Ability),
            ability => new AbilityResolutionState(world, card, occurrence, player, events)
            {
                Tier = ability.Trigger.Timing,
                GainedKeywords = gainedKeywords,
            });

        return events;
    }

    /// <inheritdoc/>
    internal static WhenRevealedCanceled? CancelWhenRevealed(this AbilityResolutionExecution execution,
        World world, Card card, int player, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(occurrence);

        var authored = AbilityProgramQueries.ActiveOn(world, execution.program, card)
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
        var kind = EffectiveCards.Kind(card, world.Facts);
        bool mayBeCanceled = !CardKinds.IsVillain(kind) && kind != CardKind.MainScheme;
        if (!mayBeCanceled || cancellation is null || !world.Effects.Use(cancellation))
        {
            return null;
        }

        if (EffectiveCards.Kind(card, world.Facts) == CardKind.Treachery)
        {
            occurrence.Resolution.BeginCard(card.ObjectId, addresses);
        }
        foreach (var address in addresses)
        {
            occurrence.Resolution.Cancel(address);
        }
        var subjects = new Dictionary<int, string> { [card.ObjectId] = world.Facts.Title(card.FaceId) };
        if (cancellation.Card is { } source)
            subjects[source] = world.Facts.Title(world.Cards[source].FaceId);
        return new WhenRevealedCanceled(card.ObjectId, cancellation.Card)
        {
            Trigger = Steps.CardRevealed,
            Verb = "Cancel_When_Revealed",
            Subjects = subjects,
        };
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
        var boosts = AbilityProgramQueries.ActiveOn(world, execution.program, card)
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

        // rr:attack-enemy-activation.step.3.b: "Resolve any Boost abilities" is
        // part of this activation. Capture its identity before any ability can
        // suspend or request a child activation. Responses have their own casts.
        var damageOrigin = BoostDamageOrigin.Capture(world);
        execution.RunOrdered(boosts,
            ability => new AbilityResolutionState(world, card, occurrence, player, events)
            {
                Tier = ability.Trigger.Timing,
                DamageOrigin = damageOrigin,
            });

        return events;
    }

    /// <inheritdoc/>
    internal static IReadOnlyList<GameEvent> ResolveSpecial(this AbilityResolutionExecution execution,
        World world, Card card, int player, bool finalStep)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        var ability = AbilityProgramQueries.ActiveOn(world, execution.program, card).SingleOrDefault(candidate =>
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
