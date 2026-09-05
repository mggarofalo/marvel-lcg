using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static IEnumerable<AbilityEffect> ReachableMutationBranches(
        AbilityEffect conditional, Cast cast)
    {
        var test = ConditionalOf(conditional, cast).Test;
        bool canSwitch = PriorStepCanChange(test, cast)
            || cast.PaymentMayMutate && PaymentCanChange(test)
            || cast.PriorBindingMayChange && BindingCanChange(test);
        if (canSwitch)
        {
            return ConditionalBranches((AbilityEffect.Conditional)conditional)
                .Where(value => value is not null)
                .Select(value => value);
        }
        return ConditionalBranch(conditional, Test(test, cast) ? "then" : "else") is { } active
            ? [active]
            : [];
    }

    private static HashSet<DeckType> SearchAreaTypes(
        AbilityEffect search, Cast cast) =>
        EffectOf<AbilityEffect.Search>(search, cast).Areas
            .Select(where => Area(where, cast).Type)
            .ToHashSet();

    private static Card? Named(AbilityCardBinding name, Cast cast) => name switch
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
    private static List<Card> ReferencedByTitle(string title, Cast cast)
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
    private static Card? Aside(Cast cast, CardKind kind) =>
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
    private static int Resolver(Cast cast) => cast.Player >= 0
        ? cast.Player
        : throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' asks who is resolving it, and an encounter card's "
            + "ability has no player unless the card says which");

    private static Card ChosenPlayer(Cast cast) =>
        (cast.PlayerSelection ?? cast.Chosen) is { Owner: >= 0 } chosen
            ? chosen
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' asks for the chosen player before one was chosen");

    private static long StartingHealth(Card identity, Cast cast)
    {
        if (FacedownDrones.Kind(identity, cast.World.Facts)
            is not (CardKind.Hero or CardKind.AlterEgo))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' asks for starting hit points of "
                + $"non-identity card {identity.ObjectId}");
        }

        return FacedownDrones.BaseValue(
            identity, cast.World.Facts, "HP", cast.World.Players);
    }


}
