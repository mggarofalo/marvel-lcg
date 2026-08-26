using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>
/// Runs authored card abilities. The one way a card's text enters the engine.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is what replaces a class per card.</b> There was one — a switch on
/// printed id, three cards deep and growing — and it was the "cards as scripts"
/// inversion this port exists to undo (<c>docs/migration.md</c>). A card is now
/// a row in <c>datasets/abilities/abilities.json</c>, and adding one is
/// authoring data rather than compiling code.
/// </para>
/// <para>
/// The vocabulary is small and every gap is loud. A node nothing implements
/// throws naming the node; a card nobody has authored throws naming the card.
/// Growing the engine means adding a case here and growing the game means adding
/// a row there, and the two are different activities on purpose.
/// </para>
/// <para>
/// See <c>docs/card-dsl.md</c> for the design this is the first executable
/// piece of, and <c>docs/enemy-attacks.md</c> for the cards it currently runs.
/// </para>
/// </remarks>
/// <param name="book">The authored cards.</param>
public sealed class AbilityRunner(AbilityBook book) : ICardAbilities
{
    private readonly AbilityBook book = book;

    /// <summary>The authored cards, whether or not they do anything.</summary>
    public IReadOnlySet<string> Authored => book.Authored;

    /// <inheritdoc/>
    public IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);

        var waiting = new List<PendingAbility>();
        foreach (var card in world.Cards)
        {
            // `rr:ability.1` -- a card's ability functions while the card is in
            // play. Being attached to something is not the same as being in
            // play: the recorded Tough hangs off Rhino from a zone that is not.
            if (!DeckTypes.IsInPlay(card.Area.Type))
            {
                continue;
            }

            foreach (var ability in book.On(card.FaceId))
            {
                if (Answers(ability, card, occurrence, window))
                {
                    // The controller is the card's owner rather than anything
                    // the data says: `rr:ability.8` lets any player use an
                    // optional ability on an encounter card, and an encounter
                    // card is one the scenario owns.
                    waiting.Add(new PendingAbility(
                        card.ObjectId, ability.Trigger.Timing, card.Owner));
                }
            }
        }

        return waiting;
    }

    /// <inheritdoc/>
    public Affordance Describe(World world, PendingAbility ability)
    {
        ArgumentNullException.ThrowIfNull(world);

        var card = world.Cards[ability.Card];
        var found = book.On(card.FaceId)
            .FirstOrDefault(candidate => candidate.Trigger.Timing == ability.Type)
            ?? throw new AbilityException(
                $"card '{card.FaceId}' has no '{ability.Type}' ability to describe");

        // The ability's own name is the verb, which is the engine's convention:
        // `datasets/digest/prompts.json` offers `Foresight` and `"I_Object!"`,
        // both card names. One string does for both fields because the engine
        // carries one -- see the remarks on `Affordance.Id`.
        return new Affordance(
            Id: ability.Card,
            Verb: found.Name,
            AnchorId: ability.Card,
            AnchorPlayer: ability.Player,
            Label: found.Name);
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);

        var card = world.Cards[ability.Card];
        var found = book.On(card.FaceId)
            .Where(candidate => candidate.Trigger.Timing == ability.Type)
            .ToList();

        if (found.Count != 1)
        {
            // Two abilities of one type on one card cannot be told apart by a
            // `PendingAbility`, which names a card and a tier. A card that needs
            // it needs the pending ability to carry which one, and that is a
            // change to make when a card demands it rather than now.
            throw new AbilityException(
                $"card '{card.FaceId}' has {found.Count} '{ability.Type}' abilities, "
                + "and exactly one can be resolved from a window");
        }

        var events = new List<GameEvent>();
        Run(found[0].Effect, new Cast(world, card, occurrence, ability.Player, events, this));
        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        if (!book.Authored.Contains(card.FaceId))
        {
            // Authored-and-does-nothing is a different thing from nobody having
            // read the card, and only one of them is safe to treat as silence.
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was revealed and no ability data is written for it; "
                + $"this engine has {book.Authored.Count} authored card(s)");
        }

        var events = new List<GameEvent>();

        // `rr:reveal` is the occurrence; the card is not in play while it
        // resolves, which is why this does not go through `Waiting`.
        var occurrence = new Occurrence(
            0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player);

        foreach (var ability in book.On(card.FaceId))
        {
            // `rr:ability.step.3` -- "When Revealed" *is* the occurrence, not a
            // window around it. An interrupt or a response to a card being
            // revealed is a different ability and reaches the board through
            // `Waiting`, so matching on the condition alone would run it twice.
            if (ability.Trigger.Timing == AbilityType.WhenRevealed
                && string.Equals(ability.Trigger.Event, Steps.CardRevealed, StringComparison.Ordinal))
            {
                Run(ability.Effect, new Cast(world, card, occurrence, player, events, this));
            }
        }

        return events;
    }

    /// <summary>Whether one ability answers this occurrence, in this window.</summary>
    private static bool Answers(
        CardAbility ability, Card card, Occurrence occurrence, WindowKind window)
    {
        if (!occurrence.Is(ability.Trigger.Event))
        {
            return false;
        }

        bool belongs = window switch
        {
            WindowKind.Interrupt => AbilityTypes.IsInterrupt(ability.Trigger.Timing),
            WindowKind.Response => AbilityTypes.IsResponse(ability.Trigger.Timing),
            _ => false,
        };

        return belongs && Subject(ability.Trigger.Subject, card, occurrence);
    }

    private static bool Subject(string subject, Card card, Occurrence occurrence) => subject switch
    {
        AbilitySubjects.This => occurrence.Subject == card.ObjectId,
        AbilitySubjects.AttachedTo => card.Area.Host >= 0 && occurrence.Subject == card.Area.Host,
        AbilitySubjects.You => occurrence.Player >= 0 && occurrence.Player == card.Owner,
        _ => throw new AbilityException($"'{subject}' is not a subject anything matches"),
    };

    // ---- the effect tree ---------------------------------------------------

    private static void Run(AbilityNode node, Cast cast)
    {
        switch (node.Kind)
        {
            case "seq":
                foreach (var step in Nodes(node.Argument))
                {
                    Run(step, cast);
                }

                break;

            case "if":
                var branch = Test(Tree(node.Require("test")), cast) ? "then" : "else";
                if (node.Field(branch) is { } taken)
                {
                    Run(Tree(taken), cast);
                }

                break;

            case "giveStatus":
                GiveStatus(node, cast);
                break;

            case "attachTo":
                AttachTo(node, cast);
                break;

            case "grantUntil":
                GrantUntil(node, cast);
                break;

            case "delayUntil":
                DelayUntil(node, cast);
                break;

            case "discard":
                Discard(node, cast);
                break;

            case "gainSurge":
                // `rr:surge`: "the player resolving the card deals themself a
                // facedown encounter card from the top of the encounter deck",
                // and `.1` writes it as "**When Revealed**: deal yourself 1
                // facedown encounter card". A card that *gains* surge does the
                // same thing the keyword would have -- so this is one deal, and
                // the number beside the node is how many.
                //
                // `.2` finishes the original card first, which the villain
                // phase's reveal queue does without anything here.
                for (long dealt = 0; dealt < Number(node.Argument); dealt++)
                {
                    Deal.EncounterCard(
                        cast.World, cast.Player, cast.Occurrence.Conditions[0], cast.Events);
                }

                break;

            case "dealDamage":
                DealDamage(node, cast);
                break;

            case "placeThreat":
                PlaceThreat(node, cast);
                break;

            case "enemyAttacks":
                Activate(node, cast, Steps.Attack);
                break;

            case "enemySchemes":
                Activate(node, cast, Steps.Scheme);
                break;

            case "draw":
                Draw.Cards(
                    cast.World, Seat(node.Require("player"), cast),
                    (int)Number(node.Require("count")),
                    cast.Occurrence.Conditions[0], cast.Events);
                break;

            default:
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' uses the effect node '{node.Kind}', "
                    + "which is not implemented");
        }
    }

    private static bool Test(AbilityNode node, Cast cast) => node.Kind switch
    {
        "and" => Nodes(node.Argument).All(each => Test(each, cast)),
        "or" => Nodes(node.Argument).Any(each => Test(each, cast)),
        "not" => !Test(Tree(node.Argument), cast),
        "exists" => Find(node.Argument, cast) is not null,

        // `rr:form` -- "(Hero)" and "(Alter-Ego)" on a card gate the ability by
        // which form the player is in. Not a boolean: `Forms.Of` answers with a
        // set, because a hero can print more than two faces.
        "inForm" => Forms.In(
            cast.World,
            cast.World.Seats[Seat(node.Require("player"), cast)],
            cast.World.Facts,
            Word(node.Require("form"))),

        "hasStatus" => Find(node.Require("card"), cast) is { } host
            && Statuses.Has(cast.World, host, Word(node.Require("status"))),
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' uses the test node '{node.Kind}', "
            + "which is not implemented"),
    };

    private static void GiveStatus(AbilityNode node, Cast cast)
    {
        var host = Find(node.Require("card"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would give a status to a card that is not there");

        string what = Word(node.Require("status"));

        // Through the rules rather than straight at `Statuses.Give`:
        // `rr:status-cards.1` caps how many a character can hold,
        // `rr:stalwart` makes that cap zero, and `rr:vulnerable` discards the
        // character. A card giving a status does not get to skip any of them.
        var status = Reveal.Afflict(
            cast.World, cast.World.Facts, host, what, cast.Trigger, cast.Events);
        if (status is null)
        {
            return;
        }

        cast.Events.Add(new CardAttached(status.ObjectId, host.ObjectId)
        {
            Trigger = cast.Trigger, Verb = "Give_Status",
        });
    }

    // `rr:attachment` -- "when an attachment enters play, it attaches to another
    // card or game element".
    private static void AttachTo(AbilityNode node, Cast cast)
    {
        var host = Find(node.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' attaches to a card that is not there");

        var onto = cast.World.AreaOf(
            DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        var from = cast.Source.Area;
        World.MoveToTop(cast.Source, onto);

        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(onto),
            [new Landing(cast.Source.ObjectId, onto.Cards.Count - 1)])
        {
            Trigger = cast.Trigger, Verb = "Attach",
        });
        cast.Events.Add(new CardAttached(cast.Source.ObjectId, host.ObjectId)
        {
            Trigger = cast.Trigger, Verb = "Attach",
        });
    }

    // `rr:lasting-effects` -- an effect "for a specified duration (such as
    // [...] 'until the end of this attack')".
    private static void GrantUntil(AbilityNode node, Cast cast)
    {
        var target = Find(node.Require("card"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would grant to a card that is not there");

        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: Word(node.Require("keyword")),
            Amount: node.Field("amount") is { } amount ? Number(amount) : 0,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.UntilEndOf(Word(node.Require("until")))));
    }

    // `rr:delayed-effect.1` -- an effect that resolves "after their specified
    // timing point or future condition occurs or becomes true".
    private static void DelayUntil(AbilityNode node, Cast cast)
    {
        var effect = Tree(node.Require("effect"));
        if (effect.Kind != "discard")
        {
            // A delayed effect is data on the board, not a closure, so what it
            // will do has to be a `Kind` the engine can read back after a save.
            // `DelayedEffects` knows one; the rest is the vocabulary that grows.
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' delays '{effect.Kind}', and only 'discard' can be "
                + "written down as a delayed effect");
        }

        var target = Find(effect.Field("card") ?? effect.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would delay a discard of a card that is not there");

        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect,
            Kind: DelayedEffects.DiscardFromPlay,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.NextTime(Word(node.Require("condition")))));
    }

    private static void Discard(AbilityNode node, Cast cast)
    {
        var target = Find(node.Field("card") ?? node.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would discard a card that is not there");

        Rules.Play.Discard.Card(cast.World, target, cast.Trigger, cast.Events);
    }

    // ---- reading a value ---------------------------------------------------

    /// <summary>"Deal N damage to …" — <c>rr:damage</c>.</summary>
    /// <remarks>
    /// Through <see cref="Damage.Deal"/> and not at the token, because damage
    /// is one rule however it arrived: <c>rr:tough.2</c> prevents all of it and
    /// discards a status card instead, and <c>rr:defeat</c> is the other half
    /// of the same moment. A card that wrote to <c>k_damage</c> would skip
    /// both and leave a defeated character standing.
    /// </remarks>
    private static void DealDamage(AbilityNode node, Cast cast)
    {
        long amount = Amount(node.Require("amount"), cast);
        foreach (var target in Every(node.Require("cards"), cast))
        {
            Damage.Deal(
                cast.World, cast.World.Facts, target, amount, cast.Trigger, "Deal_Damage",
                cast.Events);
        }
    }

    /// <summary>"Place N threat on …" — <c>rr:threat</c>.</summary>
    /// <remarks>
    /// Through <see cref="Threat.Place"/>, which checks
    /// <c>rr:main-scheme-main-scheme-deck.2</c> afterwards: threat that reaches
    /// a main scheme's target completes it whatever put it there, and a card
    /// placing threat is one of the things that can.
    /// </remarks>
    private static void PlaceThreat(AbilityNode node, Cast cast)
    {
        var scheme = Find(node.Require("scheme"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would place threat on a scheme that is not there");

        Threat.Place(
            cast.World, cast.World.Facts, cast.Abilities, scheme,
            Amount(node.Require("amount"), cast), cast.Trigger, cast.Events);
    }

    /// <summary>
    /// "The villain attacks you", "the villain schemes" — an enemy activation
    /// a card asked for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scheduled, not called.</b> <c>rr:attack-enemy-activation</c> is six
    /// steps and one of them asks a player who is defending, so an activation
    /// cannot resolve inside an ability that has to return. It goes on the
    /// agenda, and <c>Agenda.Then</c> puts it after the step that is running —
    /// which is what <c>rr:surge.2</c> wants anyway: finish resolving the card
    /// before what it caused happens.
    /// </para>
    /// <para>
    /// <b>Which activation is the card's to say.</b> <c>rr:activation.1</c>
    /// reads it off the player's form — attack in hero form, scheme in
    /// alter-ego form — but that rule is about the activation the villain phase
    /// schedules. A card that says "the villain attacks you" has already
    /// chosen, and reading the form here would make Assault do nothing to a
    /// hero who had flipped since the card was dealt.
    /// </para>
    /// <para>
    /// One step per enemy, in the order <see cref="Every"/> returns them.
    /// <c>rr:minion.3</c> makes that order the player's choice; it is taken
    /// here as the order the minions sit in the play area, deterministically
    /// and stated, exactly as the villain phase's own step 2 takes it.
    /// </para>
    /// </remarks>
    private static void Activate(AbilityNode node, Cast cast, string what)
    {
        // Against the player resolving the card. Every printed card that causes
        // an activation says "you", and `rr:reveal.2` makes that the revealing
        // player -- so there is no field here to name somebody else, and a card
        // that names one grows the vocabulary then rather than leaving an
        // untaken branch now.
        int seat = cast.Player;

        // The round the activation belongs to is the round the card was
        // revealed in. Nothing else on the agenda can tell it.
        int round = cast.World.Agenda.Current?.Round ?? 0;

        foreach (var enemy in Every(node.Require("enemies"), cast))
        {
            cast.World.Agenda.Then(new PhaseStep(
                what, round, 2, Index: seat, Subject: enemy.ObjectId, Seat: seat));
        }
    }

    /// <summary>Every card a value names, which may be none.</summary>
    /// <remarks>
    /// A value that names one card answers with that one, so a card reading
    /// "the villain attacks you" and one reading "each minion engaged with you
    /// attacks you" are the same node with a different argument.
    /// </remarks>
    private static IReadOnlyList<Card> Every(AbilityValue value, Cast cast)
    {
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

        return Find(value, cast) is { } one ? [one] : [];
    }

    /// <summary>Which card a value names, or null when it names none.</summary>
    private static Card? Find(AbilityValue value, Cast cast) => value switch
    {
        AbilityValue.Word word => Named(word.Value, cast),
        AbilityValue.Map => Query(Tree(value), cast),
        _ => throw new AbilityException($"{AbilityNode.Describe(value)} does not name a card"),
    };

    private static Card? Named(string name, Cast cast) => name switch
    {
        "this" => cast.Source,
        "attachedTo" => cast.Source.Area.Host >= 0 ? cast.World.Cards[cast.Source.Area.Host] : null,
        "trigger.subject" => cast.Occurrence.Subject >= 0
            ? cast.World.Cards[cast.Occurrence.Subject]
            : null,
        _ => throw new AbilityException($"'{name}' does not name a card"),
    };

    private static Card? Query(AbilityNode node, Cast cast)
    {
        if (node.Kind != "query")
        {
            throw new AbilityException($"'{node.Kind}' does not name a card");
        }

        string what = Word(node.Argument);
        return what switch
        {
            // `rr:villain-villain-deck` -- one villain is in the villain area.
            "villain" => cast.World.TheCardIn(DeckType.VillainArea),
            "mainScheme" => cast.World.TheCardIn(DeckType.MainSchemesArea),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' queries '{what}', which is not implemented"),
        };
    }

    private static int Seat(AbilityValue value, Cast cast) =>
        value is AbilityValue.Word word
            ? word.Value switch
            {
                "trigger.player" => cast.Occurrence.Player,
                "you" => cast.Player,
                "controller" => cast.Source.Owner,
                _ => throw new AbilityException($"'{word.Value}' does not name a player"),
            }
            : throw new AbilityException(
                $"{AbilityNode.Describe(value)} does not name a player");

    private static IEnumerable<AbilityNode> Nodes(AbilityValue value) =>
        value is AbilityValue.List list
            ? list.Values.Select(Tree)
            : throw new AbilityException(
                $"{AbilityNode.Describe(value)} is not a list of nodes");

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
    private static long Amount(AbilityValue value, Cast cast) =>
        value is AbilityValue.Map && Tree(value) is { Kind: "perPlayer" } per
            ? Number(per.Argument) * cast.World.Players
            : Number(value);

    private static long Number(AbilityValue value) =>
        value is AbilityValue.Number number
            ? number.Value
            : throw new AbilityException($"{AbilityNode.Describe(value)} is not a number");

    /// <summary>What one ability is resolving against.</summary>
    /// <param name="World">The board.</param>
    /// <param name="Source">The card whose text this is.</param>
    /// <param name="Occurrence">What it is timed to.</param>
    /// <param name="Player">The seat resolving it.</param>
    /// <param name="Events">Where to record what it did.</param>
    /// <param name="Abilities">
    /// The runner itself, for the rules that run more cards. A main scheme this
    /// ability completes advances, and <c>rr:villain-defeat</c> resolves the
    /// new stage's own "When Revealed" — so an ability can reach back into the
    /// interpreter that is running it.
    /// </param>
    private sealed record Cast(
        World World, Card Source, Occurrence Occurrence, int Player, List<GameEvent> Events,
        ICardAbilities Abilities)
    {
        /// <summary>The trigger string this ability's events carry.</summary>
        public string Trigger => Occurrence.Conditions[0];
    }
}
