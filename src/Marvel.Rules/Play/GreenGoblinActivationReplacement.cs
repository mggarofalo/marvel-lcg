using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Risky Business villain-face replacements for imminent activations.</summary>
internal static class GreenGoblinActivationReplacement
{
    internal static PendingAbility? Waiting(
        World world, Occurrence occurrence, WindowKind window)
    {
        if (window != WindowKind.Interrupt || world.Agenda.Current is not { } step
            || !ReferenceEquals(world.Agenda.Occurrence, occurrence)
            || step.What is not (Steps.Attack or Steps.Scheme)) return null;

        Card villain = world.Cards[step.Subject];
        if (!DeckTypes.IsInPlay(villain.Area.Type)
            || EffectiveCards.Kind(villain, world.Facts) != CardKind.EncounterVillain)
            return null;

        return Replacement(step.What, villain.FaceId).EnvironmentFace is not null
            ? new PendingAbility(villain.ObjectId, AbilityType.ForcedInterrupt, -1)
            {
                Native = NativeAbilityKind.GreenGoblinActivationReplacement,
                Would = true,
            }
            : null;
    }

    internal static Affordance Describe(World world, PendingAbility ability)
    {
        var step = world.Agenda.Current
            ?? throw new InvalidOperationException("replacement has no activation");
        var (environmentFace, counter, amount) = Replacement(step.What,
            world.Cards[ability.Card].FaceId);
        if (environmentFace is null)
            throw new InvalidOperationException("replacement is no longer eligible");
        string verb = amount > 0 ? "Place_Counter" : "Remove_Counter";
        string label = amount > 0
            ? $"Place {amount} infamy on Criminal Enterprise instead of attacking"
            : $"Remove {-amount} madness from State of Madness instead of scheming";
        return new(ability.OfferId ?? ability.Card, verb, ability.Card, ability.Player,
            label, Description: label)
        { Timing = ability.Type, DisplayLabel = label, CommitLabel = label };
    }

    internal static AbilityExecutionResult Resolve(World world, AbilityExecution invocation)
    {
        var step = world.Agenda.Current
            ?? throw new InvalidOperationException("replacement has no activation");
        Card villain = world.Cards[invocation.Ability.Card];

        // The amounts and face pair are printed on 02001a–02003b. The pack's
        // new rules explicitly say these replacements perform no attack/scheme.
        (string? environmentFace, string counter, long amount) = Replacement(step.What,
            villain.FaceId);
        if (environmentFace is null)
            throw new InvalidOperationException("replacement is no longer eligible");

        // A missing or wrong-side environment is an invalid Risky Business board.
        // Identify the printed card before changing state or appending an event.
        Card environment = world.Cards.SingleOrDefault(card =>
            card.FaceId == environmentFace
            && DeckTypes.IsInPlay(card.Area.Type)
            && EffectiveCards.Kind(card, world.Facts) == CardKind.Environment)
            ?? throw new RulesNotImplementedException(
                $"{villain.FaceId} replacement requires in-play {environmentFace}");
        string field = "c_" + counter;
        long before = environment.Tokens.GetValueOrDefault(field);
        var events = new List<GameEvent>();
        if (amount < 0)
            GreenGoblinTransition.RemoveCounters(world, environment, counter, -amount,
                step.What, events);
        else
        {
            long after = checked(before + amount);
            environment.PlaceTokens(field, amount);
            events.Add(new FieldSet(environment.ObjectId, field, before, after)
            {
                Trigger = step.What,
                Verb = "Place_Counter",
            });
        }
        invocation.Occurrence.Resolution.Resolve(invocation.Ability);
        world.Agenda.Cancel(step, invocation.Occurrence);
        return invocation.Finish(events);
    }

    private static (string? EnvironmentFace, string Counter, long Amount) Replacement(
        string what, string face) => (what, face) switch
        {
            (Steps.Attack, "02001a") => ("02006a", "infamy", 1),
            (Steps.Attack, "02002a") => ("02006a", "infamy", 2),
            (Steps.Attack, "02003a") => ("02006a", "infamy", 3),
            (Steps.Scheme, "02001b" or "02002b") => ("02006b", "madness", -1),
            (Steps.Scheme, "02003b") => ("02006b", "madness", -2),
            _ => default,
        };
}
