using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Describes the occurrence an optional interrupt may change.</summary>
internal static class WindowPromptContext
{
    internal static Prompt Describe(World world, Occurrence occurrence, WindowKind kind, Prompt prompt)
    {
        if (kind != WindowKind.Interrupt) return prompt;
        if (occurrence.Threat is { } threat) return Threat(world, threat, prompt);
        if (occurrence.Is(Steps.CardRevealed) && occurrence.Subject >= 0
            && world.Cards[occurrence.Subject] is { FaceUp: true } card)
        {
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
        return prompt;
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
