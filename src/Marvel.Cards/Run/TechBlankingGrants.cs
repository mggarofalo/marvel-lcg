using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Derives live TECH blanking targets and checks protected text.</summary>
internal static class TechBlankingGrants
{
    internal static IEnumerable<ContinuousEffect> For(AbilityProgram program, AbilityQueryContext bindings)
    {
        var world = bindings.World;
        foreach (var target in world.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)
            && CardControl.IsPlayerCard(world.Facts, card) && Traits.Has(world, card, "TECH", world.Facts)))
        {
            if (ProtectedPermanent(bindings, target)) continue;
            if (program.Effects.Any(pair => pair.Key.Card == target.FaceId
                && pair.Value is AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.PlayersWin }))
                throw new RulesNotImplementedException("TECH blanking with an alternate victory requires protected-text support");
            yield return new ContinuousEffect(EffectSource.ConstantAbility, PrintedText.Blanked,
                Card: bindings.Source.ObjectId, Affects: target.ObjectId, Lasts: Duration.WhileInPlay);
        }
    }

    private static bool ProtectedPermanent(AbilityQueryContext bindings, Card target)
    {
        var world = bindings.World;
        if (!world.Facts.Attributes(target.FaceId).ContainsKey("Permanent")
            && !world.Effects.Active().Any(effect => effect.Kind == "permanent"
                && effect.Amount > 0 && effect.AppliesTo(world, target))) return false;
        // rr:permanent.1: effects on cards outside this set cannot blank it.
        string sourceSet = world.Facts.EncounterSet(bindings.Source.FaceId);
        string targetSet = world.Facts.EncounterSet(target.FaceId);
        if (sourceSet.Length == 0 || targetSet.Length == 0)
            throw new RulesNotImplementedException("TECH blanking of Permanent requires printed set identity");
        if (!Discard.SameSet(world.Facts, bindings.Source, target)) return true;
        throw new RulesNotImplementedException("same-set TECH blanking of Permanent requires protected-text support");
    }
}
