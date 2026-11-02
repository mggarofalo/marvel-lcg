using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Projected membership and attachment hosts for constant threat permissions.</summary>
internal sealed record AbilityThreatPermissionProjection(
    World World, IReadOnlySet<int> Departed, IReadOnlySet<int> Entered,
    IReadOnlyDictionary<int, int> Hosts)
{
    private Card? source;

    internal bool InPlay(Card card) => !Departed.Contains(card.ObjectId)
        && (Entered.Contains(card.ObjectId) || DeckTypes.IsInPlay(card.Area.Type));

    internal AbilityThreatPermissionProjection ForSource(Card card) => this with { source = card };

    internal Card? Find(AbilityCardSelection selection) => selection switch
    {
        AbilityCardSelection.Bound { Binding: AbilityCardBinding.This } => source,
        AbilityCardSelection.Bound { Binding: AbilityCardBinding.AttachedTo } => AttachedTo(),
        _ => throw new RulesNotImplementedException(
            "projected threat-removal prohibition requires a supported projected selection"),
    };

    /// <summary>Evaluates a permission's condition against projected membership.</summary>
    /// <remarks>
    /// rr:in-play-and-out-of-play.4: "Card abilities only interact with, and can only
    /// target, cards that are in play". A titled presence test such as Total Destruction's
    /// "while Abomination is in play" therefore reads projected membership: a card the
    /// power defeats is absent and a card it puts into play is present. Engine choice:
    /// titled presence tests and their combinations are the projected predicates; any
    /// other condition raises.
    /// </remarks>
    internal bool Test(AbilityCondition condition, AbilitySelectorEvaluation selectors) => condition switch
    {
        AbilityCondition.All all => all.Operands.All(operand => Test(operand, selectors)),
        AbilityCondition.Any any => any.Operands.Any(operand => Test(operand, selectors)),
        AbilityCondition.Negated negated => !Test(negated.Operand, selectors),
        AbilityCondition.Exists { Cards: AbilityCardSelection.Titled titled } => Titled(titled, selectors).Any(),
        _ => throw new RulesNotImplementedException(
            "projected conditional threat-removal permission requires projected predicate evaluation"),
    };

    private IEnumerable<Card> Titled(AbilityCardSelection.Titled titled, AbilitySelectorEvaluation selectors) =>
        selectors.Every(titled).Where(InPlay).Concat(Entered
            .Where(id => !Departed.Contains(id))
            .Select(id => World.Cards[id])
            .Where(card => EnteredShows(card, titled.Title)));

    // rr:reveal.step.1: "Turn the encounter card faceup." An entry still facedown in
    // the projection has no settled face to compare, so the test raises rather than
    // treat it as absent. A profiled card shows no printed title, as on the live board.
    private bool EnteredShows(Card card, string title)
    {
        if (!card.FaceUp)
            throw new RulesNotImplementedException(
                "a projected threat-removal presence test reads a card entering play facedown");
        return !EffectiveCards.HasProfile(card)
            && string.Equals(World.Facts.Title(card.FaceId), title, StringComparison.Ordinal);
    }

    private Card? AttachedTo()
    {
        int host = Hosts.GetValueOrDefault(source!.ObjectId, source.Area.Host);
        return host >= 0 && !Departed.Contains(host) ? World.Cards[host] : null;
    }

    internal static AbilityThreatPermissionProjection From(World world, AreaProjectionState state) =>
        new(world, state.Departed, state.Entered, state.Hosts);

    // A power-state projection starts every unengaged minion as unavailable and a
    // projected entry removes it from that set; such a minion outside play has entered.
    internal static AbilityThreatPermissionProjection From(World world, AbilityPowerState state) =>
        new(world, state.Discarded, world.Cards
            .Where(card => !DeckTypes.IsInPlay(card.Area.Type) && card.Area.Type != DeckType.EngagedEnemiesArea
                && EffectiveCards.Kind(card, world.Facts) == CardKind.Minion
                && !state.Discarded.Contains(card.ObjectId))
            .Select(card => card.ObjectId).ToHashSet(), new Dictionary<int, int>());
}
