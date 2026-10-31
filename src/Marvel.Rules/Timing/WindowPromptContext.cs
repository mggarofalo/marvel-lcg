using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Describes the occurrence an optional interrupt may change.</summary>
internal static class WindowPromptContext
{
    internal static Prompt Describe(World world, Occurrence occurrence, WindowKind kind, Prompt prompt)
    {
        if (kind == WindowKind.Response) return Response(world, occurrence, prompt);
        if (kind != WindowKind.Interrupt) return prompt;
        if (occurrence.Is(HeroDefenseTiming.Defends)) return Defense(world, occurrence, prompt);
        if (occurrence.BasicUse is { } use) return prompt with
        {
            DisplayQuestion = $"Basic {use.Field} — interrupt?",
            Description = $"{world.Facts.Title(world.Cards[use.Actor].FaceId)} is making a basic {use.Field}. "
                + $"Current {use.Field} bonus: {use.Bonus}. Resolve an interrupt or pass before the power resolves.",
            ContextCardIds = [use.Actor, occurrence.Target],
        };
        if (Imminent(world, occurrence, prompt) is { } imminent) return imminent;
        if (occurrence.BoostCount is { } count && occurrence.BoostIcons is null) return BoostCount(world, count, prompt);
        if (Revealed(world, occurrence, prompt) is { } revealed) return revealed;
        return prompt;
    }

    private static Prompt? Revealed(World world, Occurrence occurrence, Prompt prompt)
    {
        if (!occurrence.Is(Steps.CardRevealed) || occurrence.Subject < 0
            || world.Cards[occurrence.Subject] is not { FaceUp: true } card) return null;
        string title = world.Facts.Title(card.FaceId);
        string revealed = occurrence.Player >= 0
            ? $"{world.Seats[occurrence.Player].Name} revealed {title}."
            : $"{title} was revealed.";
        return prompt with
        {
            DisplayQuestion = $"{title} was revealed — interrupt?",
            Description = $"{revealed} Its When Revealed effects have not resolved. "
                + "Resolve an interrupt or pass before those effects resolve.",
            ContextCardIds = [card.ObjectId],
        };
    }

    private static Prompt Defense(World world, Occurrence occurrence, Prompt prompt)
    {
        string hero = EffectiveCards.Title(world.Cards[occurrence.Actor], world.Facts);
        string enemy = EffectiveCards.Title(world.Cards[occurrence.Target], world.Facts);
        return prompt with
        {
            DisplayQuestion = $"{hero} is defending — interrupt?",
            Description = $"{hero} is defending against {enemy}. Resolve an interrupt or pass to continue.",
            ContextCardIds = [occurrence.Actor, occurrence.Target],
        };
    }

    private static Prompt? Imminent(World world, Occurrence occurrence, Prompt prompt) =>
        occurrence.Threat is { } threat ? Threat(world, threat, prompt)
            : occurrence.Readying is { } readying ? Readying(world, readying, prompt) : null;

    private static Prompt Readying(World world, CardReadyBatch readying, Prompt prompt)
    {
        var cards = readying.Targets.Select(target => world.Cards[target])
            .Where(card => readying.WouldReady(world, card)).ToArray();
        string titles = string.Join(", ", cards.Select(card => EffectiveCards.Title(card, world.Facts)));
        return prompt with
        {
            DisplayQuestion = $"{titles} would ready — interrupt?",
            Description = $"{titles} would ready. Resolve an interrupt or pass before readying.",
            ContextCardIds = [.. cards.Select(card => card.ObjectId)],
        };
    }

    private static Prompt BoostCount(World world, BoostIconCount count, Prompt prompt)
    {
        string title = EffectiveCards.Title(count.Original, world.Facts);
        string counted = count.Replaced
            ? $"The boost icons on {EffectiveCards.Title(count.Counted, world.Facts)} are counted instead of {title}"
            : $"The boost icons on {title} are being counted";
        return prompt with
        {
            DisplayQuestion = $"Counting boost icons on {title} — interrupt?",
            Description = $"{counted}; the count is currently {count.Value}. "
                + "Resolve an interrupt or pass before the count is used.",
            ContextCardIds = count.Replaced ? [count.Original.ObjectId, count.Counted.ObjectId] : [count.Original.ObjectId],
        };
    }

    private static Prompt Response(World world, Occurrence occurrence, Prompt prompt)
    {
        if (occurrence.Subject < 0 || world.Cards[occurrence.Subject] is not { FaceUp: true } card)
            return prompt;
        string? action = occurrence.Is(Steps.CardEntersPlay) ? "entered play"
            : occurrence.Is(Steps.CardPlayed) ? "was played" : null;
        if (action is null) return prompt;
        string title = EffectiveCards.Title(card, world.Facts);
        return prompt with
        {
            DisplayQuestion = $"{title} {action} — response?",
            Description = $"{title} {action}.",
            ContextCardIds = [card.ObjectId],
        };
    }

    private static Prompt Threat(World world, ThreatPlacement threat, Prompt prompt)
    {
        Card scheme = world.Cards[threat.Scheme];
        string title = world.Facts.Title(scheme.FaceId);
        long current = scheme.Tokens.GetValueOrDefault("k_threat");
        string prevention = threat.Remaining == threat.Assigned ? string.Empty
            : $" {threat.Assigned} threat was assigned; {threat.Remaining} remains after prevention.";
        return prompt with
        {
            DisplayQuestion = $"Interrupt {threat.Remaining} threat being placed on {title}?",
            Description = $"{Cause(threat.Cause)} would place {threat.Remaining} threat on {title}, "
                + $"which currently has {current} threat.{prevention} "
                + "Resolve an interrupt or pass before placement.",
            ContextCardIds = [scheme.ObjectId],
        };
    }

    private static string Cause(ThreatCause cause) => cause switch
    {
        ThreatCause.VillainPhase => "Step 1 of the villain phase",
        ThreatCause.EnemyScheme => "An enemy's scheme activation",
        ThreatCause.Incite => "Incite",
        ThreatCause.CardAbility => "A card ability",
        _ => throw new ArgumentOutOfRangeException(nameof(cause)),
    };
}
