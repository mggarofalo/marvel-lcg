using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Resolves an authored destination against one physical identity and an explicit
// current face. Projectors can call this without mutating the live card.
internal static class AbilityFormDestinationResolver
{
    internal static void RequireStandalone(AbilityEffect effect)
    {
        if (effect is AbilityEffect.ChangeForm) return;
        if (effect is AbilityEffect.Sequence { Effects.Length: 2 } sequence
            && sequence.Effects[0] is AbilityEffect.ChangeForm
                { Player: AbilityPlayer.You, Destination: AbilityFormDestination.OtherHero }
            && (sequence.Effects[1] is AbilityEffect.Draw
                    { Players: AbilityPlayerSelection.OnePlayer { Player: AbilityPlayer.You } }
                || sequence.Effects[1] is AbilityEffect.CardAction
                    { Instruction: AbilityCardInstruction.Ready,
                      Selection: AbilityCardSelection.Bound { Binding: AbilityCardBinding.YourHero } })) return;
        if (ContainsExplicitDestination(effect))
            throw new RulesNotImplementedException(
                "projecting a composed exact or other-hero form change is not implemented");
    }

    private static bool ContainsExplicitDestination(AbilityEffect effect) =>
        effect is AbilityEffect.ChangeForm { Destination: not AbilityFormDestination.Form }
        || AbilityEffectStructure.AllEffectChildren(effect).Any(ContainsExplicitDestination);

    internal static string LegacyForm(AbilityFormDestination destination) =>
        destination is AbilityFormDestination.Form form ? form.Name
            : throw new RulesNotImplementedException(
                "projecting an exact or other-hero form destination is not implemented");

    internal static string? Resolve(
        Card identity, ICardFacts facts, string currentFace,
        IReadOnlySet<string> currentForms, AbilityFormDestination selection)
    {
        if (!identity.Faces.Contains(currentFace, StringComparer.Ordinal))
            throw new ArgumentException("current face must belong to the identity", nameof(currentFace));

        if (selection is AbilityFormDestination.Face face)
            return ResolveFace(identity, facts, currentFace, face.Id);

        string form = selection switch
        {
            AbilityFormDestination.Form named when named.Name is Forms.Hero or Forms.AlterEgo => named.Name,
            AbilityFormDestination.OtherHero => Forms.Hero,
            _ => throw new RulesNotImplementedException("unsupported form destination"),
        };
        if (selection is AbilityFormDestination.Form && currentForms.Contains(form)) return null;
        if (selection is AbilityFormDestination.OtherHero && Forms.OfFace(currentFace, facts) != Forms.Hero) return null;
        var destinations = identity.Faces.Where(face => face != currentFace
            && Forms.OfFace(face, facts) == form).ToArray();
        return destinations.Length switch
        {
            0 => null,
            1 => destinations[0],
            _ => throw new RulesNotImplementedException(
                $"'{currentFace}' does not have one unambiguous {form} destination"),
        };
    }
    private static string? ResolveFace(
        Card identity, ICardFacts facts, string currentFace, string destination)
    {
        if (!identity.Faces.Contains(destination, StringComparer.Ordinal)
            || Forms.OfFace(destination, facts) is null)
            throw new RulesNotImplementedException($"'{destination}' is not an identity face of '{currentFace}'");
        return destination == currentFace ? null : destination;
    }

}
