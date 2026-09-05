using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static IReadOnlyList<Card> QueryCards(AbilityCardQuery query, Cast cast)
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
                return [.. cast.World
                    .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(cast.Player))
                    .Cards];
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
                return [.. cast.World.AreaOf(DeckType.SideSchemesArea).Cards];
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
                        .Where(enemy => cast.World.Abilities.CanTakeDamage(
                            cast.World, enemy, cast.Source)),
                ];
            }
            case AbilityCardQuery.AttackableMinions:
            {
                return
                [
                    .. BasicPowers.Attackable(cast.World, cast.World.Facts, Resolver(cast))
                        .Where(enemy => FacedownDrones.Kind(
                            enemy, cast.World.Facts) == CardKind.Minion)
                        .Where(enemy => cast.World.Abilities.CanTakeDamage(
                            cast.World, enemy, cast.Source)),
                ];
            }
            case AbilityCardQuery.Schemes:
            {
                return
                [
                    .. cast.World.AreaOf(DeckType.MainSchemesArea).Cards,
                    .. cast.World.AreaOf(DeckType.SideSchemesArea).Cards,
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
                        .Where(area => Owned.Contains(area.Type)
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
                return [.. cast.World.AreaOf(DeckType.SupportsArea, PlayArea.Of(cast.Player)).Cards];
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
                    .. cast.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(cast.Player)).Cards,
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
                    .. cast.World.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(player)).Cards,
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
                return [.. cast.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(cast.Player)).Cards];
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
                        .Where(player => cast.World.AreaOf(
                                DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player)
                            .Cards.Any(card => Rules.State.Traits.Has(
                                cast.World, card, "TECH", cast.World.Facts)))
                        .Select(player => cast.World.Seats[player].IdentityCard),
                ];
            }
            case AbilityCardQuery.TopmostTechInChosenDiscard:
            {
                int player = ChosenPlayer(cast).Owner;
                var card = cast.World.AreaOf(
                        DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player)
                    .Cards.LastOrDefault(candidate => Rules.State.Traits.Has(
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
                return [.. cast.World.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(Resolver(cast))).Cards
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
}
