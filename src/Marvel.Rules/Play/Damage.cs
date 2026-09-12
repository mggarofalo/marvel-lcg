using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// Dealing damage to a character — <c>rr:damage</c>.
/// </summary>
/// <remarks>
/// <para>
/// One path, because <c>rr:damage</c> is one rule however the damage arrived.
/// An enemy attacking a hero, a hero attacking a minion and a card ability
/// dealing 1 all reduce remaining hit points the same way and all check for
/// defeat the same way.
/// </para>
/// <para>
/// <c>rr:hit-points.2</c> and <c>.3</c> describe two different bookkeepings —
/// a dial for identities and villains, tokens for allies and minions — but
/// they are the same arithmetic and the digest records the result of it,
/// <c>health</c>, for all four. See <c>Card.Damage</c>.
/// </para>
/// </remarks>
public static class Damage
{
    /// <inheritdoc cref="DamagePlacement.Deal"/>
    public static bool Deal(
        World world, ICardFacts facts, Card source, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, int by = -1) =>
        DamagePlacement.Deal(world, facts, source, target, amount, trigger, verb, events, by);

    /// <inheritdoc cref="DamagePlacement.DealOutcome"/>
    public static Outcome DealOutcome(
        World world, ICardFacts facts, Card source, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, int by = -1) =>
        DamagePlacement.DealOutcome(
            world, facts, source, target, amount, trigger, verb, events, by);

    /// <inheritdoc cref="DamagePlacement.Health"/>
    public static long Health(World world, ICardFacts facts, Card character) =>
        DamagePlacement.Health(world, facts, character);

    /// <summary>Resolves an attack whose attacker is also its damage source.</summary>
    public static AttackResult Attack(
        World world, ICardFacts facts, Card attacker, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, bool retaliate = true) =>
        DamageAttacks.Attack(
            world, facts, attacker, target, amount, trigger, verb, events, retaliate);

    /// <summary>Resolves an attack with a distinct damage source.</summary>
    public static AttackResult Attack(
        World world, ICardFacts facts, Card attacker, Card source, Card target, long amount,
        string trigger, string verb, List<GameEvent> events, bool retaliate = true) =>
        DamageAttacks.Attack(
            world, facts, attacker, source, target, amount, trigger, verb, events, retaliate);

    /// <inheritdoc cref="DamageAttacks.FinishAttack"/>
    public static void FinishAttack(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events) =>
        DamageAttacks.FinishAttack(world, facts, step, events);

    /// <inheritdoc cref="DamageAttacks.Retaliate"/>
    public static void Retaliate(
        World world, ICardFacts facts, Card attacked, Card attacker,
        string trigger, List<GameEvent> events) =>
        DamageAttacks.Retaliate(world, facts, attacked, attacker, trigger, events);

    /// <inheritdoc cref="DamageRecovery.Heal"/>
    public static long Heal(
        World world, ICardFacts facts, Card target, long amount,
        string trigger, string verb, List<GameEvent> events) =>
        DamageRecovery.Heal(world, facts, target, amount, trigger, verb, events);

    /// <inheritdoc cref="DamageRecovery.MoveDamage"/>
    public static long MoveDamage(
        World world, ICardFacts facts, Card source, Card from, Card to, long amount,
        string trigger, string verb, List<GameEvent> events, int by = -1) =>
        DamageRecovery.MoveDamage(
            world, facts, source, from, to, amount, trigger, verb, events, by);

    /// <summary>Whether dealing damage finished, defeated, or suspended.</summary>
    public enum Outcome
    {
        /// <summary>The damage procedure finished without defeating the target.</summary>
        NotDefeated,
        /// <summary>The damage procedure finished and defeated the target.</summary>
        Defeated,
        /// <summary>The damage is placed but a defeat decision remains outstanding.</summary>
        Suspended,
    }

    /// <summary>The characters, damage, and excess produced by one attack.</summary>
    public sealed record AttackResult(
        IReadOnlyList<Card> Characters, long Amount, bool Suspended = false,
        long Excess = 0, long Dealt = 0, long Taken = 0);

    /// <summary>Describes the presently knowable result of committing one attack.</summary>
    /// <remarks>
    /// This engine-owned preview follows constant prohibitions, forced replacement,
    /// Piercing, Tough and active prevention. Optional future windows and
    /// would-be-defeated abilities remain future decisions rather than guesses.
    /// </remarks>
    public static string PreviewAttack(
        World world, ICardFacts facts, Card attacker, Card source, Card target, long amount,
        bool grantsOverkill = false) => Preview(
            world, facts, attacker, source, target, amount,
            isAttack: true, grantsOverkill);

    /// <summary>Describes presently knowable non-attack damage consequences.</summary>
    public static string PreviewDamage(
        World world, ICardFacts facts, Card source, Card target, long amount) => Preview(
            world, facts, source, source, target, amount,
            isAttack: false, grantsOverkill: false);

    private static string Preview(
        World world, ICardFacts facts, Card attacker, Card source, Card target, long amount,
        bool isAttack, bool grantsOverkill)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        long maximum = DamagePlacement.Health(world, facts, target);
        long current = Math.Max(0, maximum - target.Damage);
        var notes = new List<string>();
        long? taken = amount;
        string? uncertainty = null;

        (taken, uncertainty) = ProjectReplacement(
            world, target, source, amount, notes);

        bool piercing = isAttack
            && Keywords.Has(world, attacker, Keywords.Piercing, facts);
        bool tough = Statuses.Has(world, target, Statuses.Tough);
        if (taken is { } dealt)
        {
            var assignment = DamageAssignment.AfterReplacement(dealt, tough && !piercing);
            taken = assignment.Taken;
            if (assignment.SpendsTough)
            {
                notes.Add("Tough prevents the damage and is discarded");
            }
            else if (assignment.Dealt > 0 && tough && piercing)
            {
                notes.Add("Piercing discards Tough");
            }
        }

        if (taken > 0)
        {
            ContinuousEffect? prevention = world.Effects.Active().FirstOrDefault(effect =>
                string.Equals(effect.Kind, "preventDamage", StringComparison.Ordinal)
                && effect.Affects == target.ObjectId);
            if (prevention is not null)
            {
                long prevented = prevention.Amount <= 0
                    ? taken.Value
                    : Math.Min(taken.Value, prevention.Amount);
                taken -= prevented;
                string sourceName = prevention.Card is { } card
                    ? facts.Title(world.Cards[card].FaceId)
                    : "An active effect";
                notes.Add($"{sourceName} prevents {prevented} damage");
            }
        }

        string health = taken is { } known
            ? $"{current}/{maximum} → {Math.Max(0, current - known)}/{maximum} HP"
            : $"{current}/{maximum} HP · {uncertainty}";
        (health, taken) = ProjectDefeat(
            world, facts, target, maximum, current, health, taken, notes);

        if (CarriesOverkill(
            world, facts, attacker, target, taken, current, isAttack, grantsOverkill))
        {
            notes.Add($"Overkill carries {taken!.Value - current} excess damage");
        }

        string? retaliate = RetaliateNote(world, facts, attacker, target, taken, current, isAttack);
        if (retaliate is not null) notes.Add(retaliate);

        return notes.Count == 0 ? health : $"{health} · {string.Join(" · ", notes)}";
    }

    private static (long? Taken, string? Uncertainty) ProjectReplacement(
        World world, Card target, Card source, long amount, List<string> notes)
    {
        if (!world.DamageAbilities.CanTakeDamage(world, target, source))
        {
            notes.Add("cannot take damage from this source");
            return (0, null);
        }
        DamageProjection replacement = world.DamageAbilities.PreviewDamageReplacement(
            world, target, source, amount);
        long? taken = replacement.Result switch
        {
            RuleProjection<long>.Known exact => exact.Value,
            RuleProjection<long>.Possible => null,
            RuleProjection<long>.Unsupported => null,
            _ => throw new InvalidOperationException("Unknown damage projection outcome."),
        };
        string? uncertainty = replacement.Result switch
        {
            RuleProjection<long>.Possible => "damage has multiple possible outcomes",
            RuleProjection<long>.Unsupported unsupported => unsupported.Reason,
            _ => null,
        };
        if (!string.IsNullOrWhiteSpace(replacement.Note))
        {
            notes.Add(replacement.Note);
        }
        return (taken, uncertainty);
    }

    private static (string Health, long? Taken) ProjectDefeat(
        World world, ICardFacts facts, Card target, long maximum, long current,
        string health, long? taken, List<string> notes)
    {
        if (taken is null || taken < current || current <= 0)
        {
            return (health, taken);
        }
        DefeatProjection? defeat = world.DamageAbilities.PreviewDefeatReplacement(
            world, target, maximum);
        if (defeat is not null)
        {
            return ProjectDefeatReplacement(defeat, maximum, current, taken, notes);
        }

        return ProjectUnreplacedDefeat(world, facts, target, maximum, current, health, taken, notes);
    }

    private static (string Health, long? Taken) ProjectDefeatReplacement(
        DefeatProjection defeat, long maximum, long current, long? taken, List<string> notes)
    {
        string health = defeat.RemainingHealth is { } remaining
            ? $"{current}/{maximum} → {remaining}/{maximum} HP"
            : $"{current}/{maximum} HP · defeat result depends on a forced interrupt";
        notes.Add(defeat.Note);
        return (health, defeat.RemainingHealth is { } saved && saved > 0 ? 0 : taken);
    }

    private static (string Health, long? Taken) ProjectUnreplacedDefeat(
        World world, ICardFacts facts, Card target, long maximum, long current,
        string health, long? taken, List<string> notes)
    {
        Area? villainDeck = CardKinds.IsVillain(facts.Kind(target.FaceId))
            ? world.Areas.FirstOrDefault(area => area.Type == DeckType.VillainDeck)
            : null;
        Card? nextVillain = villainDeck is { Cards.Count: > 0 } ? villainDeck.Cards[^1] : null;
        if (nextVillain is not null)
        {
            long nextMaximum = DamagePlacement.Health(world, facts, nextVillain);
            health = $"{StageName(facts, target)}: {current}/{maximum} → defeated"
                + $" · {StageName(facts, nextVillain)} will enter at "
                + $"{nextMaximum}/{nextMaximum} HP";
        }
        else
        {
            notes.Add(CardKinds.IsVillain(facts.Kind(target.FaceId))
                ? "would defeat the final villain stage"
                : "would be defeated");
        }
        return (health, taken);
    }

    private static bool CarriesOverkill(
        World world, ICardFacts facts, Card attacker, Card target, long? taken,
        long current, bool isAttack, bool granted) =>
        isAttack && facts.Kind(target.FaceId) is CardKind.Minion or CardKind.Ally
        && (granted || Keywords.Has(world, attacker, Keywords.Overkill, facts))
        && taken is { } damage && damage > current;

    private static string? RetaliateNote(
        World world, ICardFacts facts, Card attacker, Card target,
        long? taken, long current, bool isAttack)
    {
        long retaliate = isAttack
            ? StateFields.Modified(world, target, "retaliate", facts, world.Players) : 0;
        if (retaliate <= 0 || taken >= current) return null;
        if (taken is null) return $"Retaliate {retaliate} applies if the target remains in play";
        return Keywords.Has(world, attacker, Keywords.Ranged, facts)
            ? $"Ranged ignores Retaliate {retaliate}"
            : $"Retaliate {retaliate} will hit {facts.Title(attacker.FaceId)}";
    }

    private static string StageName(ICardFacts facts, Card card)
    {
        string title = facts.Title(card.FaceId);
        string stage = facts.Attributes(card.FaceId).GetValueOrDefault("Stage", string.Empty);
        return stage.Length == 0 ? title : $"{title} stage {stage}";
    }

    /// <summary>Damage fixed through step 4, ready for simultaneous placement.</summary>
    internal sealed record PlacedDamage(Card Target, long Dealt, long Taken)
    {
        /// <summary>Whether the character actually took damage.</summary>
        public bool Landed => Taken > 0;
    }
}
