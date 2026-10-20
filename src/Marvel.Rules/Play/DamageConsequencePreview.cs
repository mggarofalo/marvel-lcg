using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Explains known damage consequences without committing damage or predicting optional choices.</summary>
internal sealed class DamageConsequencePreview(World world, ICardFacts facts, Card source, Card target, bool basic = false)
{
    internal string Describe(
        Card attacker, long amount,
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
        var (taken, uncertainty) = ProjectReplacement(amount, notes);

        taken = ProjectPrevention(attacker, isAttack, taken, notes);

        string health = taken is { } known
            ? $"{current}/{maximum} → {Math.Max(0, current - known)}/{maximum} HP"
            : $"{current}/{maximum} HP · {uncertainty}";
        (health, taken) = ProjectDefeat(maximum, current, health, taken, notes);

        if (CarriesOverkill(
            attacker, taken, current, isAttack, grantsOverkill))
        {
            notes.Add($"Overkill carries {taken!.Value - current} excess damage");
        }

        string? retaliate = RetaliateNote(attacker, taken, current, isAttack);
        if (retaliate is not null) notes.Add(retaliate);

        return notes.Count == 0 ? health : $"{health} · {string.Join(" · ", notes)}";
    }

    private (long? Taken, string? Uncertainty) ProjectReplacement(
        long amount, List<string> notes)
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

    private (string Health, long? Taken) ProjectDefeat(
        long maximum, long current,
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

        return ProjectUnreplacedDefeat(maximum, current, health, taken, notes);
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

    private (string Health, long? Taken) ProjectUnreplacedDefeat(
        long maximum, long current,
        string health, long? taken, List<string> notes)
    {
        Area? villainDeck = CardKinds.IsVillain(EffectiveCards.Kind(target, facts))
            ? world.Areas.FirstOrDefault(area => area.Type == DeckType.VillainDeck)
            : null;
        Card? nextVillain = villainDeck is { Cards.Count: > 0 } ? villainDeck.Cards[^1] : null;
        if (nextVillain is not null)
        {
            long nextMaximum = DamagePlacement.Health(world, facts, nextVillain);
            health = $"{StageName(target)}: {current}/{maximum} → defeated"
                + $" · {StageName(nextVillain)} will enter at "
                + $"{nextMaximum}/{nextMaximum} HP";
        }
        else
        {
            notes.Add(CardKinds.IsVillain(EffectiveCards.Kind(target, facts))
                ? "would defeat the final villain stage"
                : "would be defeated");
        }
        return (health, taken);
    }

    private bool CarriesOverkill(
        Card attacker, long? taken,
        long current, bool isAttack, bool granted) =>
        isAttack && EffectiveCards.Kind(target, facts) is CardKind.Minion or CardKind.Ally
        && (granted || AttackKeywords.Has(world, attacker, source, Keywords.Overkill, basic))
        && taken is { } damage && damage > current;

    private string? RetaliateNote(
        Card attacker, long? taken, long current, bool isAttack)
    {
        long retaliate = isAttack
            ? StateFields.Modified(world, target, "retaliate", facts, world.Players) : 0;
        if (retaliate <= 0 || taken >= current) return null;
        if (taken is null) return $"Retaliate {retaliate} applies if the target remains in play";
        return AttackKeywords.Has(world, attacker, source, Keywords.Ranged, basic)
            ? $"Ranged ignores Retaliate {retaliate}"
            : $"Retaliate {retaliate} will hit {EffectiveCards.Title(attacker, facts)}";
    }

    private string StageName(Card card)
    {
        string title = facts.Title(card.FaceId);
        string stage = facts.Attributes(card.FaceId).GetValueOrDefault("Stage", string.Empty);
        return stage.Length == 0 ? title : $"{title} stage {stage}";
    }

    private long? ProjectPrevention(Card attacker, bool isAttack, long? taken, List<string> notes)
    {
        bool piercing = isAttack
            && AttackKeywords.Has(world, attacker, source, Keywords.Piercing, basic);
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

        return ProjectActivePrevention(taken, notes);
    }

    private long? ProjectActivePrevention(long? taken, List<string> notes)
    {
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

        return taken;
    }
}
