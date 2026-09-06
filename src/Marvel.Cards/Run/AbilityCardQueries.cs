using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Stateless card and binding queries. Results never allocate board areas or
// record information exposure; execution owns those effects.
internal static class AbilityCardQueries
{
    internal static IReadOnlyList<Card> Cards(
        AbilityCardQuery query, AbilityQueryContext cast, AbilityProgram? program = null)
    {
        switch (query)
        {
            case AbilityCardQuery.MinionsEngagedWithYou:
            {
                // `rr:engage.1` -- "when a minion engages a player, it is placed in
                // that player's play area". Engagement *is* which area the minion
                // sits in, so this is a read of the board and not of a flag; and
                // "you" is the player resolving the card, so a minion engaged with
                // somebody else is not in this list however close it is on the
                // table.
                return [.. InArea(cast.World, DeckType.EngagedEnemiesArea, PlayArea.Of(cast.Player))];
            }
            case AbilityCardQuery.IdentitiesWithinPerPlayerLimit:
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
            case AbilityCardQuery.AttachedToThis:
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
            case AbilityCardQuery.HeroesAndAllies:
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
            case AbilityCardQuery.SideSchemes:
            {
                // "Each side scheme", which reaches the players' as well as the
                // scenario's: `rr:player-side-scheme` calls them "the player card
                // equivalent of the side schemes found in the encounter deck" and
                // `.1` puts them in the same place, next to the main scheme.
                return [.. InArea(cast.World, DeckType.SideSchemesArea, PlayArea.Villains)];
            }
            case AbilityCardQuery.Minions:
            {
                // `rr:minion.3`: minions in play are engaged with players, so the
                // engaged-enemy areas across every play area are the complete set.
                return
                [
                    .. cast.World.Areas
                        .Where(area => area.Type == DeckType.EngagedEnemiesArea)
                        .SelectMany(area => area.Cards)
                        .Where(card => FacedownDrones.Kind(
                            card, cast.World.Facts) == CardKind.Minion),
                ];
            }
            case AbilityCardQuery.Enemies:
            {
                return
                [
                    .. cast.World.Areas
                        .Where(area => area.Type is DeckType.VillainArea
                            or DeckType.EngagedEnemiesArea)
                        .SelectMany(area => area.Cards)
                        .Where(card => CardKinds.IsEnemy(
                            FacedownDrones.Kind(card, cast.World.Facts))),
                ];
            }
            case AbilityCardQuery.AttackableEnemies:
            {
                return
                [
                    .. BasicPowers.Attackable(cast.World, cast.World.Facts, Resolver(cast))
                        .Where(enemy => AbilityProgramQueries.CanTakeDamage(
                            cast.World, program ?? throw new InvalidOperationException(
                                "Attackable-enemy queries require the authored ability program"),
                            enemy, cast.Source)),
                ];
            }
            case AbilityCardQuery.AttackableMinions:
            {
                return
                [
                    .. BasicPowers.Attackable(cast.World, cast.World.Facts, Resolver(cast))
                        .Where(enemy => FacedownDrones.Kind(
                            enemy, cast.World.Facts) == CardKind.Minion)
                        .Where(enemy => AbilityProgramQueries.CanTakeDamage(
                            cast.World, program ?? throw new InvalidOperationException(
                                "Attackable-minion queries require the authored ability program"),
                            enemy, cast.Source)),
                ];
            }
            case AbilityCardQuery.Schemes:
            {
                return
                [
                    .. InArea(cast.World, DeckType.MainSchemesArea, PlayArea.Villains),
                    .. InArea(cast.World, DeckType.SideSchemesArea, PlayArea.Villains),
                ];
            }
            case AbilityCardQuery.ThwartableSchemes:
            {
                return BasicPowers.Thwartable(cast.World, cast.World.Facts, Resolver(cast));
            }
            case AbilityCardQuery.PowerTargets:
            {
                return cast.PowerTargets;
            }
            case AbilityCardQuery.YourAsidePile:
            {
                // "The rest of your set-aside nemesis encounter set" -- whatever is
                // still in the pile once the cards this ability took out of it have
                // gone. The obligation is not among them: setup shuffles it into
                // the encounter deck long before this resolves.
                return [.. cast.World.Seats[cast.Player].Nemesis.Cards];
            }
            case AbilityCardQuery.UpgradesAndSupportsYouControl:
            {
                // "An upgrade or support **you control**." A player's upgrades and
                // supports sit in their own play area, so control is where the card
                // is -- the same reading `rr:engage.1` gets for a minion.
                return
                [
                    .. cast.World.Areas
                        .Where(area => area.Type is DeckType.UpgradesArea or DeckType.SupportsArea
                            && area.PlayArea == PlayArea.Of(cast.Player))
                        .SelectMany(area => area.Cards),
                ];
            }
            case AbilityCardQuery.IdentitySpecificInYourHand:
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
            case AbilityCardQuery.SupportsYouControl:
            {
                // The support half of `upgradesAndSupportsYouControl`, on its own,
                // because Speed Demon's boost says "support" and an upgrade is not
                // one. `rr:play-area.1` again for what "you control" reads as.
                return [.. InArea(cast.World, DeckType.SupportsArea, PlayArea.Of(cast.Player))];
            }
            case AbilityCardQuery.CharactersYouControl:
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
                    .. InArea(cast.World, DeckType.AlliesArea, PlayArea.Of(cast.Player)),
                ];
            }
            case AbilityCardQuery.UpgradesYouControl:
            {
                // The upgrade half of `upgradesAndSupportsYouControl`, on its own,
                // because Beetle's two abilities both say "upgrade" and a support
                // is not one. Same reading of control: `rr:play-area.1` puts "any
                // cards in play under their control" in a player's own play area.
                return
                [
                    .. cast.World.Areas
                        .Where(area => area.Type == DeckType.UpgradesArea
                            && area.PlayArea == PlayArea.Of(cast.Player))
                        .SelectMany(area => area.Cards),
                ];
            }
            case AbilityCardQuery.BlackPantherUpgrades:
            {
                return
                [
                    .. cast.World.Areas
                        .Where(area => area.Type == DeckType.UpgradesArea
                            && area.PlayArea == PlayArea.Of(cast.Player))
                        .SelectMany(area => area.Cards)
                        .Where(card => Rules.State.Traits.Has(
                            cast.World, card, "BLACK_PANTHER", cast.World.Facts)),
                ];
            }
            case AbilityCardQuery.EnemiesEngagedWithChosenPlayer:
            {
                int player = ChosenPlayer(cast).Owner;
                return
                [
                    .. InArea(cast.World, DeckType.EngagedEnemiesArea, PlayArea.Of(player)),
                ];
            }
            case AbilityCardQuery.AlliesYouControl:
            {
                // "Each ally **you control**", which is where the card is:
                // `rr:play-area.1` puts "any cards in play under their control" in
                // a player's own play area, so control is a read of the board
                // rather than a field -- the same reading `rr:engage.1` gets for a
                // minion. Not `heroesAndAllies`, which is every player's: Boomerang
                // hits the allies of the player it attacked and nobody else's.
                return [.. InArea(cast.World, DeckType.AlliesArea, PlayArea.Of(cast.Player))];
            }
            case AbilityCardQuery.Allies:
            {
                // Inspired prints "Attach to an ally," not "an ally you control."
                // `rr:friendly` makes every player-controlled card friendly, and
                // `rr:upgrade.3.1` expressly gives the host's controller control of
                // an upgrade another player owns.
                return
                [
                    .. cast.World.Areas
                        .Where(area => area.Type == DeckType.AlliesArea)
                        .SelectMany(area => area.Cards),
                ];
            }
            case AbilityCardQuery.Heroes:
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
            case AbilityCardQuery.Identities:
            {
                return [.. cast.World.PlayerOrder.Select(player =>
                    cast.World.Seats[player].IdentityCard)];
            }
            case AbilityCardQuery.IdentitiesWithTechInDiscard:
            {
                return
                [
                    .. cast.World.PlayerOrder
                        .Where(player => InArea(cast.World, DeckType.DiscardPile, PlayArea.Of(player)).Any(card => Rules.State.Traits.Has(
                                cast.World, card, "TECH", cast.World.Facts)))
                        .Select(player => cast.World.Seats[player].IdentityCard),
                ];
            }
            case AbilityCardQuery.TopmostTechInChosenDiscard:
            {
                int player = ChosenPlayer(cast).Owner;
                var card = InArea(cast.World, DeckType.DiscardPile, PlayArea.Of(player)).LastOrDefault(candidate => Rules.State.Traits.Has(
                        cast.World, candidate, "TECH", cast.World.Facts));
                return card is null ? [] : [card];
            }
            case AbilityCardQuery.Characters:
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
            case AbilityCardQuery.Drones:
            {
                return FacedownDrones.InPlay(cast.World);
            }
            case AbilityCardQuery.DronesEngagedWithYou:
            {
                return [.. InArea(cast.World, DeckType.EngagedEnemiesArea, PlayArea.Of(Resolver(cast)))
                    .Where(card => FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                        && Rules.State.Traits.Has(
                            cast.World, card, "DRONE", cast.World.Facts))];
            }
            case AbilityCardQuery.Villain:
                return cast.World.TheCardIn(DeckType.VillainArea) is { } villain ? [villain] : [];
            case AbilityCardQuery.MainScheme:
                return cast.World.TheCardIn(DeckType.MainSchemesArea) is { } scheme ? [scheme] : [];
            case AbilityCardQuery.YourAsideMinion:
                return Aside(cast, CardKind.Minion) is { } minion ? [minion] : [];
            case AbilityCardQuery.YourAsideSideScheme:
                return Aside(cast, CardKind.EncounterSideScheme) is { } aside ? [aside] : [];
            default:
                throw new InvalidOperationException("Unknown compiled card query");
        }
    }
    internal static Card? Named(AbilityCardBinding name, AbilityQueryContext cast) => name switch
    {
        AbilityCardBinding.This => cast.SourceReference,
        AbilityCardBinding.That => cast.Altered,

        // "Stun **the attacking character**." Not the attacking player:
        // `rr:ally.2` lets a player attack with an ally, and `rr:you-your.15`
        // is emphatic that an ally's attack is *not* performed by that player's
        // identity -- so Shocker stuns whichever character swung, and the
        // player standing behind it is untouched.
        AbilityCardBinding.TriggerActor => cast.Occurrence.Actor >= 0
            ? cast.World.Cards[cast.Occurrence.Actor]
            : null,

        AbilityCardBinding.TriggerTarget => cast.Occurrence.Target >= 0
            ? cast.World.Cards[cast.Occurrence.Target]
            : null,

        // The card a `chooseCard` was answered with. Null while the ability is
        // still asking, which is why nothing before the answer can read it.
        AbilityCardBinding.Chosen => cast.Chosen,

        // "Your hero" and not "you". `rr:form-change-form.5`: "while a player
        // is in alter-ego form, card abilities that interact with their hero do
        // not interact with their identity" -- so this names nothing at all
        // when the player has flipped down, and a card that has something to
        // say about that says it with `exists`.
        AbilityCardBinding.YourHero => Forms.In(
            cast.World, cast.World.Seats[Resolver(cast)], cast.World.Facts, Forms.Hero)
            ? cast.World.Seats[Resolver(cast)].IdentityCard
            : null,

        // The other half of the form-specific reference. `rr:form-change-form.4`
        // says a hero-form identity is not its alter-ego for card abilities,
        // just as `.5` says an alter-ego-form identity is not its hero.
        AbilityCardBinding.YourAlterEgo => Forms.In(
            cast.World, cast.World.Seats[Resolver(cast)], cast.World.Facts, Forms.AlterEgo)
            ? cast.World.Seats[Resolver(cast)].IdentityCard
            : null,

        // `rr:you-your.5`: "if a card ability places a status card on 'you'
        // (such as 'you are stunned'), the player resolving that card ability
        // places that status card on their identity." `rr:you-your` opens with
        // the general form -- "if the word 'you' **can** be resolved as
        // referring to the player's identity, it **must** be resolved as such"
        // -- so "you" is a card here whenever a card is what is wanted.
        // "The player who defeated this scheme confuses their identity."
        // `rr:you-your.5` is why this answers an identity rather than a seat:
        // a status card placed on a player goes on their identity.
        AbilityCardBinding.Defeater => cast.Occurrence.Defeat is { By: >= 0 } defeated
            ? cast.World.Seats[defeated.By].IdentityCard
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' names the player who defeated a card, and no player "
                + "did"),

        // "The **activating enemy** gets +2 SCH and +2 ATK for this
        // activation." A boost card is turned faceup in the middle of an
        // activation and its own occurrence is about the boost card, so the
        // enemy is read off the board rather than off the moment --
        // `rr:activation` is what makes one answer serve an attack and a scheme
        // alike.
        AbilityCardBinding.ActivatingEnemy => cast.World.Activation is { } activating
            ? cast.World.Cards[activating.Enemy]
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' names the activating enemy, and no enemy is "
                + "activating"),

        // "After **an ally** is defeated by anything other than consequential
        // damage." The card the occurrence defeated, which is not its subject:
        // an attack keeps its participants in actor and target roles, and the
        // ally that died is a second thing the same moment did.
        AbilityCardBinding.Defeated => cast.Occurrence.Defeat is { } killed
            ? cast.World.Cards[killed.Card]
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' names the defeated card, and nothing was defeated"),

        AbilityCardBinding.You => cast.World.Seats[Resolver(cast)].IdentityCard,
        AbilityCardBinding.AttachedTo => cast.Source.Area.Host >= 0 ? cast.World.Cards[cast.Source.Area.Host] : null,
        AbilityCardBinding.TriggerSubject => cast.Occurrence.Subject >= 0
            ? cast.World.Cards[cast.Occurrence.Subject]
            : null,
        _ => throw new InvalidOperationException("Unknown compiled card binding"),
    };

    /// <summary>Cards a printed title reference denotes — <c>rr:referential-ability</c>.</summary>
    /// <remarks>
    /// The Rules Reference supplies the precedence; the set-name normalization
    /// is the engine's mapping from the vendored dataset to “associated with
    /// the same identity.” A unique title needs no tie-break. When the title is
    /// shared, self wins, then the identity family, then cards on the same side
    /// of the encounter/player boundary as the source.
    /// </remarks>
    internal static List<Card> ReferencedByTitle(string title, AbilityQueryContext cast)
    {
        bool HasPrintedTitle(Card card) => card.Faces.Any(face => string.Equals(
            cast.World.Facts.Title(face), title, StringComparison.Ordinal));

        bool ShowsTitle(Card card) => card.FaceUp
            && !FacedownDrones.Is(card)
            && string.Equals(
                cast.World.Facts.Title(card.FaceId), title, StringComparison.Ordinal);

        if (HasPrintedTitle(cast.Source))
        {
            // A self-reference continues to name its source after a cost moves
            // that card out of play; rr:initiating-abilities.3 lets it finish.
            return [cast.Source];
        }

        var matches = cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => !FacedownDrones.Is(card) && HasPrintedTitle(card))
            .ToList();
        if (matches.Count <= 1)
        {
            return [.. matches.Where(ShowsTitle)];
        }

        string associated = IdentityAssociation(
            cast.World.Facts.EncounterSet(cast.Source.FaceId));
        if (associated.Length > 0)
        {
            var sameIdentity = matches.Where(card => string.Equals(
                    IdentityAssociation(cast.World.Facts.EncounterSet(card.FaceId)),
                    associated,
                    StringComparison.Ordinal))
                .ToList();
            if (sameIdentity.Count > 0)
            {
                // The higher tier remains the reference even when its title
                // is on an inactive identity face. Form legality can make the
                // result empty; it cannot fall through to an unrelated card.
                return [.. sameIdentity.Where(ShowsTitle)];
            }
        }

        bool playerCard = IsPlayerCard(cast.World.Facts, cast.Source);
        return [.. matches.Where(card =>
            IsPlayerCard(cast.World.Facts, card) == playerCard
            && ShowsTitle(card))];
    }

    private static string IdentityAssociation(string set)
    {
        string[] suffixes =
        [
            "_nemesis",
            "_sense_deck",
            "_invocation_deck",
            "_gift_deck",
            "_labor_deck",
            "_weather_deck",
        ];
        foreach (string suffix in suffixes)
        {
            if (set.EndsWith(suffix, StringComparison.Ordinal))
            {
                return set[..^suffix.Length];
            }
        }
        return set;
    }

    /// <summary>The one card of a kind in the player's set-aside pile.</summary>
    private static Card? Aside(AbilityQueryContext cast, CardKind kind) =>
        cast.World.Seats[cast.Player].Nemesis.Cards
            .FirstOrDefault(card => cast.World.Facts.Kind(card.FaceId) == kind);

    /// <summary>
    /// Which player is resolving this ability, or a refusal.
    /// </summary>
    /// <remarks>
    /// <b>An encounter card's ability does not always have one.</b> A "When
    /// Defeated" on a minion belongs to nobody until somebody defeats it, and
    /// the cards say whose it is themselves — "the player who defeated Fabian
    /// Cortez". Until <c>Defeat</c> carries that, a card that asks for a player
    /// it has not got is refused by name rather than reaching for the first
    /// one.
    /// </remarks>
    internal static int Resolver(AbilityQueryContext cast) => cast.Player >= 0
        ? cast.Player
        : throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' asks who is resolving it, and an encounter card's "
            + "ability has no player unless the card says which");

    internal static Card ChosenPlayer(AbilityQueryContext cast) =>
        (cast.PlayerSelection ?? cast.Chosen) is { Owner: >= 0 } chosen
            ? chosen
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' asks for the chosen player before one was chosen");


    internal static bool IsPlayerCard(ICardFacts facts, Card card)
    {
        var kind = facts.Kind(card.FaceId);

        // Player side schemes are not yet a modelled kind and answer Unknown.
        // Unlike an unknown encounter card, one created in a player's deck has
        // that player as its owner, which preserves the rule's distinction.
        return kind is CardKind.AlterEgo
                or CardKind.Hero
                or CardKind.Ally
                or CardKind.Event
                or CardKind.Resource
                or CardKind.Support
                or CardKind.Upgrade
            || (kind == CardKind.Unknown && card.Owner != World.Scenario);
    }

    /// <summary>The card's current controller, falling back to its owner out of play.</summary>
    /// <remarks>
    /// <c>rr:ownership-and-control.5</c> moves a changed-control player card to
    /// its controller's play area. Ownership remains on <see cref="Card.Owner"/>,
    /// so the two facts must not be read from the same field.
    /// </remarks>
    internal static int ControllerOf(World world, Card card) =>
        IsPlayerCard(world.Facts, card)
        && DeckTypes.IsInPlay(card.Area.Type)
        && card.Area.PlayArea.IsPlayers
            ? card.Area.PlayArea.Player
            : card.Owner;


    private static IReadOnlyList<Card> InArea(World world, DeckType type, PlayArea playArea) =>
        world.Areas.FirstOrDefault(area => area.Type == type && area.PlayArea == playArea && area.Host == -1)?.Cards ?? [];
}
