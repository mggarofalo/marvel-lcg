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
    /// power defeats is absent and a card it puts into play is present. A titled card
    /// in the villain's play area, such as "Take What Is Mine"'s "while the Power Stone is
    /// attached to Ronan", reads projected membership and the projected host. Engine
    /// choice: these tests and their combinations are the projected predicates; any other
    /// condition raises.
    /// </remarks>
    internal bool Test(AbilityCondition condition, AbilitySelectorEvaluation selectors) => condition switch
    {
        AbilityCondition.All all => all.Operands.All(operand => Test(operand, selectors)),
        AbilityCondition.Any any => any.Operands.Any(operand => Test(operand, selectors)),
        AbilityCondition.Negated negated => !Test(negated.Operand, selectors),
        AbilityCondition.Exists { Cards: AbilityCardSelection.Titled titled } => Titled(titled, selectors).Any(),
        AbilityCondition.Exists { Cards: AbilityCardSelection.InAreas
        {
            Areas: [AbilitySearchArea.VillainPlayArea], Kind: null, Trait: null, Title: { } title,
            Top: null, AspectOnly: false,
        } } => InVillainPlayArea(title),
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
    // treat it as absent. An entering copy is outside play, so it carries no profile.
    private bool EnteredShows(Card card, string title)
    {
        if (!card.FaceUp)
            throw new RulesNotImplementedException(
                "a projected threat-removal presence test reads a card entering play facedown");
        return string.Equals(World.Facts.Title(card.FaceId), title, StringComparison.Ordinal);
    }

    // An entering card's destination is not part of the projection, so a titled entry raises.
    private bool InVillainPlayArea(string title)
    {
        if (Entered.Any(id => !Departed.Contains(id) && EnteredShows(World.Cards[id], title)))
            throw new RulesNotImplementedException(
                "a projected villain play area test reads a card entering play");
        return World.Cards.Any(card => InPlay(card)
            && string.Equals(World.Facts.Title(card.FaceId), title, StringComparison.Ordinal)
            && ProjectedPlayArea(card).IsVillains);
    }

    private PlayArea ProjectedPlayArea(Card card) =>
        Hosts.TryGetValue(card.ObjectId, out int host) ? World.Cards[host].Area.PlayArea : card.Area.PlayArea;

    private Card? AttachedTo()
    {
        int host = Hosts.GetValueOrDefault(source!.ObjectId, source.Area.Host);
        return host >= 0 && !Departed.Contains(host) ? World.Cards[host] : null;
    }

    internal static AbilityThreatPermissionProjection From(World world, AreaProjectionState state) =>
        new(world, state.Departed, state.Entered, state.Hosts);

    // A power-state projection starts every unengaged minion as unavailable and a
    // projected entry removes it from that set; such a minion outside play has entered.
    // A minion still unavailable stays absent because Departed is that same set.
    internal static AbilityThreatPermissionProjection From(World world, AbilityPowerState state) =>
        new(world, state.Discarded, world.Cards
            .Where(card => !DeckTypes.IsInPlay(card.Area.Type) && card.Area.Type != DeckType.EngagedEnemiesArea
                && EffectiveCards.Kind(card, world.Facts) == CardKind.Minion)
            .Select(card => card.ObjectId).ToHashSet(), new Dictionary<int, int>());
}
