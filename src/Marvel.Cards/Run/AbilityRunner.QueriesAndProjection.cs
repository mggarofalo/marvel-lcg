using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    /// <summary>Every card a value names, which may be none.</summary>
    /// <remarks>
    /// A value that names one card answers with that one, so a card reading
    /// "the villain attacks you" and one reading "each minion engaged with you
    /// attacks you" are the same node with a different argument.
    /// </remarks>
    private static IReadOnlyList<Card> Every(AbilityValue value, Cast cast)
    {
        if (value is AbilityValue.Map && Tree(value) is { Kind: "titled" } titled)
        {
            return ReferencedByTitle(Word(titled.Argument), cast);
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } query
            && query.Argument is AbilityValue.Word { Value: "minionsEngagedWithYou" })
        {
            // `rr:engage.1` -- "when a minion engages a player, it is placed in
            // that player's play area". Engagement *is* which area the minion
            // sits in, so this is a read of the board and not of a flag; and
            // "you" is the player resolving the card, so a minion engaged with
            // somebody else is not in this list however close it is on the
            // table.
            return [.. cast.World
                .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(cast.Player))
                .Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } eligibleIdentities
            && eligibleIdentities.Argument is AbilityValue.Word
                { Value: "identitiesWithinPerPlayerLimit" })
        {
            long maximum = cast.World.Facts.PrintedValue(
                cast.Source.FaceId, "MaxPerUnit", cast.World.Players);
            string title = cast.World.Facts.Title(cast.Source.FaceId);
            return
            [
                .. cast.World.PlayerOrder
                    .Where(player => maximum <= 0 || cast.World.Areas
                        .Where(area => area.PlayArea == PlayArea.Of(player))
                        .SelectMany(area => area.Cards)
                        .Count(card => DeckTypes.IsInPlay(card.Area.Type)
                            && string.Equals(
                                cast.World.Facts.Title(card.FaceId),
                                title,
                                StringComparison.Ordinal)) < maximum)
                    .Select(player => cast.World.Seats[player].IdentityCard),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } attached
            && attached.Argument is AbilityValue.Word { Value: "attachedToThis" })
        {
            // What is sitting on this card. `rr:attachment` puts an attachment
            // in an area hosted by the card it is attached to, so this is a
            // read of the board.
            return
            [
                .. cast.World.Areas
                    .Where(area => area.Host == cast.Source.ObjectId)
                    .SelectMany(area => area.Cards),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } friendly
            && friendly.Argument is AbilityValue.Word { Value: "heroesAndAllies" })
        {
            // `rr:indirect-damage.2`'s "friendly characters in play", which
            // `rr:friendly` makes every player's rather than one player's: "a
            // blanket term that refers to cards **the players** control".
            //
            // **Every identity, not only those in hero form.** "Heroes and
            // allies" is what the card says, but `rr:you-your.3` divides
            // indirect damage "among characters in play under their control",
            // and a player in alter-ego form is still a character with hit
            // points. A reading that skipped them would leave damage
            // unassignable at a table where everyone had flipped down.
            return
            [
                .. cast.World.PlayerOrder.Select(seat => cast.World.Seats[seat].IdentityCard),
                .. cast.World.Areas
                    .Where(area => area.Type == DeckType.AlliesArea)
                    .SelectMany(area => area.Cards),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } schemes
            && schemes.Argument is AbilityValue.Word { Value: "sideSchemes" })
        {
            // "Each side scheme", which reaches the players' as well as the
            // scenario's: `rr:player-side-scheme` calls them "the player card
            // equivalent of the side schemes found in the encounter deck" and
            // `.1` puts them in the same place, next to the main scheme.
            return [.. cast.World.AreaOf(DeckType.SideSchemesArea).Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } minions
            && minions.Argument is AbilityValue.Word { Value: "minions" })
        {
            // `rr:minion.3`: minions in play are engaged with players, so the
            // engaged-enemy areas across every play area are the complete set.
            return
            [
                .. cast.World.Areas
                    .Where(area => area.Type == DeckType.EngagedEnemiesArea)
                    .SelectMany(area => area.Cards)
                    .Where(card => cast.World.Facts.Kind(card.FaceId) == CardKind.Minion),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } enemies
            && enemies.Argument is AbilityValue.Word { Value: "enemies" })
        {
            return
            [
                .. cast.World.Areas
                    .Where(area => area.Type is DeckType.VillainArea
                        or DeckType.EngagedEnemiesArea)
                    .SelectMany(area => area.Cards)
                    .Where(card => cast.World.Facts.Kind(card.FaceId) is
                        CardKind.EncounterVillain or CardKind.Minion),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } attackable
            && attackable.Argument is AbilityValue.Word { Value: "attackableEnemies" })
        {
            return
            [
                .. BasicPowers.Attackable(cast.World, cast.World.Facts, Resolver(cast))
                    .Where(enemy => cast.World.Abilities.CanTakeDamage(
                        cast.World, enemy, cast.Source)),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } attackableMinions
            && attackableMinions.Argument is AbilityValue.Word { Value: "attackableMinions" })
        {
            return
            [
                .. BasicPowers.Attackable(cast.World, cast.World.Facts, Resolver(cast))
                    .Where(enemy => cast.World.Facts.Kind(enemy.FaceId) == CardKind.Minion)
                    .Where(enemy => cast.World.Abilities.CanTakeDamage(
                        cast.World, enemy, cast.Source)),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } allSchemes
            && allSchemes.Argument is AbilityValue.Word { Value: "schemes" })
        {
            return
            [
                .. cast.World.AreaOf(DeckType.MainSchemesArea).Cards,
                .. cast.World.AreaOf(DeckType.SideSchemesArea).Cards,
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } thwartable
            && thwartable.Argument is AbilityValue.Word { Value: "thwartableSchemes" })
        {
            return BasicPowers.Thwartable(cast.World, cast.World.Facts, Resolver(cast));
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } powerTargets
            && powerTargets.Argument is AbilityValue.Word { Value: "powerTargets" })
        {
            return cast.PowerTargets;
        }

        if (value is AbilityValue.Map
            && Tree(value) is { Kind: "withoutAnotherCopyAttached" } unoccupied)
        {
            string title = cast.World.Facts.Title(cast.Source.FaceId);
            return
            [
                .. Every(unoccupied.Argument, cast).Where(candidate =>
                    !cast.World.Areas
                        .Where(area => area.Host == candidate.ObjectId)
                        .SelectMany(area => area.Cards)
                        .Any(attached => attached.ObjectId != cast.Source.ObjectId
                            && string.Equals(
                                cast.World.Facts.Title(attached.FaceId), title,
                                StringComparison.Ordinal))),
            ];
        }

        if (value is AbilityValue.Map
            && Tree(value) is { Kind: "discardable" } discardable)
        {
            return
            [
                .. Every(discardable.Argument, cast).Where(card =>
                    CanRemoveByEffect(discardable.Argument, cast, card)),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } pile
            && pile.Argument is AbilityValue.Word { Value: "yourAsidePile" })
        {
            // "The rest of your set-aside nemesis encounter set" -- whatever is
            // still in the pile once the cards this ability took out of it have
            // gone. The obligation is not among them: setup shuffles it into
            // the encounter deck long before this resolves.
            return [.. cast.World.Seats[cast.Player].Nemesis.Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } yours
            && yours.Argument is AbilityValue.Word { Value: "upgradesAndSupportsYouControl" })
        {
            // "An upgrade or support **you control**." A player's upgrades and
            // supports sit in their own play area, so control is where the card
            // is -- the same reading `rr:engage.1` gets for a minion.
            return
            [
                .. Owned.SelectMany(where =>
                    cast.World.AreaOf(where, PlayArea.Of(cast.Player)).Cards),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "minBy" or "maxBy" } ranked)
        {
            return Ranked(ranked, cast);
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "cardsIn" } search)
        {
            return CardsIn(search, cast);
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "enemiesWithTrait" } trait)
        {
            // "Each **[[Criminal]]** enemy in play." A query with an argument
            // rather than one of the bare words, the way `titled` is -- the
            // trait is the whole of what varies, and dozens of cards in the
            // pool print this shape with a different one.
            //
            // **Spelled as the engine spells it** -- `CRIMINAL`, upper case,
            // spaces underscored -- for the reason `AbilityTrigger.Event` gives
            // for conditions: a translation table between the printed trait and
            // the stored one is a second vocabulary, and a second vocabulary
            // drifts. `ICardFacts.Traits` answers in that spelling.
            //
            // `rr:enemy`: "an enemy is a minion or villain", so this is the
            // villain's own area and every player's engaged minions --
            // `rr:minion.3` is why engagement is which play area a minion sits
            // in. Every player's, not the resolving one's: the card says "in
            // play" and says nothing about whose.
            string wanted = Word(trait.Argument);
            return
            [
                .. cast.World.Areas
                    .Where(area => area.Type is DeckType.VillainArea
                        or DeckType.EngagedEnemiesArea)
                    .SelectMany(area => area.Cards)
                    .Where(card => Rules.State.Traits.Of(cast.World, card, cast.World.Facts)
                        .Contains(wanted, StringComparer.Ordinal)),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } hand
            && hand.Argument is AbilityValue.Word { Value: "identitySpecificInYourHand" })
        {
            // "1 identity-specific card from your hand."
            // `rr:identity-specific-card` calls it a classification -- "cards
            // that belong to an identity's set of accompanying cards" -- and
            // `.3` says it is "designated by the identity icon printed in the
            // bottom right corner of the card". The extract records that corner
            // as the `Class` attribute, where an aspect card carries its aspect
            // and an identity-specific one carries `Hero`.
            //
            // A contains rather than an equals: `rr:classifications` lets a
            // card hold more than one, and three cards in the pool are printed
            // both identity-specific and aspect.
            return
            [
                .. cast.World.Seats[cast.Player].Hand.Cards
                    .Where(card => cast.World.Facts
                        .Attributes(card.FaceId)
                        .GetValueOrDefault("Class", string.Empty)
                        .Split(';')
                        .Contains("Hero", StringComparer.Ordinal)),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } supports
            && supports.Argument is AbilityValue.Word { Value: "supportsYouControl" })
        {
            // The support half of `upgradesAndSupportsYouControl`, on its own,
            // because Speed Demon's boost says "support" and an upgrade is not
            // one. `rr:play-area.1` again for what "you control" reads as.
            return [.. cast.World.AreaOf(DeckType.SupportsArea, PlayArea.Of(cast.Player)).Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } characters
            && characters.Argument is AbilityValue.Word { Value: "charactersYouControl" })
        {
            // "The character you control with the highest ATK value." Every
            // character, not only those in hero form: `rr:you-your.10` reads
            // "you control" as the cards in that player's play area, and an
            // alter-ego is a character with a hit point dial. An alter-ego
            // prints no ATK, and `rr:dash-value.3` makes that "an unmodifiable
            // 0" rather than a card that cannot be compared.
            return
            [
                cast.World.Seats[cast.Player].IdentityCard,
                .. cast.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(cast.Player)).Cards,
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } upgrades
            && upgrades.Argument is AbilityValue.Word { Value: "upgradesYouControl" })
        {
            // The upgrade half of `upgradesAndSupportsYouControl`, on its own,
            // because Beetle's two abilities both say "upgrade" and a support
            // is not one. Same reading of control: `rr:play-area.1` puts "any
            // cards in play under their control" in a player's own play area.
            return [.. cast.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(cast.Player)).Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } panther
            && panther.Argument is AbilityValue.Word { Value: "blackPantherUpgrades" })
        {
            return
            [
                .. cast.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(cast.Player)).Cards
                    .Where(card => Rules.State.Traits.Has(
                        cast.World, card, "BLACK_PANTHER", cast.World.Facts)),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } engaged
            && engaged.Argument is AbilityValue.Word { Value: "enemiesEngagedWithChosenPlayer" })
        {
            int player = ChosenPlayer(cast).Owner;
            return
            [
                .. cast.World.AreaOf(
                    DeckType.EngagedEnemiesArea, PlayArea.Of(player)).Cards,
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } allies
            && allies.Argument is AbilityValue.Word { Value: "alliesYouControl" })
        {
            // "Each ally **you control**", which is where the card is:
            // `rr:play-area.1` puts "any cards in play under their control" in
            // a player's own play area, so control is a read of the board
            // rather than a field -- the same reading `rr:engage.1` gets for a
            // minion. Not `heroesAndAllies`, which is every player's: Boomerang
            // hits the allies of the player it attacked and nobody else's.
            return [.. cast.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(cast.Player)).Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } heroes
            && heroes.Argument is AbilityValue.Word { Value: "heroes" })
        {
            // **Not every identity.** `rr:form-change-form.5`: "while a player
            // is in alter-ego form, card abilities that interact with their
            // hero do not interact with their identity." So "each hero" passes
            // over a player who has flipped down, and Shocker's one damage is
            // one damage to whoever is standing up.
            return [.. cast.World.PlayerOrder
                .Select(seat => cast.World.Seats[seat])
                .Where(seat => Forms.In(cast.World, seat, cast.World.Facts, Forms.Hero))
                .Select(seat => seat.IdentityCard)];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } identities
            && identities.Argument is AbilityValue.Word { Value: "identities" })
        {
            return [.. cast.World.PlayerOrder.Select(player =>
                cast.World.Seats[player].IdentityCard)];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } eligiblePlayers
            && eligiblePlayers.Argument is AbilityValue.Word
                { Value: "identitiesWithTechInDiscard" })
        {
            return
            [
                .. cast.World.PlayerOrder
                    .Where(player => cast.World.AreaOf(
                            DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player)
                        .Cards.Any(card => Rules.State.Traits.Has(
                            cast.World, card, "TECH", cast.World.Facts)))
                    .Select(player => cast.World.Seats[player].IdentityCard),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } topmost
            && topmost.Argument is AbilityValue.Word
                { Value: "topmostTechInChosenDiscard" })
        {
            int player = ChosenPlayer(cast).Owner;
            var card = cast.World.AreaOf(
                    DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player)
                .Cards.LastOrDefault(candidate => Rules.State.Traits.Has(
                    cast.World, candidate, "TECH", cast.World.Facts));
            return card is null ? [] : [card];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } allCharacters
            && allCharacters.Argument is AbilityValue.Word { Value: "characters" })
        {
            return
            [
                .. cast.World.PlayerOrder.Select(player =>
                    cast.World.Seats[player].IdentityCard),
                .. cast.World.Areas
                    .Where(area => area.Type is DeckType.AlliesArea
                        or DeckType.VillainArea or DeckType.EngagedEnemiesArea)
                    .SelectMany(area => area.Cards),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } drones
            && drones.Argument is AbilityValue.Word { Value: "drones" })
        {
            return FacedownDrones.InPlay(cast.World);
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } engagedDrones
            && engagedDrones.Argument is AbilityValue.Word { Value: "dronesEngagedWithYou" })
        {
            return FacedownDrones.EngagedWith(cast.World, Resolver(cast));
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "withTrait" } withTrait)
        {
            string wanted = Word(withTrait.Require("trait"));
            return [.. Every(withTrait.Require("cards"), cast).Where(card =>
                Rules.State.Traits.Has(cast.World, card, wanted, cast.World.Facts))];
        }

        return Find(value, cast) is { } one ? [one] : [];
    }

    /// <summary>The top cards of a deck, in top-to-bottom order.</summary>
    private static IReadOnlyList<Card> TopCards(Area deck, int count) =>
        [.. deck.Cards.TakeLast(count).Reverse()];

    /// <summary>
    /// The cards in named areas that match a search's criteria — <c>rr:search</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Search the encounter deck for a <b>[[Criminal]] minion</b>." Three
    /// named facets: which area, which card type, which trait. The Doomsday
    /// Chair adds the other printed shape in the core set: two areas and one
    /// card named by title.
    /// <c>docs/card-dsl.md</c> is explicit that selection must be "a fixed
    /// vocabulary of relations, <b>not</b> as a general 'run this predicate'
    /// hook" — so this grows a facet when a card prints one, and never a
    /// filter expression.
    /// </para>
    /// <para>
    /// <b>Nothing leaves the area here.</b> <c>rr:search.2</c>: "cards being
    /// searched are not considered to leave the searched area." This answers
    /// which cards a player may pick; the picking is a <c>chooseCard</c>, which
    /// is where <c>rr:search.1</c> puts the choice — "if a player finds
    /// multiple cards that satisfy the criteria of a search, the player chooses
    /// among those options."
    /// </para>
    /// <para>
    /// <b>The shuffle is not here either.</b> <c>rr:search.3</c> shuffles "upon
    /// completion of that game step, game function, or card ability", which is
    /// after the choice has been answered — so the card carries it as a step of
    /// its own, in both branches, because the deck was searched whether or not
    /// anything was found.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<Card> CardsIn(AbilityNode node, Cast cast)
    {
        var areas = node.Field("areas") is AbilityValue.List several
            ? several.Values.Select(named => Area(Word(named), cast)).ToList()
            : [Area(Word(node.Require("area")), cast)];
        string? kind = node.Field("kind") is { } named ? Word(named) : null;
        string? trait = node.Field("trait") is { } carried ? Word(carried) : null;
        string? title = node.Field("title") is { } titled ? Word(titled) : null;

        return
        [
            .. areas.SelectMany(area => area.Cards)
                .Where(card => kind is null || string.Equals(
                    cast.World.Facts.Kind(card.FaceId).ToString(), kind, StringComparison.Ordinal))
                .Where(card => trait is null
                    || Rules.State.Traits.Has(cast.World, card, trait, cast.World.Facts))
                .Where(card => title is null || string.Equals(
                    cast.World.Facts.Title(card.FaceId), title, StringComparison.Ordinal)),
        ];
    }

    /// <summary>
    /// "The lowest-cost upgrade you control" — <c>minBy</c> and <c>maxBy</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ties are kept.</b> The Rules Reference gives no tie-break for "the
    /// lowest-cost X", and collapsing one here would be the interpreter
    /// deciding something the rules leave to the table. So this answers with
    /// every card that shares the extreme value, and the card that wants one
    /// wraps it in a <c>chooseCard</c> — which is where
    /// <c>rr:choose-game-element.1</c> puts the question, to the player
    /// resolving.
    /// </para>
    /// <para>
    /// <b>Permanents are not among the candidates.</b>
    /// <c>rr:permanent.4.1</c> names this exact shape: "if a permanent card
    /// would be targeted by such an effect <i>(for example, 'discard the
    /// lowest-cost support you control')</i>, that effect instead targets the
    /// <b>non-permanent</b> card that fits its criteria." So a permanent is
    /// dropped before the comparison rather than after it, or a cheap
    /// permanent would shield a dearer card that the effect should have taken.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<Card> Ranked(AbilityNode node, Cast cast)
    {
        // Through `StateFields` rather than straight at the printed field:
        // `rr:permanent.1` makes the keyword "equivalent to the following
        // constant ability", and a constant ability is something a card can
        // grant. Reading print alone would miss a permanence handed out in
        // play.
        var among = Every(node.Require("of"), cast)
            .Where(card => CanRemoveByEffect(node.Require("of"), cast, card))
            .ToList();

        if (among.Count == 0)
        {
            return [];
        }

        string key = Word(node.Require("by"));
        long Rank(Card card) => key switch
        {
            // `rr:dash-value.3` -- a printed dash "is treated as an
            // unmodifiable 0", which is what `PrintedValue` answers for a field
            // that is not a number, so nothing extra is needed for it here.
            "cost" => cast.World.Facts.PrintedValue(card.FaceId, "Cost", cast.World.Players),
            "attack" => StateFields.Modified(
                cast.World, card, "attack", cast.World.Facts, cast.World.Players),
            "printedHealth" => cast.World.Facts.PrintedValue(
                card.FaceId, "HP", cast.World.Players),
            _ => throw new AbilityException($"'{key}' is not a value cards can be ranked by"),
        };

        long extreme = node.Kind == "minBy" ? among.Min(Rank) : among.Max(Rank);
        return [.. among.Where(card => Rank(card) == extreme)];
    }

    /// <summary>Which card a value names, or null when it names none.</summary>
    private static Card? Find(AbilityValue value, Cast cast) => value switch
    {
        AbilityValue.Word word => Named(word.Value, cast),
        AbilityValue.Map => Find(Tree(value), cast),
        _ => throw new AbilityException($"{AbilityNode.Describe(value)} does not name a card"),
    };

    /// <summary>Which one card a query names, refusing a player choice.</summary>
    private static Card? Find(AbilityNode node, Cast cast)
    {
        if (node.Kind != "cardsIn")
        {
            return Query(node, cast);
        }

        if (!SingularAreaQueryIsStable(node, cast))
        {
            return null;
        }
        var found = CardsIn(node, cast);
        return found.Count switch
        {
            0 => null,
            1 => found[0],
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' searched and found {found.Count} matching cards; "
                + "rr:search.1 gives the player that choice and asking is not implemented"),
        };
    }

    private static bool SingularAreaQueryIsStable(
        AbilityNode query, Cast cast)
    {
        if (!cast.CheckingInitiation)
        {
            return true;
        }

        var areas = CardsInAreaTypes(query, cast);
        bool priorCanChange = EffectsMayChangeAnyArea(
            cast.PriorSteps, areas, cast);
        bool paymentCanChange = cast.PaymentCost is { } cost
            && MayChangeAnyArea(cost, areas, cast);
        if (priorCanChange || paymentCanChange)
        {
            if (cast.FilteringContinuationOption)
            {
                return false;
            }
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a singular area query after its "
                + "matching cards may change"
                + (cast.PriorSteps.Count > 0
                    ? " during prior effects"
                    : " during payment"));
        }
        return true;
    }

    private static HashSet<DeckType> CardsInAreaTypes(
        AbilityNode query, Cast cast)
    {
        var names = query.Field("areas") is AbilityValue.List several
            ? several.Values.Select(Word)
            : [Word(query.Require("area"))];
        return names.Select(name => Area(name, cast).Type).ToHashSet();
    }

    private static bool MayChangeAnyArea(
        AbilityNode effect, IReadOnlySet<DeckType> queried, Cast cast,
        long multiplier = 1)
    {
        bool Includes(params DeckType[] areas) => areas.Any(queried.Contains);

        bool SelectedCardMovesToDiscard(AbilityValue selector) =>
            Every(selector, cast).Any(card =>
                queried.Contains(card.Area.Type)
                || card.Owner < 0
                    && queried.Contains(DeckType.EncounterDiscardPile)
                || card.Owner >= 0 && queried.Contains(DeckType.DiscardPile));

        bool SelectedCardMoves(AbilityValue selector, params DeckType[] destinations) =>
            Every(selector, cast).Any(card => queried.Contains(card.Area.Type))
            || Includes(destinations);

        bool DamageCouldDiscard(AbilityNode damage)
        {
            long amount = SaturatingSum(
                SaturatingMultiply(
                    Amount(damage.Require("amount"), cast), multiplier),
                [EventModifier(cast, "eventDamage")]);
            if (cast.Power == BasicPowers.AttackVerb)
            {
                amount = SaturatingSum(
                    amount, [EventModifier(cast, "attackDamage")]);
            }
            return DamageTargets(damage.Require("cards"), cast).Any(card =>
                cast.Abilities.CanTakeDamage(cast.World, card, cast.Source)
                && Statuses.Count(cast.World, card, Statuses.Tough) == 0
                && Damage.Health(cast.World, cast.World.Facts, card) - card.Damage <= amount
                && (cast.World.Facts.Kind(card.FaceId) == CardKind.Minion
                        && queried.Contains(DeckType.EncounterDiscardPile)
                    || cast.World.Facts.Kind(card.FaceId) == CardKind.Ally
                        && queried.Contains(DeckType.DiscardPile)));
        }

        bool ThreatRemovalCouldDiscard(AbilityNode removal)
        {
            if (!queried.Contains(DeckType.EncounterDiscardPile))
            {
                return false;
            }
            long amount = SaturatingSum(
                SaturatingMultiply(
                    Amount(removal.Require("amount"), cast), multiplier),
                [EventModifier(cast, "eventThreatRemoval")]);
            return Every(removal.Require("scheme"), cast).Any(scheme =>
                cast.World.Facts.Kind(scheme.FaceId) == CardKind.EncounterSideScheme
                && scheme.Tokens.GetValueOrDefault("k_threat") <= amount
                && DefeatTreeChangesArea(scheme, queried, cast));
        }

        bool MovedDamageCouldDiscard(AbilityNode movement)
        {
            var from = Find(movement.Require("from"), cast);
            var to = Find(movement.Require("to"), cast);
            if (from is null || to is null)
            {
                return false;
            }
            long amount = Math.Min(
                from.Damage,
                SaturatingMultiply(
                    Amount(movement.Require("amount"), cast), multiplier));
            return amount > 0
                && cast.Abilities.CanTakeDamage(cast.World, to, cast.Source)
                && Statuses.Count(cast.World, to, Statuses.Tough) == 0
                && Damage.Health(cast.World, cast.World.Facts, to) - to.Damage <= amount
                && (cast.World.Facts.Kind(to.FaceId) == CardKind.Minion
                        && queried.Contains(DeckType.EncounterDiscardPile)
                    || cast.World.Facts.Kind(to.FaceId) == CardKind.Ally
                        && queried.Contains(DeckType.DiscardPile));
        }

        bool direct = effect.Kind switch
        {
            "draw" or "drawToHandSize" or "drawToPrintedHandSize" =>
                Includes(DeckType.PlayerDeck, DeckType.HandsArea),
            "discard" => SelectedCardMovesToDiscard(
                effect.Field("card") ?? effect.Argument),
            "removeFromGame" => SelectedCardMoves(
                effect.Argument, DeckType.RemovedArea),
            "returnToHand" => SelectedCardMoves(
                effect.Argument, DeckType.HandsArea),
            "reveal" => SelectedCardMoves(
                effect.Argument, DeckType.RevealingArea),
            "putIntoPlay" => SelectedCardMoves(
                effect.Require("card"),
                DeckType.AlliesArea, DeckType.SupportsArea, DeckType.UpgradesArea,
                DeckType.EngagedEnemiesArea, DeckType.SideSchemesArea),
            "search" => SearchAreaTypes(effect, cast).Any(queried.Contains)
                || queried.Contains(DeckType.RevealingArea),
            "shuffleInto" => Includes(
                DeckType.EncounterDeck, DeckType.EncounterDiscardPile,
                DeckType.AsideDeck),
            "dealEncounterCard" or "dealEncounterCards" or "revealTop"
                or "discardTop" or "discardUntil" or "createDrones" =>
                Includes(
                    DeckType.EncounterDeck, DeckType.EncounterDiscardPile,
                    DeckType.RevealingArea, DeckType.DealtEncounterCardsDeck),
            "dealDamage" or "dealAttackDamage" => DamageCouldDiscard(effect),
            "moveDamage" or "moveAttackDamage" =>
                MovedDamageCouldDiscard(effect),
            "removeThreat" => ThreatRemovalCouldDiscard(effect),
            "indirectDamage" => queried.Contains(DeckType.DiscardPile),
            "discardFromHand" or "discardUpToFromHand" or "discardAnyFromHand"
                or "spend" or "spendPrinted" or "spendEnergyX" =>
                Includes(DeckType.HandsArea, DeckType.DiscardPile),
            _ => false,
        };
        if (direct)
        {
            return true;
        }

        if (effect.Kind == "forEach")
        {
            if (CurrentlyZeroForEach(effect, cast))
            {
                return false;
            }
            long count = ForEachCount(effect, cast);
            return MayChangeAnyArea(
                Tree(effect.Require("effect")), queried, cast,
                SaturatingMultiply(multiplier, count));
        }
        if (effect.Kind == "eachPlayer")
        {
            int priorPlayer = cast.Player;
            try
            {
                return cast.World.PlayerOrder.Any(player =>
                {
                    cast.RestorePlayer(player);
                    return MayChangeAnyArea(
                        Tree(effect.Require("effect")), queried, cast, multiplier);
                });
            }
            finally
            {
                cast.RestorePlayer(priorPlayer);
            }
        }
        if (effect.Kind is "seq" or "and")
        {
            return EffectsMayChangeAnyArea(
                Nodes(effect.Argument).ToList(), queried, cast, multiplier);
        }

        IEnumerable<AbilityNode> reachable = effect.Kind switch
        {
            // A choice has not run during the enclosing sequence preflight.
            // Its selected option/effect is added to PriorSteps when that
            // answer is validated against the saved continuation.
            "choose" or "chooseCard" => [],
            "if" => ReachableMutationBranches(effect, cast),
            "defense" or "delayUntil"
                or "attack" or "thwart" => [Tree(effect.Require("effect"))],
            _ => StructuralChildren(effect),
        };
        return reachable.Any(child =>
            MayChangeAnyArea(child, queried, cast, multiplier));
    }

    private static bool EffectsMayChangeAnyArea(
        IReadOnlyList<AbilityNode> effects, IReadOnlySet<DeckType> queried,
        Cast cast, long baseMultiplier = 1)
    {
        bool couldDiscard = false;

        bool DiscardedByDamage(AreaProjectionState state, Card target) =>
            state.DamageOf(target) >= state.HealthOf(cast, target)
            && DefeatTreeChangesArea(target, queried, cast);

        bool RootLeavesOnDefeat(Card root)
        {
            var kind = cast.World.Facts.Kind(root.FaceId);
            if (kind is CardKind.Minion or CardKind.Ally
                or CardKind.EncounterSideScheme)
            {
                return true;
            }
            if (kind != CardKind.EncounterVillain)
            {
                return false;
            }
            var villainDeck = cast.World.AreaOf(DeckType.VillainDeck).Cards;
            var next = villainDeck.Count > 0 ? villainDeck[^1] : null;
            return next is null || !string.Equals(
                cast.World.Facts.Title(root.FaceId),
                cast.World.Facts.Title(next.FaceId),
                StringComparison.Ordinal);
        }

        void MarkDiscardedTree(AreaProjectionState state, Card root)
        {
            var pending = new Stack<Card>();
            pending.Push(root);
            while (pending.TryPop(out var card))
            {
                if (!state.Departed.Add(card.ObjectId))
                {
                    continue;
                }
                foreach (var child in ProjectedHostedCards(
                             state, card.ObjectId).ToList())
                {
                    pending.Push(child);
                }
            }
        }

        IEnumerable<Card> ProjectedHostedCards(
            AreaProjectionState state, int host) => cast.World.Cards
            .Where(card => !state.Departed.Contains(card.ObjectId))
            .Where(card => state.Hosts.TryGetValue(card.ObjectId, out int projected)
                ? projected == host
                : card.Area.Host == host);

        void MarkHostedCardsDiscarded(AreaProjectionState state, int host)
        {
            foreach (var child in ProjectedHostedCards(state, host).ToList())
            {
                MarkDiscardedTree(state, child);
            }
        }

        bool HostedCardsChangeArea(AreaProjectionState state, int host) =>
            ProjectedHostedCards(state, host)
            .Any(card => DiscardTreeChangesArea(card));

        bool DefeatedHostsCardsChangeArea(
            AreaProjectionState state, int host) =>
            ProjectedHostedCards(state, host).Any(card =>
            {
                bool movesToVictory = cast.World.Facts.Kind(card.FaceId) is
                        CardKind.Attachment or CardKind.Upgrade
                    && Keywords.Has(
                        cast.World, card, "victory", cast.World.Facts);
                return movesToVictory
                    ? ProjectedHostedCards(state, card.ObjectId)
                        .Any(child => DiscardTreeChangesArea(child))
                    : DiscardTreeChangesArea(card);
            });

        Card? NextVillainStage(AreaProjectionState state) =>
            cast.World.AreaOf(DeckType.VillainDeck).Cards
                .LastOrDefault(card => !state.Entered.Contains(card.ObjectId));

        bool DiscardTreeChangesArea(Card root)
        {
            var pending = new Stack<Card>();
            var seen = new HashSet<int>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var card = pending.Pop();
                if (!seen.Add(card.ObjectId))
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' reaches a hosted-card cycle while "
                        + "projecting a discard");
                }
                var destination = card.Owner < 0
                    ? DeckType.EncounterDiscardPile : DeckType.DiscardPile;
                if (queried.Contains(destination))
                {
                    return true;
                }
                foreach (var child in cast.World.Areas
                             .Where(area => area.Host == card.ObjectId)
                             .SelectMany(area => area.Cards))
                {
                    pending.Push(child);
                }
            }
            return false;
        }

        void DealProjected(
            AreaProjectionState state, Card target, long amount,
            long repetitions)
        {
            if (cast.World.Facts.Kind(target.FaceId) == CardKind.EncounterVillain
                && state.ActiveVillain >= 0)
            {
                target = cast.World.Cards[state.ActiveVillain];
            }
            if (amount <= 0 || repetitions <= 0
                || state.Departed.Contains(target.ObjectId)
                || !cast.Abilities.CanTakeDamage(
                    cast.World, target, cast.Source))
            {
                return;
            }
            long prevented = Math.Min(state.ToughOf(cast, target), repetitions);
            state.Tough[target.ObjectId] =
                state.ToughOf(cast, target) - prevented;
            long dealt = SaturatingMultiply(amount, repetitions - prevented);
            state.Damage[target.ObjectId] = SaturatingSum(
                state.DamageOf(target), [dealt]);
            couldDiscard |= DiscardedByDamage(state, target);
            if (state.DamageOf(target) < state.HealthOf(cast, target))
            {
                return;
            }

            if (cast.World.Facts.Kind(target.FaceId) == CardKind.EncounterVillain)
            {
                var next = NextVillainStage(state);
                bool carries = next is not null && string.Equals(
                    cast.World.Facts.Title(target.FaceId),
                    cast.World.Facts.Title(next.FaceId),
                    StringComparison.Ordinal);
                int attachmentHost = state.VillainAttachmentHost >= 0
                    ? state.VillainAttachmentHost : target.ObjectId;
                if (!carries)
                {
                    couldDiscard |= HostedCardsChangeArea(state, attachmentHost);
                    MarkHostedCardsDiscarded(state, attachmentHost);
                }
                state.Departed.Add(target.ObjectId);
                state.ActiveVillain = next?.ObjectId ?? -1;
                if (next is not null)
                {
                    state.Entered.Add(next.ObjectId);
                    state.Damage[next.ObjectId] = 0;
                    if (StateFields.Modified(
                            cast.World, next, "toughness",
                            cast.World.Facts, cast.World.Players) > 0)
                    {
                        state.Tough[next.ObjectId] = Math.Max(
                            1, state.ToughOf(cast, next));
                    }
                    if (carries)
                    {
                        foreach (string status in new[]
                                 {
                                     Statuses.Tough,
                                     Statuses.Stunned,
                                     Statuses.Confused,
                                 })
                        {
                            long carried = state.StatusOf(cast, target, status);
                            if (status == Statuses.Tough)
                            {
                                state.Tough[next.ObjectId] = Math.Max(
                                    state.ToughOf(cast, next), carried);
                            }
                            else
                            {
                                state.Status[(next.ObjectId, status)] = carried;
                            }
                        }
                        foreach (var attachment in ProjectedHostedCards(
                                     state, attachmentHost).ToList())
                        {
                            state.Hosts[attachment.ObjectId] = next.ObjectId;
                        }
                        state.VillainAttachmentHost = next.ObjectId;
                    }
                }
                return;
            }

            if (RootLeavesOnDefeat(target))
            {
                couldDiscard |= DefeatedHostsCardsChangeArea(
                    state, target.ObjectId);
                MarkDiscardedTree(state, target);
            }
        }

        List<AreaProjectionState> TraceSequence(
            IEnumerable<AbilityNode> sequence,
            List<AreaProjectionState> states, long multiplier = 1)
        {
            foreach (var step in sequence)
            {
                states = Trace(step, states, baseMultiplier: multiplier);
            }
            return states;
        }

        List<AreaProjectionState> Trace(
            AbilityNode effect, List<AreaProjectionState> states,
            long repetitions = 1, long baseMultiplier = 1)
        {
            if (repetitions <= 0 || states.Count == 0)
            {
                return states;
            }

            if (effect.Kind is "dealDamage" or "dealAttackDamage")
            {
                long amount = SaturatingSum(
                    SaturatingMultiply(
                        Amount(effect.Require("amount"), cast), baseMultiplier),
                    [EventModifier(cast, "eventDamage"),
                     effect.Kind == "dealAttackDamage"
                         || cast.Power == BasicPowers.AttackVerb
                            ? EventModifier(cast, "attackDamage")
                            : 0]);
                foreach (var state in states)
                {
                    foreach (var target in ProjectedEvery(
                                 effect.Require("cards"), state, cast))
                    {
                        DealProjected(state, target, amount, repetitions);
                    }
                }
                return states;
            }

            if (effect.Kind is "moveDamage" or "moveAttackDamage")
            {
                long requested = SaturatingMultiply(
                    Amount(effect.Require("amount"), cast), baseMultiplier);
                foreach (var state in states)
                {
                    var from = ProjectedFind(effect.Require("from"), state, cast);
                    var to = ProjectedFind(effect.Require("to"), state, cast);
                    if (from is null || to is null
                        || !cast.Abilities.CanTakeDamage(
                            cast.World, to, cast.Source))
                    {
                        continue;
                    }
                    for (long repeat = 0;
                         repeat < repetitions && state.DamageOf(from) > 0;
                         repeat++)
                    {
                        long moved = Math.Min(state.DamageOf(from), requested);
                        state.Damage[from.ObjectId] = state.DamageOf(from) - moved;
                        DealProjected(state, to, moved, 1);
                    }
                }
                return states;
            }

            if (effect.Kind == "heal")
            {
                foreach (var state in states)
                {
                    var healed = ProjectedFind(effect.Require("card"), state, cast);
                    if (healed is null)
                    {
                        continue;
                    }
                    long amount = SaturatingMultiply(
                        SaturatingMultiply(
                            Amount(effect.Require("amount"), cast), baseMultiplier),
                        repetitions);
                    state.Damage[healed.ObjectId] = Math.Max(
                        0, state.DamageOf(healed) - amount);
                }
                return states;
            }

            if (effect.Kind == "removeThreat")
            {
                long amount = SaturatingSum(
                    SaturatingMultiply(
                        Amount(effect.Require("amount"), cast), baseMultiplier),
                    [EventModifier(cast, "eventThreatRemoval")]);
                foreach (var state in states)
                {
                    foreach (var scheme in ProjectedEvery(
                                 effect.Require("scheme"), state, cast))
                    {
                        long removed = SaturatingMultiply(amount, repetitions);
                        state.Threat[scheme.ObjectId] = Math.Max(
                            0, state.ThreatOf(scheme) - removed);
                        couldDiscard |= state.ThreatOf(scheme) == 0
                            && cast.World.Facts.Kind(scheme.FaceId)
                                == CardKind.EncounterSideScheme
                            && DefeatTreeChangesArea(scheme, queried, cast);
                        if (state.ThreatOf(scheme) == 0
                            && cast.World.Facts.Kind(scheme.FaceId)
                                == CardKind.EncounterSideScheme)
                        {
                            MarkDiscardedTree(state, scheme);
                        }
                    }
                }
                return states;
            }

            if (effect.Kind == "forEach")
            {
                if (CurrentlyZeroForEach(effect, cast))
                {
                    return states;
                }
                var repeated = Tree(effect.Require("effect"));
                long count = ForEachCount(effect, cast);
                return repeated.Kind is "dealDamage" or "dealAttackDamage"
                    or "removeThreat"
                    ? Trace(
                        repeated, states, repetitions,
                        SaturatingMultiply(baseMultiplier, count))
                    : Trace(
                        repeated, states,
                        SaturatingMultiply(repetitions, count), baseMultiplier);
            }

            if (effect.Kind == "giveStatus")
            {
                string status = Word(effect.Require("status"));
                foreach (var state in states)
                {
                    foreach (var target in ProjectedEvery(
                                 effect.Require("card"), state, cast))
                    {
                        long limit = Statuses.Limit(
                            cast.World, cast.World.Facts, target, status);
                        long held = state.StatusOf(cast, target, status);
                        if (held >= limit)
                        {
                            continue;
                        }
                        state.Status[(target.ObjectId, status)] = held + 1;
                        if (status == Statuses.Tough)
                        {
                            state.Tough[target.ObjectId] = held + 1;
                        }
                        bool vulnerable = status is Statuses.Stunned
                                or Statuses.Confused
                            && StateFields.Modified(
                                cast.World, target, "vulnerable",
                                cast.World.Facts, cast.World.Players) > 0
                            && held + 1 >= limit && limit > 0;
                        if (vulnerable && DiscardTreeChangesArea(target))
                        {
                            couldDiscard = true;
                        }
                    }
                }
                return states;
            }

            if (effect.Kind == "grantUntil"
                && effect.Field("trait") is { } gainedTrait)
            {
                string trait = Word(gainedTrait);
                foreach (var state in states)
                {
                    var target = ProjectedFind(
                        effect.Require("card"), state, cast);
                    if (target is null)
                    {
                        continue;
                    }
                    if (!state.Traits.TryGetValue(
                            target.ObjectId, out var granted))
                    {
                        state.Traits[target.ObjectId] = granted = [];
                    }
                    granted.Add(trait);
                }
                return states;
            }

            if (effect.Kind == "grantUntil"
                && effect.Field("keyword") is { } grantedField)
            {
                string field = Word(grantedField);
                long amount = effect.Field("amount") is { } given
                    ? Amount(given, cast) : 0;
                foreach (var state in states)
                {
                    var target = ProjectedFind(
                        effect.Require("card"), state, cast);
                    if (target is not null)
                    {
                        var key = (target.ObjectId, field);
                        state.Modifiers[key] = SaturatingSum(
                            state.Modifiers.GetValueOrDefault(key), [amount]);
                    }
                }
                return states;
            }

            if (effect.Kind == "eachPlayer")
            {
                int priorPlayer = cast.Player;
                try
                {
                    foreach (int player in cast.World.PlayerOrder)
                    {
                        cast.RestorePlayer(player);
                        states = Trace(
                            Tree(effect.Require("effect")), states,
                            repetitions, baseMultiplier);
                    }
                    return states;
                }
                finally
                {
                    cast.RestorePlayer(priorPlayer);
                }
            }

            if (effect.Kind == "and")
            {
                if (repetitions > 1)
                {
                    for (long repeat = 0; repeat < repetitions; repeat++)
                    {
                        states = Trace(
                            effect, states, 1, baseMultiplier);
                    }
                    return states;
                }
                var simultaneous = Nodes(effect.Argument).ToList();
                const int MaximumProjectedAndEffects = 12;
                if (simultaneous.Count > MaximumProjectedAndEffects)
                {
                    throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' has an and-group with "
                        + $"{simultaneous.Count} effects; projecting more than "
                        + $"{MaximumProjectedAndEffects} orders is not implemented");
                }

                var frontier = states
                    .Select(state => (Mask: 0UL, State: state.Clone()))
                    .ToList();
                for (int depth = 0; depth < simultaneous.Count; depth++)
                {
                    var next = new List<(ulong Mask, AreaProjectionState State)>();
                    foreach (var (mask, state) in frontier)
                    {
                        for (int index = 0; index < simultaneous.Count; index++)
                        {
                            ulong bit = 1UL << index;
                            if ((mask & bit) != 0)
                            {
                                continue;
                            }
                            foreach (var projected in Trace(
                                         simultaneous[index], [state.Clone()],
                                         baseMultiplier: baseMultiplier))
                            {
                                next.Add((mask | bit, projected));
                            }
                        }
                    }
                    frontier = next
                        .GroupBy(candidate =>
                            (candidate.Mask, candidate.State.Key()))
                        .Select(group => group.First()).ToList();
                }
                return AreaProjectionState.Distinct(
                    frontier.Select(candidate => candidate.State));
            }

            if (effect.Kind == "if")
            {
                var test = Tree(effect.Require("test"));
                var branched = new List<AreaProjectionState>();
                foreach (var state in states)
                {
                    bool? projected = ProjectedTest(test, state, cast);
                    var branches = projected is { } result
                        ? effect.Field(result ? "then" : "else") is { } taken
                            ? [Tree(taken)]
                            : []
                        : ReachableMutationBranches(effect, cast).ToList();
                    if (branches.Count == 0)
                    {
                        branched.Add(state);
                        continue;
                    }
                    foreach (var branch in branches)
                    {
                        branched.AddRange(Trace(
                            branch, [state.Clone()], repetitions, baseMultiplier));
                    }
                }
                return AreaProjectionState.Distinct(branched);
            }

            if (effect.Kind is "then" or "otherwise")
            {
                var predecessor = Tree(effect.Require("effect"));
                var dependent = Tree(effect.Require(effect.Kind));
                var required = effect.Kind == "then"
                    ? ResolutionOutcome.Full : ResolutionOutcome.None;
                var branched = new List<AreaProjectionState>();
                foreach (var state in states)
                {
                    var outcome = ProjectedResolution(
                        predecessor, state, cast);
                    var projected = outcome == ResolutionOutcome.None
                        ? [state]
                        : Trace(
                            predecessor, [state], repetitions,
                            baseMultiplier);
                    if (outcome == required)
                    {
                        projected = Trace(
                            dependent, projected, repetitions,
                            baseMultiplier);
                    }
                    branched.AddRange(projected);
                }
                return AreaProjectionState.Distinct(branched);
            }

            if (effect.Kind is "choose" or "chooseCard")
            {
                return states;
            }

            if (effect.Kind is "attack" or "thwart")
            {
                var projected = new List<AreaProjectionState>();
                foreach (var state in states)
                {
                    var target = ProjectedFind(
                        effect.Require("target"), state, cast);
                    if (target is null)
                    {
                        projected.Add(state);
                        continue;
                    }
                    var prior = cast.CaptureChosen();
                    try
                    {
                        cast.Choose(target);
                        projected.AddRange(Trace(
                            Tree(effect.Require("effect")), [state],
                            repetitions, baseMultiplier));
                    }
                    finally
                    {
                        cast.RestoreChosen(prior);
                    }
                }
                return projected;
            }

            if (effect.Kind == "thwartSchemes")
            {
                var projected = new List<AreaProjectionState>();
                foreach (var state in states)
                {
                    var schemes = ProjectedEvery(
                        effect.Require("schemes"), state, cast);
                    if (schemes.Count == 0)
                    {
                        projected.Add(state);
                        continue;
                    }
                    var prior = cast.CaptureChosen();
                    var priorTargets = cast.PowerTargets;
                    try
                    {
                        cast.Choose(schemes[0]);
                        cast.SetPowerTargets(schemes);
                        projected.AddRange(Trace(
                            Tree(effect.Require("power")), [state],
                            repetitions, baseMultiplier));
                    }
                    finally
                    {
                        cast.RestoreChosen(prior);
                        cast.SetPowerTargets(priorTargets);
                    }
                }
                return projected;
            }

            if (effect.Kind is "defense" or "delayUntil")
            {
                return Trace(
                    Tree(effect.Require("effect")), states,
                    repetitions, baseMultiplier);
            }

            if (effect.Kind == "discard")
            {
                foreach (var state in states)
                {
                    foreach (var card in ProjectedEvery(
                                 effect.Field("card") ?? effect.Argument,
                                 state,
                                 cast))
                    {
                        couldDiscard |= queried.Contains(card.Area.Type)
                            || DiscardTreeChangesArea(card);
                        MarkDiscardedTree(state, card);
                    }
                }
                return states;
            }

            if (effect.Kind == "attachTo")
            {
                foreach (var state in states)
                {
                    var host = ProjectedFind(effect.Argument, state, cast);
                    if (host is null)
                    {
                        continue;
                    }
                    couldDiscard |= queried.Contains(cast.Source.Area.Type)
                        || queried.Contains(DeckType.UpgradesArea);
                    state.Departed.Remove(cast.Source.ObjectId);
                    state.Entered.Add(cast.Source.ObjectId);
                    state.Hosts[cast.Source.ObjectId] = host.ObjectId;
                }
                return states;
            }

            if (effect.Kind is "removeFromGame" or "returnToHand" or "reveal")
            {
                var destination = effect.Kind switch
                {
                    "removeFromGame" => DeckType.RemovedArea,
                    "returnToHand" => DeckType.HandsArea,
                    _ => DeckType.RevealingArea,
                };
                foreach (var state in states)
                {
                    foreach (var card in ProjectedEvery(
                                 effect.Argument, state, cast))
                    {
                        couldDiscard |= queried.Contains(card.Area.Type)
                            || queried.Contains(destination)
                            || HostedCardsChangeArea(state, card.ObjectId);
                        MarkDiscardedTree(state, card);
                    }
                }
                return states;
            }

            if (effect.Kind == "putIntoPlay")
            {
                foreach (var state in states)
                {
                    var selector = effect.Require("card");
                    var cards = ProjectedEvery(selector, state, cast);
                    if (cards.Count == 0
                        && selector is AbilityValue.Word { Value: "this" }
                        && state.SourceReferenceCurrent)
                    {
                        cards.Add(cast.Source);
                    }
                    foreach (var card in cards)
                    {
                        state.Departed.Remove(card.ObjectId);
                        state.Entered.Add(card.ObjectId);
                        if (effect.Field("where") is { } where
                            && string.Equals(
                                Word(where), "engagedWithYou",
                                StringComparison.Ordinal))
                        {
                            state.EngagedWith[card.ObjectId] = Resolver(cast);
                        }
                        if (StateFields.Modified(
                                cast.World, card, "toughness",
                                cast.World.Facts, cast.World.Players) > 0)
                        {
                            state.Tough[card.ObjectId] = Math.Max(
                                1, state.ToughOf(cast, card));
                        }
                        if (selector is AbilityValue.Word { Value: "this" })
                        {
                            // Moving out of play and entering play creates a
                            // new incarnation. Later `this` references retain
                            // the old binding and no longer denote the card.
                            state.SourceReferenceCurrent = false;
                        }
                    }
                }
                couldDiscard |= MayChangeAnyArea(
                    effect, queried, cast, baseMultiplier);
                return states;
            }

            if (effect.Kind is "draw" or "drawToHandSize"
                or "drawToPrintedHandSize" or "search"
                or "shuffleInto" or "dealEncounterCard" or "dealEncounterCards"
                or "revealTop" or "discardTop" or "discardUntil"
                or "createDrones" or "indirectDamage" or "discardFromHand"
                or "discardUpToFromHand" or "discardAnyFromHand" or "spend"
                or "spendPrinted" or "spendEnergyX")
            {
                couldDiscard |= MayChangeAnyArea(
                    effect, queried, cast, baseMultiplier);
                return states;
            }

            if (repetitions > 1)
            {
                for (long repeat = 0; repeat < repetitions; repeat++)
                {
                    states = Trace(effect, states, 1, baseMultiplier);
                }
                return states;
            }
            return TraceSequence(
                StructuralChildren(effect), states, baseMultiplier);
        }

        _ = TraceSequence(
            effects, [new AreaProjectionState(cast)], baseMultiplier);
        return couldDiscard;
    }

    private static bool DefeatTreeChangesArea(
        Card root, IReadOnlySet<DeckType> queried, Cast cast)
    {
        var kind = cast.World.Facts.Kind(root.FaceId);
        bool rootDiscards = kind is CardKind.Minion or CardKind.Ally
                or CardKind.EncounterSideScheme
            && !Keywords.Has(cast.World, root, "victory", cast.World.Facts);
        if (rootDiscards
            && queried.Contains(root.Owner < 0
                ? DeckType.EncounterDiscardPile : DeckType.DiscardPile))
        {
            return true;
        }

        if (kind == CardKind.EncounterVillain)
        {
            var villainDeck = cast.World.AreaOf(DeckType.VillainDeck).Cards;
            var next = villainDeck.Count > 0 ? villainDeck[^1] : null;
            if (next is not null && string.Equals(
                cast.World.Facts.Title(root.FaceId),
                cast.World.Facts.Title(next.FaceId),
                StringComparison.Ordinal))
            {
                return false;
            }
        }
        else if (kind is not (
            CardKind.Minion or CardKind.Ally or CardKind.EncounterSideScheme))
        {
            return false;
        }

        var direct = cast.World.Areas
            .Where(area => area.Host == root.ObjectId)
            .SelectMany(area => area.Cards)
            .ToList();
        var pending = new Stack<Card>();
        var seen = new HashSet<int> { root.ObjectId };
        foreach (var card in direct)
        {
            bool movesToVictory = cast.World.Facts.Kind(card.FaceId) is
                    CardKind.Attachment or CardKind.Upgrade
                && DeckTypes.IsInPlay(card.Area.Type)
                && Keywords.Has(cast.World, card, "victory", cast.World.Facts);
            if (movesToVictory)
            {
                foreach (var child in cast.World.Areas
                             .Where(area => area.Host == card.ObjectId)
                             .SelectMany(area => area.Cards))
                {
                    pending.Push(child);
                }
                seen.Add(card.ObjectId);
            }
            else
            {
                pending.Push(card);
            }
        }
        while (pending.TryPop(out var card))
        {
            if (!seen.Add(card.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' reaches a hosted-card cycle while "
                    + "projecting defeat");
            }
            if (queried.Contains(card.Owner < 0
                    ? DeckType.EncounterDiscardPile : DeckType.DiscardPile))
            {
                return true;
            }
            foreach (var child in cast.World.Areas
                         .Where(area => area.Host == card.ObjectId)
                         .SelectMany(area => area.Cards))
            {
                pending.Push(child);
            }
        }
        return false;
    }

    private static List<Card> ProjectedEvery(
        AbilityValue selector, AreaProjectionState state, Cast cast)
    {
        var found = Every(selector, cast)
            .Where(card => !state.Departed.Contains(card.ObjectId))
            .ToList();
        if (selector is AbilityValue.Word)
        {
            if (selector is AbilityValue.Word { Value: "this" }
                && !state.SourceReferenceCurrent)
            {
                return [];
            }
            return found;
        }

        var node = Tree(selector);
        if (node.Kind == "enemiesWithTrait")
        {
            string wanted = Word(node.Argument);
            return
            [
                .. ProjectedEvery(
                        new AbilityValue.Map(new Dictionary<string, AbilityValue>
                        {
                            ["query"] = new AbilityValue.Word("enemies"),
                        }),
                        state,
                        cast)
                    .Where(card => state.HasTrait(cast, card, wanted)),
            ];
        }
        if (node.Kind is "minBy" or "maxBy")
        {
            var among = ProjectedEvery(node.Require("of"), state, cast)
                .Where(card => state.Entered.Contains(card.ObjectId)
                    ? Rules.Play.Discard.EffectCanRemove(
                        cast.World, cast.World.Facts, cast.Source, card)
                    : CanRemoveByEffect(node.Require("of"), cast, card))
                .ToList();
            if (among.Count == 0)
            {
                return [];
            }
            string key = Word(node.Require("by"));
            long Rank(Card card) => key switch
            {
                "cost" => cast.World.Facts.PrintedValue(
                    card.FaceId, "Cost", cast.World.Players),
                "attack" => state.ModifiedOf(cast, card, "attack"),
                "printedHealth" => cast.World.Facts.PrintedValue(
                    card.FaceId, "HP", cast.World.Players),
                _ => throw new AbilityException(
                    $"'{key}' is not a value cards can be ranked by"),
            };
            long extreme = node.Kind == "minBy"
                ? among.Min(Rank) : among.Max(Rank);
            return [.. among.Where(card => Rank(card) == extreme)];
        }
        if (node.Kind == "withTrait")
        {
            string wanted = Word(node.Require("trait"));
            return
            [
                .. ProjectedEvery(node.Require("cards"), state, cast)
                    .Where(card => state.HasTrait(cast, card, wanted)),
            ];
        }
        if (node.Kind == "query"
            && string.Equals(
                Word(node.Argument), "villain", StringComparison.Ordinal)
            && state.ActiveVillain >= 0)
        {
            found.RemoveAll(card => cast.World.Facts.Kind(card.FaceId)
                == CardKind.EncounterVillain);
            found.Add(cast.World.Cards[state.ActiveVillain]);
        }
        else if (node.Kind == "query")
        {
            string query = Word(node.Argument);
            if (query is "attackableEnemies" or "attackableMinions")
            {
                string candidates = query == "attackableEnemies"
                    ? "enemies" : "minions";
                found = ProjectedEvery(
                    new AbilityValue.Map(new Dictionary<string, AbilityValue>
                    {
                        ["query"] = new AbilityValue.Word(candidates),
                    }),
                    state,
                    cast);
                found = found.Where(card =>
                        cast.World.Abilities.CanTakeDamage(
                            cast.World, card, cast.Source))
                    .Where(card => query != "attackableMinions"
                        || cast.World.Facts.Kind(card.FaceId) == CardKind.Minion)
                    .ToList();
                bool guard = found.Any(card =>
                    cast.World.Facts.Kind(card.FaceId) == CardKind.Minion
                    && state.ModifiedOf(cast, card, "guard") > 0
                    && (state.EngagedWith.GetValueOrDefault(
                            card.ObjectId, -1) == Resolver(cast)
                        || !state.EngagedWith.ContainsKey(card.ObjectId)
                            && card.Area.PlayArea == PlayArea.Of(Resolver(cast))));
                if (guard)
                {
                    found.RemoveAll(card => cast.World.Facts.Kind(card.FaceId)
                        == CardKind.EncounterVillain);
                }
            }
            else
            {
                found.AddRange(state.Entered
                    .Select(id => cast.World.Cards[id])
                    .Where(card => !state.Departed.Contains(card.ObjectId))
                    .Where(card => query switch
                    {
                        "minions" => cast.World.Facts.Kind(card.FaceId)
                            == CardKind.Minion,
                        "minionsEngagedWithYou" =>
                            cast.World.Facts.Kind(card.FaceId) == CardKind.Minion
                            && state.EngagedWith.GetValueOrDefault(
                                card.ObjectId, -1) == Resolver(cast),
                        "enemiesEngagedWithChosenPlayer" =>
                            cast.World.Facts.Kind(card.FaceId) == CardKind.Minion
                            && state.EngagedWith.GetValueOrDefault(
                                card.ObjectId, -1) == ChosenPlayer(cast).Owner,
                        "enemies" => cast.World.Facts.Kind(card.FaceId) is
                            CardKind.Minion or CardKind.EncounterVillain,
                        "characters" => cast.World.Facts.Kind(card.FaceId) is
                            CardKind.Minion or CardKind.EncounterVillain
                                or CardKind.Ally or CardKind.Hero or CardKind.AlterEgo,
                        "sideSchemes" or "schemes" or "thwartableSchemes" =>
                            cast.World.Facts.Kind(card.FaceId)
                                == CardKind.EncounterSideScheme,
                        _ => false,
                    }));
            }
        }
        else if (node.Kind == "titled")
        {
            string title = Word(node.Argument);
            found.AddRange(state.Entered
                .Select(id => cast.World.Cards[id])
                .Where(card => !state.Departed.Contains(card.ObjectId))
                .Where(card => string.Equals(
                    cast.World.Facts.Title(card.FaceId),
                    title,
                    StringComparison.Ordinal)));
        }
        return [.. found.DistinctBy(card => card.ObjectId)];
    }

    private static Card? ProjectedFind(
        AbilityValue selector, AreaProjectionState state, Cast cast)
    {
        var found = ProjectedEvery(selector, state, cast);
        return found.Count switch
        {
            0 => null,
            1 => found[0],
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' projects {found.Count} cards where one is required"),
        };
    }

    private static bool? ProjectedTest(
        AbilityNode test, AreaProjectionState state, Cast cast)
    {
        if (test.Kind == "hasStatus"
            && ProjectedFind(test.Require("card"), state, cast) is { } target)
        {
            return state.StatusOf(
                cast, target, Word(test.Require("status"))) > 0;
        }
        if (test.Kind == "not")
        {
            return ProjectedTest(Tree(test.Argument), state, cast) is { } inner
                ? !inner : null;
        }
        if (test.Kind is "and" or "or")
        {
            var values = Nodes(test.Argument)
                .Select(child => ProjectedTest(child, state, cast)).ToList();
            if (test.Kind == "and" && values.Any(value => value == false))
            {
                return false;
            }
            if (test.Kind == "or" && values.Any(value => value == true))
            {
                return true;
            }
            return values.Any(value => value is null)
                ? null
                : test.Kind == "and";
        }
        return null;
    }

    private static ResolutionOutcome ProjectedResolution(
        AbilityNode effect, AreaProjectionState state, Cast cast) =>
        effect.Kind switch
        {
            "seq" or "and" => CombinedOutcomes(
                Nodes(effect.Argument).Select(child =>
                    ProjectedResolution(child, state, cast))),
            "if" => ProjectedTest(
                    Tree(effect.Require("test")), state, cast)
                is { } result
                    ? effect.Field(result ? "then" : "else") is { } branch
                        ? ProjectedResolution(Tree(branch), state, cast)
                        : ResolutionOutcome.None
                    : throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' has a projected dependent condition "
                        + "whose outcome is not implemented"),
            "forEach" when ForEachCount(effect, cast) == 0 =>
                ResolutionOutcome.None,
            "forEach" => ProjectedResolution(
                Tree(effect.Require("effect")), state, cast),
            "heal" => ResolutionOfAmount(
                ProjectedFind(effect.Require("card"), state, cast) is { } healed
                    ? state.DamageOf(healed) : 0,
                Amount(effect.Require("amount"), cast)),
            "removeThreat" => CombinedOutcomes(
                ProjectedEvery(effect.Require("scheme"), state, cast).Select(scheme =>
                    ResolutionOfAmount(
                        state.ThreatOf(scheme),
                        Amount(effect.Require("amount"), cast)))),
            _ => ResolutionOf(effect, cast),
        };

    private sealed class AreaProjectionState(Cast cast)
    {
        public Dictionary<int, long> Damage { get; } = [];
        public Dictionary<int, long> Tough { get; } = [];
        public Dictionary<int, long> Threat { get; } = [];
        public Dictionary<(int Card, string Status), long> Status { get; } = [];
        public HashSet<int> Departed { get; } = [];
        public HashSet<int> Entered { get; } = [];
        public Dictionary<int, int> Hosts { get; } = [];
        public Dictionary<int, int> EngagedWith { get; } = [];
        public Dictionary<int, HashSet<string>> Traits { get; } = [];
        public Dictionary<(int Card, string Field), long> Modifiers { get; } = [];
        public bool SourceReferenceCurrent { get; set; } = true;
        public int ActiveVillain { get; set; } =
            cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        public int VillainAttachmentHost { get; set; } = -1;

        public long DamageOf(Card card) =>
            Damage.GetValueOrDefault(card.ObjectId, card.Damage);

        public long ToughOf(Cast current, Card card) => Tough.GetValueOrDefault(
            card.ObjectId,
            Statuses.Count(current.World, card, Statuses.Tough));

        public long StatusOf(Cast current, Card card, string status) =>
            status == Statuses.Tough
                ? ToughOf(current, card)
                : Status.GetValueOrDefault(
                    (card.ObjectId, status),
                    Statuses.Count(current.World, card, status));

        public long ThreatOf(Card card) => Threat.GetValueOrDefault(
            card.ObjectId, card.Tokens.GetValueOrDefault("k_threat"));

        public long HealthOf(Cast current, Card card) => SaturatingSum(
            FacedownDrones.BaseValue(
                card, current.World.Facts, "HP", current.World.Players),
            [ModifiedOf(current, card, "health")]);

        public long ModifiedOf(Cast current, Card card, string field)
        {
            long value = StateFields.Modified(
                current.World, card, field,
                current.World.Facts, current.World.Players);
            var effects = current.World.Effects.Active();
            foreach (var effect in effects)
            {
                if (effect.Source == EffectSource.ConstantAbility
                    && effect.Card is int hostedSource
                    && !Departed.Contains(hostedSource)
                    && Hosts.TryGetValue(hostedSource, out int projectedHost)
                    && string.Equals(effect.Kind, field, StringComparison.Ordinal))
                {
                    bool liveApplies = effect.AppliesTo(current.World, card);
                    if (liveApplies && projectedHost != card.ObjectId)
                    {
                        value -= effect.Amount;
                    }
                    else if (!liveApplies && projectedHost == card.ObjectId)
                    {
                        value += effect.Amount;
                    }
                }
                if (effect.Source == EffectSource.ConstantAbility
                    && effect.Card is int source
                    && Departed.Contains(source)
                    && string.Equals(effect.Kind, field, StringComparison.Ordinal)
                    && effect.AppliesTo(current.World, card))
                {
                    value -= effect.Amount;
                }
            }
            if (ProjectedPredicateInputsChanged(current)
                && ConditionalConstantMayModify(current, card, field))
            {
                throw new RulesNotImplementedException(
                    $"'{current.Source.FaceId}' changes game state before reading "
                    + $"a conditional constant '{field}' modifier on "
                    + $"'{card.FaceId}'; projecting that predicate is not implemented");
            }
            value = SaturatingSum(
                value,
                [Modifiers.GetValueOrDefault((card.ObjectId, field))]);

            string? printedModifier = field switch
            {
                "attack" => "ATK+",
                "scheme" => "SCH+",
                "thwart" => "THW+",
                _ => null,
            };
            if (printedModifier is null)
            {
                return value;
            }
            foreach (var attached in current.World.Cards.Where(candidate =>
                         candidate.Area.Host == card.ObjectId
                         && DeckTypes.IsInPlay(candidate.Area.Type)))
            {
                if (Departed.Contains(attached.ObjectId)
                    || Hosts.TryGetValue(attached.ObjectId, out int projected)
                        && projected != card.ObjectId)
                {
                    value -= current.World.Facts.PrintedValue(
                        attached.FaceId, printedModifier, current.World.Players);
                }
            }
            foreach (var (attachedId, host) in Hosts)
            {
                var attached = current.World.Cards[attachedId];
                if (host == card.ObjectId
                    && attached.Area.Host != card.ObjectId
                    && !Departed.Contains(attachedId))
                {
                    value += current.World.Facts.PrintedValue(
                        attached.FaceId, printedModifier, current.World.Players);
                }
            }
            return value;
        }

        private bool ProjectedPredicateInputsChanged(Cast current) =>
            Damage.Any(pair => pair.Value
                != current.World.Cards[pair.Key].Damage)
            || Tough.Any(pair => pair.Value != Statuses.Count(
                current.World, current.World.Cards[pair.Key], Statuses.Tough))
            || Threat.Any(pair => pair.Value != current.World.Cards[pair.Key]
                .Tokens.GetValueOrDefault("k_threat"))
            || Status.Any(pair => pair.Value != Statuses.Count(
                current.World,
                current.World.Cards[pair.Key.Card], pair.Key.Status))
            || Departed.Count > 0
            || Entered.Count > 0
            || Hosts.Count > 0
            || EngagedWith.Count > 0
            || Traits.Count > 0
            || Modifiers.Count > 0
            || ActiveVillain != (current.World.TheCardIn(
                DeckType.VillainArea)?.ObjectId ?? -1);

        private bool ConditionalConstantMayModify(
            Cast current, Card target, string field)
        {
            if (current.Abilities is not AbilityRunner runner)
            {
                return false;
            }
            var sources = current.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Concat(Entered.Select(id => current.World.Cards[id]))
                .Where(source => !Departed.Contains(source.ObjectId))
                .DistinctBy(source => source.ObjectId);
            return sources
                .Any(source => runner.On(source)
                    .Where(ability =>
                        ability.Trigger.Timing == AbilityType.Constant)
                    .Any(ability => ConditionalGrant(
                        ability.Effect, field, ability.When is not null,
                        source, target, current)));
        }

        private bool ConditionalGrant(
            AbilityNode node, string field, bool conditioned,
            Card source, Card target, Cast current)
        {
            if (node.Kind == "if")
            {
                return Branches.Select(node.Field)
                    .Where(value => value is not null)
                    .Any(value => ConditionalGrant(
                        Tree(value!), field, conditioned: true,
                        source, target, current));
            }
            if (node.Kind is "grant" or "grantEach"
                && node.Field("keyword") is { } keyword
                && string.Equals(Word(keyword), field, StringComparison.Ordinal))
            {
                bool dynamicAmount = node.Field("amount") is { } amount
                    && amount is not AbilityValue.Number;
                return (conditioned || dynamicAmount)
                    && GrantCouldAffect(node, source, target, current);
            }
            if (node.Kind is "seq" or "and")
            {
                return Nodes(node.Argument).Any(child =>
                    ConditionalGrant(
                        child, field, conditioned, source, target, current));
            }
            return false;
        }

        private bool GrantCouldAffect(
            AbilityNode grant, Card source, Card target, Cast current)
        {
            var selector = grant.Require(
                grant.Kind == "grant" ? "card" : "cards");
            if (selector is AbilityValue.Word { Value: "this" })
            {
                return source.ObjectId == target.ObjectId;
            }
            if (selector is AbilityValue.Word { Value: "attachedTo" })
            {
                return Hosts.GetValueOrDefault(
                    source.ObjectId, source.Area.Host) == target.ObjectId;
            }
            if (selector is AbilityValue.Map
                && Tree(selector) is { Kind: "titled" } titled)
            {
                return string.Equals(
                    current.World.Facts.Title(target.FaceId),
                    Word(titled.Argument), StringComparison.Ordinal);
            }
            if (selector is AbilityValue.Map
                && Tree(selector) is { Kind: "query" } query
                && query.Argument is AbilityValue.Word { Value: "villain" })
            {
                return current.World.Facts.Kind(target.FaceId)
                    == CardKind.EncounterVillain;
            }
            // Other selectors may change membership with the projected facts.
            // Failing closed is required until that membership is projected.
            return true;
        }

        public bool HasTrait(Cast current, Card card, string trait)
        {
            var active = current.World.Effects.Active()
                .Where(effect => effect.Source != EffectSource.ConstantAbility
                    || effect.Card is not int source
                    || !Departed.Contains(source))
                .ToList();
            bool lost = active.Any(effect =>
                ProjectedTraitEffectApplies(current, effect, card)
                && string.Equals(
                    effect.Kind,
                    Characteristics.Lost + Rules.State.Traits.Granted + trait,
                    StringComparison.Ordinal));
            if (lost)
            {
                return false;
            }
            return FacedownDrones.InherentTraits(card, current.World.Facts)
                    .Contains(trait, StringComparer.Ordinal)
                || active.Any(effect =>
                    ProjectedTraitEffectApplies(current, effect, card)
                    && string.Equals(
                        effect.Kind,
                        Rules.State.Traits.Granted + trait,
                        StringComparison.Ordinal))
                || Traits.TryGetValue(card.ObjectId, out var granted)
                    && granted.Contains(trait);
        }

        private bool ProjectedTraitEffectApplies(
            Cast current, ContinuousEffect effect, Card card)
        {
            if (effect.Source == EffectSource.ConstantAbility
                && effect.Card is int source
                && Hosts.TryGetValue(source, out int projectedHost)
                && current.World.Cards[source].Area.Host == effect.Affects)
            {
                return projectedHost == card.ObjectId;
            }
            return effect.AppliesTo(current.World, card);
        }

        public AreaProjectionState Clone()
        {
            var clone = new AreaProjectionState(cast);
            foreach (var (card, amount) in Damage)
            {
                clone.Damage[card] = amount;
            }
            foreach (var (card, count) in Tough)
            {
                clone.Tough[card] = count;
            }
            foreach (var (card, amount) in Threat)
            {
                clone.Threat[card] = amount;
            }
            foreach (var (key, count) in Status)
            {
                clone.Status[key] = count;
            }
            clone.Departed.UnionWith(Departed);
            clone.Entered.UnionWith(Entered);
            foreach (var (card, host) in Hosts)
            {
                clone.Hosts[card] = host;
            }
            foreach (var (card, player) in EngagedWith)
            {
                clone.EngagedWith[card] = player;
            }
            foreach (var (card, traits) in Traits)
            {
                clone.Traits[card] = [.. traits];
            }
            foreach (var (key, amount) in Modifiers)
            {
                clone.Modifiers[key] = amount;
            }
            clone.ActiveVillain = ActiveVillain;
            clone.VillainAttachmentHost = VillainAttachmentHost;
            clone.SourceReferenceCurrent = SourceReferenceCurrent;
            return clone;
        }

        public static List<AreaProjectionState> Distinct(
            IEnumerable<AreaProjectionState> states) =>
            states.GroupBy(state => state.Key(), StringComparer.Ordinal)
                .Select(group => group.First()).ToList();

        public string Key() => string.Join(
            ";",
            Damage.OrderBy(pair => pair.Key)
                .Select(pair => $"d{pair.Key}:{pair.Value}")
                .Concat(Tough.OrderBy(pair => pair.Key)
                    .Select(pair => $"s{pair.Key}:{pair.Value}"))
                .Concat(Threat.OrderBy(pair => pair.Key)
                    .Select(pair => $"t{pair.Key}:{pair.Value}"))
                .Concat(Status.OrderBy(pair => pair.Key.Card)
                    .ThenBy(pair => pair.Key.Status, StringComparer.Ordinal)
                    .Select(pair =>
                        $"x{pair.Key.Card}:{pair.Key.Status}:{pair.Value}"))
                .Concat(Departed.Order().Select(card => $"o{card}"))
                .Concat(Entered.Order().Select(card => $"i{card}"))
                .Concat(Hosts.OrderBy(pair => pair.Key)
                    .Select(pair => $"h{pair.Key}:{pair.Value}"))
                .Concat(EngagedWith.OrderBy(pair => pair.Key)
                    .Select(pair => $"e{pair.Key}:{pair.Value}"))
                .Concat(Traits.OrderBy(pair => pair.Key)
                    .SelectMany(pair => pair.Value.Order(StringComparer.Ordinal)
                        .Select(trait => $"g{pair.Key}:{trait}")))
                .Concat(Modifiers.OrderBy(pair => pair.Key.Card)
                    .ThenBy(pair => pair.Key.Field, StringComparer.Ordinal)
                    .Select(pair =>
                        $"m{pair.Key.Card}:{pair.Key.Field}:{pair.Value}"))
                .Append($"v{ActiveVillain}:{VillainAttachmentHost}")
                .Append($"r{SourceReferenceCurrent}"));
    }

}
