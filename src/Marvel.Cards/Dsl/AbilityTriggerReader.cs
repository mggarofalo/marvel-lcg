using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Dsl.AbilityJsonReader;

namespace Marvel.Cards.Dsl;

/// <summary>Parses triggering conditions, roles and supported timing qualifiers.</summary>
internal static class AbilityTriggerReader
{
    internal static AbilityTrigger Read(JsonElement element, string card)
    {
        if (!element.TryGetProperty("trigger", out var trigger))
        {
            throw new AbilityException($"an ability on '{card}' has no 'trigger'");
        }

        Refuse(trigger, AbilityCatalog.TriggerKeys, $"a trigger on '{card}'");

        string? actor = Role(trigger, "actor", card);
        string? target = Role(trigger, "target", card);
        string? subject = TriggerSubject(trigger, actor, target, card);
        AbilityType type = TriggerType(trigger, card);

        bool would = Flag(trigger, "would", card);
        if (would && type != AbilityType.ForcedInterrupt)
            throw new AbilityException(
                $"'{card}' uses would timing outside the supported forced-interrupt window");
        string? condition = Event(trigger, card, type);
        return new AbilityTrigger(
            condition, type, subject, actor, target,
            Form(trigger, card), Also(trigger, card), Whose(trigger, card))
        {
            Would = would,
            Damage = Damage(trigger, card, condition, type),
        };
    }

    private static DamageTriggerCondition Damage(
        JsonElement trigger, string card, string? condition, AbilityType type)
    {
        if (!trigger.TryGetProperty("damage", out var value)) return DamageTriggerCondition.Dealt;
        if (condition != Steps.DamageWouldBeDealt || !AbilityTypes.IsInterrupt(type))
            throw new AbilityException($"'{card}' qualifies damage outside a damage interrupt");
        return value.ValueKind == JsonValueKind.String ? value.GetString() switch
        {
            "dealt" => DamageTriggerCondition.Dealt,
            "taken" => DamageTriggerCondition.Taken,
            _ => throw new AbilityException($"'{card}' damage qualifier must be 'dealt' or 'taken'"),
        } : throw new AbilityException($"'{card}' damage qualifier must be a string");
    }

    private static string? TriggerSubject(
        JsonElement trigger, string? actor, string? target, string card)
    {
        string? subject = Text(trigger, "subject")
            ?? (actor is null && target is null ? AbilitySubjects.This : null);
        if (subject is not null && !AbilitySubjects.All.Contains(subject))
        {
            throw new AbilityException(
                $"'{card}' triggers on subject '{subject}', which is not one of "
                + string.Join(", ", AbilitySubjects.All.Order(StringComparer.Ordinal)));
        }
        return subject;
    }

    private static AbilityType TriggerType(JsonElement trigger, string card)
    {
        string timing = Text(trigger, "timing")
            ?? throw new AbilityException($"a trigger on '{card}' has no 'timing'");
        if (!Enum.TryParse(timing, ignoreCase: false, out AbilityType type))
        {
            throw new AbilityException(
                $"'{card}' has timing '{timing}', which is not an ability type");
        }
        return type;
    }

    /// <summary>One explicit occurrence role matcher, or null.</summary>
    internal static string? Role(JsonElement trigger, string name, string card)
    {
        string? role = Text(trigger, name);
        if (role is null || AbilityRoles.All.Contains(role))
        {
            return role;
        }

        throw new AbilityException(
            $"'{card}' matches {name} '{role}', which is not one of "
            + string.Join(", ", AbilityRoles.All.Order(StringComparer.Ordinal)));
    }

    /// <summary>
    /// The triggering condition, which two ability types do not have.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:ability.5</c>: "An ability prefaced by a bold timing trigger
    /// followed by a colon is referred to as a triggered ability. An ability
    /// without a bold timing trigger is referred to as a constant ability."
    /// A constant is the half that is not, so there is no occurrence to name.
    /// </para>
    /// <para>
    /// A "Setup" ability is the other, and for a different reason: it <i>is</i>
    /// a triggered ability, but <c>rr:setup-triggered-ability.2</c> times it to
    /// a step of setup rather than to something happening in the game. Setup is
    /// not on the agenda, so no condition in <c>StepConditions.EveryCondition</c> names
    /// it — and inventing one would put a triggering condition nothing produces
    /// into the data, which is the failure the whole dataset is held against
    /// the engine to avoid.
    /// </para>
    /// <para>
    /// <b>Refused rather than ignored</b>, for the reason the class remarks
    /// give. An <c>event</c> on one of these is not a harmless extra key — it
    /// is the author having believed the card triggers on something, and the
    /// whole point of reading it back is to say so.
    /// </para>
    /// </remarks>
    internal static string? Event(JsonElement trigger, string card, AbilityType type)
    {
        string? when = Text(trigger, "event");

        if (type is AbilityType.Constant or AbilityType.Setup)
        {
            return when is null
                ? null
                : throw new AbilityException(
                    $"'{card}' is '{type}' and triggers on '{when}'. That ability type is "
                    + "not timed to an occurrence and so names no triggering condition");
        }

        return when ?? throw new AbilityException($"a trigger on '{card}' has no 'event'");
    }

    /// <summary>
    /// The form a triggered ability requires, or null —
    /// <c>rr:player-turn.5.1</c>.
    /// </summary>
    /// <remarks>
    /// "If the action ability is preceded by <b>Hero</b> or <b>Alter-Ego</b>,
    /// the player must be in the specified form in order to trigger the
    /// ability." A closed set, because it is a form and not a predicate: the
    /// only two the parenthesis ever names are the two an identity has.
    /// </remarks>
    internal static string? Form(JsonElement trigger, string card)
    {
        string? form = Text(trigger, "form");
        if (form is null || form == Forms.Hero || form == Forms.AlterEgo)
        {
            return form;
        }

        throw new AbilityException(
            $"'{card}' requires the form '{form}', and an ability may require "
            + $"'{Forms.Hero}' or '{Forms.AlterEgo}'");
    }

    /// <summary>
    /// A second triggering condition of the same occurrence, or null.
    /// </summary>
    /// <remarks>
    /// Held against <c>StepConditions.EveryCondition</c> exactly as
    /// a trigger's event is, and for the same reason: the vocabulary is the
    /// engine's spelling of a triggering condition rather than a DSL word, so a
    /// name nothing produces is a card that would never fire and this is where
    /// that is caught.
    /// </remarks>
    internal static string? Also(JsonElement trigger, string card)
    {
        string? also = Text(trigger, "alsoHappened");
        if (also is null || StepConditions.EveryCondition.Contains(also))
        {
            return also;
        }

        throw new AbilityException(
            $"'{card}' asks whether '{also}' also happened, and no step in this engine "
            + "produces that triggering condition");
    }

    /// <summary>
    /// Whose opportunity a triggered ability is, or null for its controller's.
    /// </summary>
    /// <remarks>
    /// A closed set of one, and closed for the reason
    /// <see cref="AbilitySubjects"/> is: "the seat this happened to" is a
    /// relation between a card and an occurrence, and naming the relations is
    /// what stops the field becoming a general predicate. The word is the one
    /// the effect tree already binds a player with, so a card reads the same
    /// phrase the same way in both halves.
    /// </remarks>
    internal static string? Whose(JsonElement trigger, string card)
    {
        string? whose = Text(trigger, "player");
        if (whose is null
            || whose == AbilityPlayers.TriggerPlayer
            || whose == AbilityPlayers.You)
        {
            return whose;
        }

        throw new AbilityException(
            $"'{card}' offers its ability to '{whose}', and an ability may be offered to "
            + $"'{AbilityPlayers.TriggerPlayer}', '{AbilityPlayers.You}', "
            + "or to whoever controls its card");
    }

}
