using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static IEnumerable<AbilityNode> ReachableMutationBranches(
        AbilityNode conditional, Cast cast)
    {
        var test = Tree(conditional.Require("test"));
        bool canSwitch = PriorStepCanChange(test, cast)
            || cast.PaymentMayMutate && PaymentCanChange(test)
            || cast.PriorBindingMayChange && BindingCanChange(test.Argument);
        if (canSwitch)
        {
            return Branches.Select(conditional.Field)
                .Where(value => value is not null)
                .Select(value => Tree(value!));
        }
        return conditional.Field(Test(test, cast) ? "then" : "else") is { } active
            ? [Tree(active)]
            : [];
    }

    private static HashSet<DeckType> SearchAreaTypes(
        AbilityNode search, Cast cast) =>
        Nodes(search.Require("in"))
            .Select(where => Area(where.Kind, cast).Type)
            .ToHashSet();

    // MARVEL-375: remove this syntax adapter when all selector consumers take typed bindings.
    private static Card? Named(string name, Cast cast) => Named(name switch
    {
        "this" => AbilityCardBinding.This,
        "that" => AbilityCardBinding.That,
        "trigger.actor" => AbilityCardBinding.TriggerActor,
        "trigger.target" => AbilityCardBinding.TriggerTarget,
        "chosen" => AbilityCardBinding.Chosen,
        "yourHero" => AbilityCardBinding.YourHero,
        "yourAlterEgo" => AbilityCardBinding.YourAlterEgo,
        "defeater" => AbilityCardBinding.Defeater,
        "activatingEnemy" => AbilityCardBinding.ActivatingEnemy,
        "defeated" => AbilityCardBinding.Defeated,
        "you" => AbilityCardBinding.You,
        "attachedTo" => AbilityCardBinding.AttachedTo,
        "trigger.subject" => AbilityCardBinding.TriggerSubject,
        _ => throw new AbilityException($"'{name}' does not name a card"),
    }, cast);

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

    private static Card? Query(AbilityNode node, Cast cast)
    {
        // "Bomb Scare", "Vulture" -- a card in play named by its title, which
        // is a query with an argument rather than one of the bare words below.
        // `rr:identity.2` makes a title name one card, so this compares titles
        // and not printed ids.
        if (node.Kind == "titled")
        {
            var referenced = ReferencedByTitle(Word(node.Argument), cast);
            return referenced.Count switch
            {
                0 => null,
                1 => referenced[0],
                _ => throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' refers to {referenced.Count} cards titled "
                    + $"'{Word(node.Argument)}' where one card is required"),
            };
        }

        if (node.Kind != "query")
        {
            throw new AbilityException($"'{node.Kind}' does not name a card");
        }

        string what = Word(node.Argument);
        if (what == "topmostTechInChosenDiscard")
        {
            return QueryCards(AbilityCardQuery.TopmostTechInChosenDiscard, cast).SingleOrDefault();
        }

        return what switch
        {
            // `rr:villain-villain-deck` -- one villain is in the villain area.
            "villain" => QueryCards(AbilityCardQuery.Villain, cast).SingleOrDefault(),
            "mainScheme" => QueryCards(AbilityCardQuery.MainScheme, cast).SingleOrDefault(),

            // "Your set-aside nemesis minion" and "your set-aside nemesis side
            // scheme". A nemesis set holds one of each, so naming the kind
            // names the card -- and answering null when it has already been
            // taken is what Shadow of the Past's surge branch reads.
            "yourAsideMinion" => QueryCards(AbilityCardQuery.YourAsideMinion, cast).SingleOrDefault(),
            "yourAsideSideScheme" => QueryCards(AbilityCardQuery.YourAsideSideScheme, cast).SingleOrDefault(),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' queries '{what}', which is not implemented"),
        };
    }

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

    private static int Seat(AbilityValue value, Cast cast) =>
        value is AbilityValue.Word word
            ? word.Value switch
            {
                AbilityPlayers.TriggerPlayer => cast.Occurrence.Player,
                AbilityPlayers.You => Resolver(cast),
                AbilityPlayers.Controller => cast.ProjectedPlayAreaPlayer
                    ?? ControllerOf(cast.World, cast.Source),
                "chosenPlayer" => ChosenPlayer(cast).Owner,
                "engagedPlayer" => cast.ProjectedPlayAreaPlayer
                    ?? (cast.Source.Area.PlayArea.Player >= 0
                    ? cast.Source.Area.PlayArea.Player
                    : throw new RulesNotImplementedException(
                        $"'{cast.Source.FaceId}' asks for its engaged player outside a "
                        + "player's engaged area")),
                "firstPlayer" => cast.World.FirstPlayer,
                _ => throw new AbilityException($"'{word.Value}' does not name a player"),
            }
            : throw new AbilityException(
                $"{AbilityNode.Describe(value)} does not name a player");

    private static Card ChosenPlayer(Cast cast) =>
        (cast.PlayerSelection ?? cast.Chosen) is { Owner: >= 0 } chosen
            ? chosen
            : throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' asks for the chosen player before one was chosen");

    private static IEnumerable<AbilityNode> Nodes(AbilityValue value) =>
        value is AbilityValue.List list
            ? list.Values.Select(Tree)
            : throw new AbilityException(
                $"{AbilityNode.Describe(value)} is not a list of nodes");

    private static bool InspectsConcealedPile(AbilityValue value)
    {
        if (value is AbilityValue.Map map)
        {
            if (map.Entries.TryGetValue("cardsIn", out AbilityValue? argument)
                && IsConcealedCardsIn(new AbilityNode("cardsIn", argument)))
            {
                return true;
            }

            return map.Entries.Values.Any(InspectsConcealedPile);
        }

        return value is AbilityValue.List list
            && list.Values.Any(InspectsConcealedPile);
    }

    private static bool IsConcealedArea(AbilityValue? value) =>
        value is AbilityValue.Word { Value: "yourDeck" or "encounterDeck" };

    private static bool IsConcealedCardsIn(AbilityNode node) =>
        node.Kind == "cardsIn"
        && node.Argument is AbilityValue.Map fields
        && (IsConcealedArea(fields.Entry("area"))
            || fields.Entry("areas") is AbilityValue.List areas
            && areas.Values.Any(IsConcealedArea));

    private static AbilityNode Tree(AbilityValue value) => AbilityNode.Of(value);

    private static string Word(AbilityValue value) =>
        value is AbilityValue.Word word
            ? word.Value
            : throw new AbilityException($"{AbilityNode.Describe(value)} is not a word");

    /// <summary>How much, which may be printed per player.</summary>
    /// <remarks>
    /// <c>rr:per-player-icon</c> multiplies by the number of players, and
    /// <c>rr:player-elimination.6</c> is the exception that keeps this
    /// <c>World.Players</c> rather than the number still playing: "effects that
    /// refer to the players in the game ignore eliminated players, <b>except
    /// for the per player icon</b>."
    /// </remarks>
    private static long Amount(AbilityValue value, Cast cast)
    {
        if (value is not AbilityValue.Map)
        {
            return Number(value);
        }

        var node = Tree(value);
        return node.Kind switch
        {
            "perPlayer" => Number(node.Argument) * cast.World.Players,

            // "X is the amount of threat on Bomb Scare" -- a number read off
            // the board rather than printed. `rr:threat` counts tokens, so this
            // is the token pool and not a printed field.
            "tokensOn" => Find(node.Argument, cast) is { } holder
                ? holder.Tokens.GetValueOrDefault("k_threat")
                : 0,

            // `result.*` -- what an action earlier in this ability actually
            // did, which is not what it was asked to do. Zero when nothing has
            // written it, so a card reading a result it never produced reads a
            // number rather than throwing: "no damage was healed" is exactly
            // the case where nothing ran.
            "result" => cast.Results.GetValueOrDefault(Word(node.Argument)),

            // "If there is at least 5 damage here" -- damage tokens on a card,
            // which `rr:damage.2` puts on an ally or minion and which an
            // attachment can hold when a card puts them there.
            "damageOn" => Find(node.Argument, cast)?.Damage ?? 0,
            "powerAmount" => cast.PowerAmount,
            "countersOn" => Find(node.Require("card"), cast) is { } counterHolder
                ? CounterCount(counterHolder, Word(node.Require("counter")))
                : 0,
            "printedResourceCountDiscarded" => Resources.PrintedCount(
                cast.Discarded, Word(node.Argument)[0], cast.World.Facts),
            "printedBoostIconsDiscarded" => cast.Discarded.Sum(card =>
                cast.World.Facts.PrintedValue(card.FaceId, "Boost", cast.World.Players)),
            // The binding's spelling is the engine's choice. The printed card
            // names what was "discarded this way," whose identity survives an
            // immediate encounter-deck reset even when the discard pile does not.
            "topEncounterDiscardBoostPlusOne" => 1 + (cast.Discarded.LastOrDefault() is { } card
                ? cast.World.Facts.PrintedValue(card.FaceId, "Boost", cast.World.Players)
                : 0),
            "remainingHealth" => Find(node.Argument, cast) is { } remaining
                ? Math.Max(
                    0,
                    Damage.Health(cast.World, cast.World.Facts, remaining) - remaining.Damage)
                : 0,
            // `rr:hit-points.1`: "starting hit points" means the identity's
            // printed hit point value. It deliberately excludes attachments,
            // constant abilities, and lasting effects that raise its dial.
            "startingHealth" => StartingHealth(node.Argument, cast),
            "if" => Test(Tree(node.Require("test")), cast)
                ? Amount(node.Require("then"), cast)
                : node.Field("else") is { } otherwise
                    ? Amount(otherwise, cast)
                    : 0,
            "count" => Every(node.Argument, cast).Count,
            "discardedWithResource" => cast.Discarded.Count(card =>
                Resources.GeneratedBy(card.FaceId, cast.World.Facts).Contains(
                    Word(node.Argument), StringComparison.Ordinal)),
            "modified" => Find(node.Require("card"), cast) is { } modified
                ? StateFields.Modified(
                    cast.World, modified, Word(node.Require("field")),
                    cast.World.Facts, cast.World.Players)
                : 0,
            "min" => Values(node.Argument).Select(each => Amount(each, cast)).Min(),
            "add" => Values(node.Argument).Sum(each => Amount(each, cast)),
            "mul" => Values(node.Argument).Aggregate(1L, (product, each) =>
                product * Amount(each, cast)),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' asks for the amount '{node.Kind}', "
                + "which is not implemented"),
        };
    }

    private static long StartingHealth(AbilityValue value, Cast cast)
    {
        if (Find(value, cast) is not { } identity)
        {
            return 0;
        }

        return StartingHealth(identity, cast);
    }

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

    private static long Number(AbilityValue value) =>
        value is AbilityValue.Number number
            ? number.Value
            : throw new AbilityException($"{AbilityNode.Describe(value)} is not a number");

    private static IReadOnlyList<AbilityValue> Values(AbilityValue value) =>
        value is AbilityValue.List list
            ? list.Values
            : throw new AbilityException($"{AbilityNode.Describe(value)} is not a list");

}
