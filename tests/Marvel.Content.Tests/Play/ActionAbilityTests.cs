using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Triggering an "Action" ability — <c>rr:player-turn.5</c>.
/// </summary>
/// <remarks>
/// <para>
/// The fifth of the six things a turn offers, and until now the engine had
/// none of it. <b>966 cards in the pool print an Action ability</b> — 445 of
/// them events, which are reached only this way: <c>rr:player-turn.5.d</c>
/// plays an event "by playing that event", and <c>.2</c>'s list of what may be
/// played from hand does not include events at all.
/// </para>
/// <para>
/// <b>An action is not in a window.</b> It happens because a player says so on
/// their turn, so it is offered beside the basic powers rather than in an
/// interrupt or a response — which is why <c>AbilityTypes.PriorityOf</c>
/// refuses to give it a tier.
/// </para>
/// </remarks>
public sealed partial class ActionAbilityTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:form-change-form.4")]
    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public void AlterEgoReferencesOnlyAffectAnIdentityInAlterEgoForm(
        bool hero, int remainingDamage)
    {
        // "While a player is in hero form, card abilities that interact with
        // their alter-ego do not interact with their identity." The explicit
        // alter-ego selector therefore has no target in hero form and names the
        // same physical identity card after it changes to alter-ego form.
        var runner = Runner(
            "01017",
            "Action",
            """{ "heal": { "card": "yourAlterEgo", "amount": 1 } }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(2);
            },
            hero: hero,
            abilities: runner);
        var action = game.Pending!.Affordances.SingleOrDefault(
            option => option.AnchorId == source!.ObjectId);
        if (hero)
        {
            Assert.Null(action);
        }
        else
        {
            game.Resolve(Decision.Take(Assert.IsType<Affordance>(action).Id));
        }

        Assert.Equal(remainingDamage, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:support.2")]
    [Fact]
    public void AnActionIsOfferedOnTheTurnAndDoesWhatItSays()
    {
        // "Alter-Ego Action: Exhaust Aunt May → heal 4 damage from Peter
        // Parker." The whole path: offered among the turn options, taken, cost
        // paid, effect resolved, and the turn goes on. A support is "active
        // while it is in play", which is why Aunt May offers that action.
        Card? may = null;
        var (game, world) = Playing(board =>
        {
            may = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(5);
        });

        var identity = world.Seats[0].IdentityCard;

        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
        Assert.Equal(may!.ObjectId, action.AnchorId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(1, identity.Damage);
        Assert.False(may!.Ready);

        // `rr:player-turn`: every option but changing form may be taken "as
        // many times as the player is able", so the turn is still going.
        Assert.NotNull(game.Pending);
        Assert.Equal(Question.TurnOption, game.Pending.Asking);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.3")]
    [Rule("rr:damage.step.7")]
    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void ASourceDefeatedByItsActionCostStillFinishesThatAction()
    {
        // "Action: Exhaust War Machine and deal 2 damage to him → deal 1
        // damage to each enemy." The cost is paid before the effect. At two
        // remaining hit points that cost defeats and discards War Machine, but
        // rr:initiating-abilities.3 says leaving play does not stop the
        // sequence, and the post-arrow effect still deals damage to every
        // enemy.
        Card? warMachine = null;
        var (game, world) = Playing(board =>
        {
            warMachine = board.CreateCard(
                "01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            warMachine.TakeDamage(2);
        });
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == warMachine!.ObjectId);
        var resolved = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.DiscardPile, warMachine!.Area.Type);
        Assert.Equal(1, villain.Damage);
        Assert.All(resolved.Events, happened => Assert.Equal(Steps.TurnAction, happened.Trigger));

        int discarded = resolved.Events
            .Select((happened, index) => (happened, index))
            .First(pair => pair.happened is CardsMoved moved
                && moved.Cards.Any(card => card.Card == warMachine.ObjectId))
            .index;
        int damaged = resolved.Events
            .Select((happened, index) => (happened, index))
            .First(pair => pair.happened is FieldSet changed
                && changed.Card == villain.ObjectId
                && changed.Field == "health")
            .index;
        Assert.True(discarded >= 0);
        Assert.True(damaged > discarded);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Rule("rr:cost.11")]
    [Fact]
    public void DealingDamageAsACostIsPaidWhenTheDamageIsPrevented()
    {
        // Focused Rage deals one damage as its cost. Tough prevents all of
        // that damage, but dealing damage is still considered paid and the
        // post-arrow draw resolves.
        Card? rage = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01019a");
                rage = board.CreateCard(
                    "01027",
                    board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
            },
            heroes: ["she_hulk"]);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == rage!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:cost.12")]
    [Fact]
    public void TakingDamageAsACostIsUnpaidWhenDamageIsPrevented()
    {
        // A take-damage cost "is not considered paid unless all of that
        // damage was taken." Preventing even part of it therefore suppresses
        // the post-arrow draw.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost:
            """
            { "takeDamage": { "cards": "you", "amount": 2 } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "preventDamage",
            Amount: 1,
            Card: source!.ObjectId,
            Affects: world.Seats[0].IdentityCard.ObjectId,
            Lasts: new Duration(Uses: 1)));

        var failure = Assert.Throws<RulesNotImplementedException>(() =>
            runner.Act(world, action, [], []));

        Assert.Contains("only 1 was taken", failure.Message);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:cost.12")]
    [Fact]
    public void TakingAllDamagePaysTheCost()
    {
        // The cost is paid when "all of that damage was taken," so the effect
        // after the arrow resolves.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost:
            """
            { "takeDamage": { "cards": "you", "amount": 2 } }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.Act(world, action, [], []);

        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:event.5")]
    [Fact]
    public void AnEventDamageModifierAppliesToEveryDamageInstance()
    {
        // When an event "deals multiple instances of damage, each of those
        // instances is modified." Two one-damage instances with +2 each deal
        // six, not four.
        var runner = Runner(
            "01005",
            "Action",
            """
            { "seq": [
              { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } },
              { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }
            ] }
            """);
        Card? played = null;
        var payments = new List<Card>();
        var (_, world) = Playing(
            board =>
            {
                played = board.CreateCard("01005", board.Seats[0].Hand);
                for (int index = 0; index < 3; index++)
                {
                    payments.Add(board.CreateCard("01087", board.Seats[0].Hand));
                }
            },
            hero: true,
            abilities: runner);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "eventDamage", Amount: 2,
            Card: played!.ObjectId, Affects: played.ObjectId));
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == played.ObjectId);

        runner.Act(world, action, [.. payments.Select(card => card.ObjectId)], []);

        Assert.Equal(6, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:event.5")]
    [Fact]
    public void AnEventThreatModifierAppliesToEveryRemovalInstance()
    {
        // The same clause says that when an event "removes multiple instances
        // of threat, each of those instances is modified."
        var runner = Runner(
            "01005",
            "Action",
            """
            { "seq": [
              { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } },
              { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }
            ] }
            """);
        Card? played = null;
        var payments = new List<Card>();
        var (_, world) = Playing(
            board =>
            {
                board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 10);
                played = board.CreateCard("01005", board.Seats[0].Hand);
                for (int index = 0; index < 3; index++)
                {
                    payments.Add(board.CreateCard("01087", board.Seats[0].Hand));
                }
            },
            hero: true,
            abilities: runner);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "eventThreatRemoval", Amount: 2,
            Card: played!.ObjectId, Affects: played.ObjectId));
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == played.ObjectId);

        runner.Act(world, action, [.. payments.Select(card => card.ObjectId)], []);

        Assert.Equal(
            4,
            world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:attack-player-ability-type.3")]
    [Rule("rr:attack-player-ability-type.3.1")]
    [Rule("rr:event.5.1")]
    [Fact]
    public void AnAttackModifierAppliesOnlyToAnEventsFirstAttack()
    {
        // Each listed damage instance is a separate attack. A modifier to one
        // attack therefore increases only the first: it deals three and the
        // second deals one.
        var runner = Runner(
            "01005",
            "Action",
            """
            { "seq": [
              { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } },
              { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } }
            ] }
            """);
        Card? played = null;
        var payments = new List<Card>();
        var (_, world) = Playing(
            board =>
            {
                played = board.CreateCard("01005", board.Seats[0].Hand);
                for (int index = 0; index < 3; index++)
                {
                    payments.Add(board.CreateCard("01087", board.Seats[0].Hand));
                }
            },
            hero: true,
            abilities: runner);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attackDamage", Amount: 2,
            Card: played!.ObjectId, Affects: played.ObjectId,
            Lasts: new Duration(Uses: 1)));
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == played.ObjectId);

        runner.Act(world, action, [.. payments.Select(card => card.ObjectId)], []);

        Assert.Equal(4, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.3.1")]
    [Rule("rr:event.5.1")]
    [Fact]
    public void AnAttackModifierIsConsumedAtTheAttackWrapperBoundary()
    {
        // Each wrapper is one attack even when its effect uses generic damage.
        // The modifier remains through the first wrapper and is gone before
        // the second attack begins.
        var runner = Runner(
            "01005",
            "Action",
            """
            { "seq": [
              { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } },
              { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } }
            ] }
            """);
        Card? played = null;
        var payments = new List<Card>();
        var (game, world) = Playing(
            board =>
            {
                played = board.CreateCard("01005", board.Seats[0].Hand);
                for (int index = 0; index < 3; index++)
                {
                    payments.Add(board.CreateCard("01087", board.Seats[0].Hand));
                }
            },
            hero: true,
            abilities: runner);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attackDamage", Amount: 2,
            Card: played!.ObjectId, Affects: played.ObjectId,
            Lasts: new Duration(Uses: 1)));
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.AnchorId == played.ObjectId);

        game.Resolve(Decision.Take(
            action.Id, [], [.. payments.Select(card => card.ObjectId)]));

        Assert.Equal(4, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.2.2")]
    [Rule("rr:event.5.1")]
    [Fact]
    public void EveryDamageInstanceInsideTheFirstAttackIsModified()
    {
        // A labelled attack remains one attack across multiple damage
        // instances, and an increase applies to each instance that does not
        // say "additional". Both one-damage nodes therefore become three.
        var runner = Runner(
            "01005",
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """);
        Card? played = null;
        var payments = new List<Card>();
        var (game, world) = Playing(
            board =>
            {
                played = board.CreateCard("01005", board.Seats[0].Hand);
                for (int index = 0; index < 3; index++)
                {
                    payments.Add(board.CreateCard("01087", board.Seats[0].Hand));
                }
            },
            hero: true,
            abilities: runner);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "attackDamage", Amount: 2,
            Card: played!.ObjectId, Affects: played.ObjectId,
            Lasts: new Duration(Uses: 1)));
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.AnchorId == played.ObjectId);

        game.Resolve(Decision.Take(
            action.Id, [], [.. payments.Select(card => card.ObjectId)]));

        Assert.Equal(6, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:player-elimination.5")]
    [Rule("rr:player-elimination.step.5")]
    [Rule("rr:upgrade.1")]
    [Fact]
    public void APlayerEliminatedByAnActionCostFinishesItAndTheirTurn()
    {
        // An upgrade is "active so long as it is in play". Focused Rage's Hero
        // Action deals one damage to its player as a cost.
        // At one remaining hit point that eliminates She-Hulk, but the ability
        // still completes. She no longer participates, so the engine asks the
        // next player instead of constructing another prompt for seat zero.
        Card? rage = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01019a");
                board.Seats[0].IdentityCard.TakeDamage(14);
                rage = board.CreateCard(
                    "01027",
                    board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            },
            heroes: ["she_hulk", "spider_man"]);

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == rage!.ObjectId);
        var resolved = game.Resolve(Decision.Take(action.Id));

        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.IsOver);
        Assert.Equal(1, game.Active);
        Assert.Equal(1, game.Pending!.Player);
        Assert.Equal(Question.TurnOption, game.Pending.Asking);
        Assert.All(resolved.Events, happened => Assert.Equal(Steps.TurnAction, happened.Trigger));

        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(1, game.Pending!.Player);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:event")]
    [Rule("rr:initiating-abilities.step.1")]
    [Rule("rr:initiating-abilities.step.7")]
    [Fact]
    public void AnEventActionResumesItsOccurrenceAfterChoosingATarget()
    {
        Card? kick = null;
        Card? genius = null;
        Card? energy = null;
        Card? minion = null;
        var (game, world) = Playing(
            board =>
            {
                foreach (var card in board.Seats[0].Hand.Cards.ToList())
                {
                    World.MoveToTop(card, board.Seats[0].Deck);
                }

                kick = board.CreateCard(AuthoredCards.SwingingWebKick, board.Seats[0].Hand);
                genius = board.CreateCard("01089", board.Seats[0].Hand);
                energy = board.CreateCard("01088", board.Seats[0].Hand);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == kick!.ObjectId);

        var paying = new[] { genius!.ObjectId, energy!.ObjectId };
        var suspended = game.Resolve(Decision.Take(action.Id, [], paying));
        Assert.Equal(Question.Element, suspended.Prompt!.Asking);
        Assert.Equal(DeckType.RevealingArea, kick!.Area.Type);
        Assert.Equal(PlayArea.Of(0), kick.Area.PlayArea);
        Assert.True(kick.FaceUp);
        Assert.False(DeckTypes.IsInPlay(kick.Area.Type));

        var finished = game.Resolve(Decision.Take(minion!.ObjectId));

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(DeckType.DiscardPile, kick.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Question.TurnOption, finished.Prompt!.Asking);
        var damage = Assert.Single(finished.Events.OfType<FieldSet>(), happened =>
            happened.Card == minion.ObjectId && happened.Field == "health");
        Assert.Equal(Steps.TurnAction, damage.Trigger);
        Assert.Equal(
            paying,
            suspended.Events.OfType<CardsMoved>()
                .SelectMany(moved => moved.Cards)
                .Select(card => card.Card)
                .Where(paying.Contains));
    }

    [Rule("rr:ability.2")]
    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:in-play-and-out-of-play.7")]
    [Rule("rr:in-play-and-out-of-play.8")]
    [Rule("rr:ownership-and-control.4")]
    [Fact]
    public void ActionsComeFromInPlayCardsAndEventsInHand()
    {
        // Hero, alter-ego, ally, upgrade, and support abilities "may only be
        // used if the card is in play"; cards in a player's hand, deck, and
        // discard pile are out of play. Events implicitly resolve from an
        // out-of-play area, so the event in hand is offered while Focused Rage
        // is silent from each of those three areas. The live copy is the
        // control that proves the card's action itself is reachable.
        Card? discardedRage = null;
        Card? heldRage = null;
        Card? deckRage = null;
        Card? liveRage = null;
        Card? kick = null;
        var (game, _) = Playing(
            board =>
            {
                discardedRage = board.CreateCard(
                    "01027",
                    board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
                heldRage = board.CreateCard("01027", board.Seats[0].Hand);
                deckRage = board.CreateCard("01027", board.Seats[0].Deck);
                liveRage = board.CreateCard(
                    "01027",
                    board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                kick = board.CreateCard(AuthoredCards.SwingingWebKick, board.Seats[0].Hand);
                board.Seats[0].IdentityCard.TakeDamage(1);
            },
            hero: true);

        int[] outOfPlay =
            [discardedRage!.ObjectId, heldRage!.ObjectId, deckRage!.ObjectId];
        Assert.DoesNotContain(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && outOfPlay.Contains(option.AnchorId));
        Assert.Contains(game.Pending.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == liveRage!.ObjectId);
        Assert.Contains(
            game.Pending.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == kick!.ObjectId);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ARejectedActionCommandDoesNotRemainOnTheAgenda()
    {
        Card? kick = null;
        Card? genius = null;
        Card? energy = null;
        var (game, world) = Playing(
            board =>
            {
                foreach (var card in board.Seats[0].Hand.Cards.ToList())
                {
                    World.MoveToTop(card, board.Seats[0].Deck);
                }

                kick = board.CreateCard(AuthoredCards.SwingingWebKick, board.Seats[0].Hand);
                genius = board.CreateCard("01089", board.Seats[0].Hand);
                energy = board.CreateCard("01088", board.Seats[0].Hand);
            },
            hero: true);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == kick!.ObjectId);

        Assert.Throws<RulesNotImplementedException>(
            () => game.Resolve(Decision.Take(action.Id)));

        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(DeckType.HandsArea, kick!.Area.Type);
        Assert.Equal(DeckType.HandsArea, genius!.Area.Type);
        Assert.Equal(DeckType.HandsArea, energy!.Area.Type);

        var retry = game.Resolve(Decision.Take(
            action.Id, [], [genius.ObjectId, energy.ObjectId]));
        Assert.Equal(Question.Element, retry.Prompt!.Asking);
    }

    [Rule("rr:choose-game-element.3.1")]
    [Rule("rr:scheme-card-type.1")]
    [Rule("rr:player-turn.5")]
    [Fact]
    public void ACardSpecificChoiceSuspendsInsideTheActionOccurrence()
    {
        Card? practice = null;
        Card? discard = null;
        Card? side = null;
        var (game, world) = Playing(board =>
        {
            var scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
            scheme.PlaceTokens("k_threat", 2);
            side = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
            side.PlaceTokens("k_threat", 1);
            practice = board.CreateCard("01023", board.Seats[0].Hand);
            discard = board.CreateCard("01087", board.Seats[0].Hand);
        });
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == practice!.ObjectId);

        var suspended = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(Question.Element, suspended.Prompt!.Asking);
        Assert.Contains(suspended.Prompt.Affordances,
            option => option.Id == world.TheCardIn(DeckType.MainSchemesArea)!.ObjectId);
        Assert.Contains(suspended.Prompt.Affordances,
            option => option.Id == side!.ObjectId);
        Assert.Equal(Steps.ChooseOption, world.Agenda.Current!.Value.What);
        Assert.True(world.Agenda.Current.Value.Plan);
        var occurrence = Assert.IsType<Occurrence>(world.Agenda.Occurrence);
        Assert.True(occurrence.Is(Steps.TurnAction));
        Assert.Equal(practice!.ObjectId, occurrence.Subject);
        Assert.Contains(discard!, world.Seats[0].Hand.Cards);
    }

    [Rule("rr:player-turn.5.1")]
    [Rule("rr:ability.13")]
    [Fact]
    public void AnAlterEgoActionIsNotOfferedToAHero()
    {
        // "If the action ability is preceded by **Hero** or **Alter-Ego**, the
        // player must be in the specified form in order to trigger the
        // ability." 728 of the 966 in the pool are preceded by one.
        var (game, _) = Playing(
            board =>
            {
                InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(5);
            },
            hero: true);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
    }

    [Rule("rr:exhausted.2")]
    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void AnExhaustedCardCannotPayToExhaustItself()
    {
        // `rr:initiating-abilities.step.3` checks "the player's ability to pay"
        // before anything happens, and step 5 aborts "without paying any
        // costs". So an ability whose cost cannot be met is not offered at all
        // -- an affordance that would abort is a trap, not an offer.
        var (game, _) = Playing(board =>
        {
            InPlay(board, AuthoredCards.AuntMay).Exhaust();
            board.Seats[0].IdentityCard.TakeDamage(5);
        });

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:action.1")]
    [Fact]
    public void AnotherPlayersActionIsOfferedDirectlyAndResolvedByThem()
    {
        // "Ask another player to trigger any Action ability that player could
        // trigger on their own turn." The engine treats taking this direct
        // affordance as the other player accepting or offering. It is still
        // their action: their alter-ego form makes it legal, their Aunt May
        // exhausts, and their identity is healed.
        Card? may = null;
        var (game, world) = Playing(
            board =>
            {
                may = board.CreateCard(
                    AuthoredCards.AuntMay,
                    board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                board.Seats[1].IdentityCard.TakeDamage(5);
            },
            heroes: ["spider_man", "she_hulk"]);

        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
        Assert.Equal(may!.ObjectId, action.AnchorId);
        Assert.Equal(1, action.AnchorPlayer);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(may.Ready);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-turn.5.1")]
    [Rule("rr:player-turn.6")]
    [Rule("rr:limit")]
    [Fact]
    public void AnotherPlayersFormPaymentAndLimitRemainTheirs()
    {
        // Captain Marvel's Rechannel can be offered during Spider-Man's turn,
        // but Captain Marvel remains the resolving player. Her hero face makes
        // it available, her hand pays it, her damage is healed and her
        // once-per-round limit removes the re-offer.
        Card? energy = null;
        var (game, world) = Playing(
            board =>
            {
                board.Seats[1].IdentityCard.TurnTo("01010a");
                board.Seats[1].IdentityCard.TakeDamage(2);
                energy = board.CreateCard("01087", board.Seats[1].Hand);
            },
            heroes: ["spider_man", "captain_marvel"]);

        int captain = world.Seats[1].IdentityCard.ObjectId;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == captain);
        var price = Assert.Single(action.CostOptions);

        Assert.Equal(1, action.AnchorPlayer);
        Assert.Contains(price.Generators, source => source.Effect == energy!.ObjectId);
        Assert.DoesNotContain(
            price.Generators,
            source => world.Seats[0].Hand.Cards.Any(card => card.ObjectId == source.Effect));

        game.Resolve(Decision.Take(action.Id, [], [energy!.ObjectId]));

        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == captain);
    }

    [Rule("rr:alliance")]
    [Rule("rr:alliance.1")]
    [Rule("rr:alliance.2")]
    [Fact]
    public void AnAllianceEventUsesTheWholeTablesResourcesButItsPlayerResolvesIt()
    {
        // Any player may contribute while paying an Alliance card's costs,
        // but "only the player playing the card ... is considered to be
        // resolving that card." Player one supplies two of the three
        // resources; the effect still draws for player zero.
        const string alliance = "25036"; // Cosmic Alliance, cost 3.
        Card? card = null;
        Card? mine = null;
        Card? theirs = null;
        var runner = Runner(
            alliance,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""");
        var (game, world) = Playing(
            board =>
            {
                Hand(board, player: 0, Physicals, count: 0);
                Hand(board, player: 1, Mentals, count: 0);
                card = board.CreateCard(alliance, board.Seats[0].Hand);
                mine = board.CreateCard(Physicals, board.Seats[0].Hand);
                theirs = board.CreateCard("01088", board.Seats[1].Hand); // two energy
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == card!.ObjectId);
        var price = Assert.Single(action.CostOptions);
        Assert.Contains(price.Generators, source => source.Effect == mine!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == theirs!.ObjectId);
        int playerZeroHand = world.Seats[0].Hand.Cards.Count;
        int playerOneHand = world.Seats[1].Hand.Cards.Count;

        game.Resolve(Decision.Take(
            action.Id, [], [mine!.ObjectId, theirs!.ObjectId]));

        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, mine.Area.Type);
        Assert.Equal(DeckType.DiscardPile, theirs.Area.Type);
        Assert.Equal(playerZeroHand - 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(playerOneHand - 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:alliance")]
    [Rule("rr:resource-ability.1")]
    [Fact]
    public void AnAllianceHelperCanUseTheirResourceAbility()
    {
        // Player one controls Peter Parker's Scientist resource ability. It is
        // offered as their contribution and is used by them, while player zero
        // remains the event's resolver.
        const string alliance = "25036"; // Cosmic Alliance, cost 3.
        Card? card = null;
        Card? doubleEnergy = null;
        var runner = Runner(
            alliance,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            includeAuthored: true);
        var (game, world) = Playing(
            board =>
            {
                Hand(board, player: 0, Physicals, count: 0);
                Hand(board, player: 1, Mentals, count: 0);
                card = board.CreateCard(alliance, board.Seats[0].Hand);
                doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
            },
            heroes: ["captain_marvel", "spider_man"],
            abilities: runner);

        int scientist = world.Seats[1].IdentityCard.ObjectId;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == card!.ObjectId);
        var price = Assert.Single(action.CostOptions);
        Assert.Contains(price.Generators, source => source.Effect == scientist);

        game.Resolve(Decision.Take(
            action.Id, [], [doubleEnergy!.ObjectId, scientist]));

        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleEnergy.Area.Type);
        Assert.DoesNotContain(
            runner.ResourceAbilities(world, 1), source => source.Effect == scientist);
    }

    [Fact]
    public void AnEventWithPrintedAndArrowCostsIsRejectedBeforePayment()
    {
        const string alliance = "25036";
        Card? card = null;
        var runner = Runner(
            alliance,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spend": "Y" }""");
        var (game, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 4);
                card = board.CreateCard(alliance, board.Seats[0].Hand);
            },
            abilities: runner);
        var eventCard = Assert.IsType<Card>(card);

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == eventCard.ObjectId);
        int[] payment = [.. world.Seats[0].Hand.Cards
            .Where(candidate => candidate.ObjectId != eventCard.ObjectId)
            .Select(candidate => candidate.ObjectId)];

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world,
            new PendingAbility(eventCard.ObjectId, AbilityType.Action, 0),
            payment,
            []));
        Assert.Same(world.Seats[0].Hand, eventCard.Area);
        Assert.All(payment, id => Assert.Same(world.Seats[0].Hand, world.Cards[id].Area));
    }

    [Fact]
    public void AWindowEventWithPrintedAndArrowCostsUsesOneAllocatedPayment()
    {
        Card? card = null;
        Card? energy = null;
        var runner = Runner(
            AuthoredCards.Backflip,
            "Interrupt",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spend": "Y" }""",
            eventName: "WhenDamageWouldBeDealt");
        var (_, world) = Playing(
            board =>
            {
                Hand(board, AuthoredCards.Backflip, 0);
                card = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
                energy = board.CreateCard("01088", board.Seats[0].Hand);
            },
            hero: true,
            abilities: runner);
        var eventCard = Assert.IsType<Card>(card);
        var payment = Assert.IsType<Card>(energy);
        var occurrence = new Occurrence(
            1,
            ["WhenDamageWouldBeDealt"],
            Player: 0,
            Target: world.Seats[0].IdentityCard.ObjectId);

        Assert.Contains(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == eventCard.ObjectId);
        runner.Resolve(
            world,
            occurrence,
            new PendingAbility(eventCard.ObjectId, AbilityType.Interrupt, 0),
            [payment.ObjectId],
            [],
            allocations:
            [
                new ResourceAllocation(payment.ObjectId, Cost: 1, PaidAs: "Y"),
            ]);
        Assert.Equal(DeckType.DiscardPile, eventCard.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Fact]
    public void UnlikeOverpaymentForAResourceSensitiveEventIsRejectedBeforePayment()
    {
        const string relentlessAssault = "01053"; // cost 2; physical payment grants overkill.
        Card? card = null;
        Card? doubleMental = null;
        Card? physical = null;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                board.Seats[0].IdentityCard.TurnTo("01010a");
                card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
                doubleMental = board.CreateCard("01089", board.Seats[0].Hand);
                physical = board.CreateCard(Physicals, board.Seats[0].Hand);
                board.CreateCard(
                    AuthoredCards.Shocker,
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            heroes: ["captain_marvel"],
            abilities: runner);
        foreach (var extra in world.Seats[0].Hand.Cards
                     .Where(candidate => candidate != card
                         && candidate != doubleMental
                         && candidate != physical)
                     .ToList())
        {
            World.MoveToTop(extra, world.Seats[0].Deck);
        }
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);

        Assert.Contains(
            price.Generators, source => source.Effect == doubleMental!.ObjectId);
        Assert.Contains(
            price.Generators, source => source.Effect == physical!.ObjectId);

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world, action, [doubleMental!.ObjectId, physical!.ObjectId],
            [world.Cards.First(candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId]));

        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleMental!.Area);
        Assert.Same(world.Seats[0].Hand, physical!.Area);
    }

    [Fact]
    public void WildOverpaymentForAResourceSensitiveEventIsRejectedBeforePayment()
    {
        const string relentlessAssault = "01053";
        Card? card = null;
        Card? doubleMental = null;
        Card? wild = null;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
                doubleMental = board.CreateCard("01089", board.Seats[0].Hand);
                wild = board.CreateCard("01011", board.Seats[0].Hand);
                board.CreateCard(
                    AuthoredCards.Shocker,
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int target = world.Cards.First(
            candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId;

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world, action, [doubleMental!.ObjectId, wild!.ObjectId], [target]));

        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleMental!.Area);
        Assert.Same(world.Seats[0].Hand, wild!.Area);
    }

    [Fact]
    public void AllWildOverpaymentStillNeedsEachWildsDeclaredType()
    {
        // The player may declare every paid wild as a non-physical type. The
        // source-only wire cannot infer Relentless Assault's physical bonus.
        const string relentlessAssault = "01053";
        Card? card = null;
        var wilds = new List<Card>();
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                board.Seats[0].IdentityCard.TurnTo("01010a");
                card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
                for (int index = 0; index < 3; index++)
                {
                    wilds.Add(board.CreateCard("01011", board.Seats[0].Hand));
                }
                board.CreateCard(
                    AuthoredCards.Shocker,
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            heroes: ["captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int target = world.Cards.First(
            candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId;

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world,
            action,
            [.. wilds.Select(source => source.ObjectId)],
            [target]));

        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.All(wilds, source => Assert.Same(world.Seats[0].Hand, source.Area));
    }

    [Fact]
    public void ExactWildPaymentStillNeedsTheWildsDeclaredType()
    {
        const string relentlessAssault = "01053";
        Card? card = null;
        Card? wild = null;
        Card? mental = null;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
                wild = board.CreateCard("01011", board.Seats[0].Hand);
                mental = board.CreateCard(Mentals, board.Seats[0].Hand);
                board.CreateCard(
                    AuthoredCards.Shocker,
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int target = world.Cards.First(
            candidate => candidate.FaceId == AuthoredCards.Shocker).ObjectId;

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world, action, [wild!.ObjectId, mental!.ObjectId], [target]));

        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, wild!.Area);
        Assert.Same(world.Seats[0].Hand, mental!.Area);
    }

    [Rule("rr:requirement-resources")]
    [Fact]
    public void ARequirementCanForceAnUnambiguousWildDeclaration()
    {
        const string requiredEvent = "27016"; // cost 2, requirement physical.
        Card? card = null;
        Card? doubleWild = null;
        var runner = Runner(
            requiredEvent,
            "Action",
            """{ "if": { "test": { "paidWithResource": "R" }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(requiredEvent, board.Seats[0].Hand);
                doubleWild = board.CreateCard("01044", board.Seats[0].Hand);
            },
            heroes: ["captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);

        runner.Act(world, action, [doubleWild!.ObjectId], []);

        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleWild.Area.Type);
    }

    [Rule("rr:requirement-resources")]
    [Fact]
    public void AForcedWildDeclarationIsCarriedIntoPaidResourceTests()
    {
        const string requiredEvent = "27016";
        Card? card = null;
        Card? wild = null;
        Card? mental = null;
        var runner = Runner(
            requiredEvent,
            "Action",
            """{ "if": { "test": { "paidWithResource": "Y" }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(requiredEvent, board.Seats[0].Hand);
                wild = board.CreateCard("01011", board.Seats[0].Hand);
                mental = board.CreateCard(Mentals, board.Seats[0].Hand);
            },
            heroes: ["captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.Act(world, action, [wild!.ObjectId, mental!.ObjectId], []);

        // The requirement declared the wild physical, so the energy branch
        // did not draw. The event and both generators have left the hand.
        Assert.Equal(held - 3, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void EveryRepresentablePaidResourceChoiceRemainsAdvertised()
    {
        const string forJustice = "01060";
        Card? card = null;
        Card? doubleEnergy = null;
        Card? doubleMental = null;
        Card? safeTriple = null;
        Card? ambiguousTriple = null;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(forJustice, board.Seats[0].Hand);
                doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
                doubleMental = board.CreateCard("01089", board.Seats[0].Hand);
                safeTriple = board.CreateCard("01014", board.Seats[0].Hand);
                ambiguousTriple = board.CreateCard("21183", board.Seats[0].Hand);
                board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);

        Assert.Contains(price.Generators, source => source.Effect == doubleEnergy!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == doubleMental!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == safeTriple!.ObjectId);
        Assert.Contains(
            price.Generators, source => source.Effect == ambiguousTriple!.ObjectId);
    }

    [Fact]
    public void ARedundantSourceRemainsAdvertisedWhenItCannotChangeThePaidOutcome()
    {
        const string relentlessAssault = "01053";
        Card? card = null;
        Card? triplePhysical = null;
        Card? mental = null;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(relentlessAssault, board.Seats[0].Hand);
                triplePhysical = board.CreateCard("10007", board.Seats[0].Hand);
                mental = board.CreateCard(Mentals, board.Seats[0].Hand);
                board.CreateCard(
                    AuthoredCards.Shocker,
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);
        foreach (var extra in world.Seats[0].Hand.Cards
                     .Where(candidate => candidate != card
                         && candidate != triplePhysical
                         && candidate != mental)
                     .ToList())
        {
            World.MoveToTop(extra, world.Seats[0].Deck);
        }
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);

        Assert.Contains(
            price.Generators, source => source.Effect == triplePhysical!.ObjectId);
        Assert.Contains(price.Generators, source => source.Effect == mental!.ObjectId);

        runner.Act(
            world,
            action,
            [triplePhysical!.ObjectId, mental!.ObjectId],
            [world.Cards.First(candidate =>
                candidate.FaceId == AuthoredCards.Shocker).ObjectId]);

        Assert.Equal(DeckType.DiscardPile, triplePhysical.Area.Type);
        Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
    }

    [Fact]
    public void ARemainingWildDeclarationChoiceIsOfferedButNotInferred()
    {
        const string requiredEvent = "27016"; // one of two wilds must be physical.
        Card? card = null;
        Card? doubleWild = null;
        var runner = Runner(
            requiredEvent,
            "Action",
            """{ "if": { "test": { "paidWithResource": "G" }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                card = board.CreateCard(requiredEvent, board.Seats[0].Hand);
                doubleWild = board.CreateCard("01044", board.Seats[0].Hand);
            },
            heroes: ["captain_marvel"],
            abilities: runner);

        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);
        Assert.Contains(
            Assert.Single(runner.Describe(world, action).CostOptions).Generators,
            source => source.Effect == doubleWild!.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world,
            action,
            [doubleWild!.ObjectId],
            []));
        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleWild!.Area);

        runner.Act(
            world,
            action,
            [doubleWild.ObjectId],
            [],
            allocations:
            [
                new ResourceAllocation(doubleWild.ObjectId, Cost: 0, PaidAs: "RG"),
            ]);

        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleWild.Area.Type);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:cost.5.1")]
    [Fact]
    public void OneDoubleResourceCanBeDividedBetweenSimultaneousCosts()
    {
        // Two energy costs on one ability are paid simultaneously. A single
        // card generating two energy icons supplies one to each cost and is
        // discarded once.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "seq": [ { "spend": "Y" }, { "spend": "Y" } ] }""");
        Card? source = null;
        Card? doubleEnergy = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Hand(board, AuthoredCards.Backflip, 0);
                doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
            },
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);
        var price = Assert.Single(action.CostOptions);

        Assert.Equal("2", price.Cost);
        Assert.Equal(["YY"], price.Rule);
        game.Resolve(Decision.Take(action.Id, [], [doubleEnergy!.ObjectId]));

        Assert.Equal(DeckType.DiscardPile, doubleEnergy.Area.Type);
        Assert.NotNull(game.Pending);
        Assert.Equal(Question.TurnOption, game.Pending.Asking);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:cost.5.1")]
    [Fact]
    public void OneDoubleResourceCanBeAllocatedAcrossAnEventsTwoCosts()
    {
        // An event's printed cost and its cost before the arrow are paid
        // simultaneously, and the player "chooses how to divide" a generated
        // double resource between them. One command therefore assigns one icon
        // to each component while naming the generator only once.
        const string eventCard = "01004";
        var runner = Runner(
            eventCard,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spend": "Y" }""");
        Card? card = null;
        Card? doubleEnergy = null;
        var (game, _) = Playing(
            board =>
            {
                card = board.CreateCard(eventCard, board.Seats[0].Hand);
                doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
            },
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == card!.ObjectId);
        var price = Assert.Single(action.CostOptions);

        Assert.Equal("2", price.Cost);
        Assert.Equal(2, price.ResourceCosts.Count);
        Assert.Equal(["1", "1"], price.ResourceCosts.Select(cost => cost.Cost));

        game.Resolve(Decision.Take(
            action.Id,
            [],
            [doubleEnergy!.ObjectId],
            new Dictionary<string, long>(StringComparer.Ordinal),
            [
                new ResourceAllocation(doubleEnergy.ObjectId, Cost: 0, PaidAs: "Y"),
                new ResourceAllocation(doubleEnergy.ObjectId, Cost: 1, PaidAs: "Y"),
            ]));

        Assert.Equal(DeckType.DiscardPile, card!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, doubleEnergy!.Area.Type);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ACombinedEventCostWithoutAnAllocationChangesNoState()
    {
        const string eventCard = "01004";
        var runner = Runner(
            eventCard,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spend": "Y" }""");
        Card? card = null;
        Card? doubleEnergy = null;
        var (_, world) = Playing(
            board =>
            {
                card = board.CreateCard(eventCard, board.Seats[0].Hand);
                doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
            },
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);

        var thrown = Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world, action, [doubleEnergy!.ObjectId], []));

        Assert.Contains("allocation was not supplied", thrown.Message, StringComparison.Ordinal);
        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleEnergy!.Area);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnInvalidCombinedEventAllocationChangesNoState()
    {
        const string eventCard = "01004";
        var runner = Runner(
            eventCard, "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spend": "Y" }""");
        Card? card = null;
        Card? doubleEnergy = null;
        var (_, world) = Playing(
            board =>
            {
                card = board.CreateCard(eventCard, board.Seats[0].Hand);
                doubleEnergy = board.CreateCard("01088", board.Seats[0].Hand);
            },
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == card!.ObjectId);

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world, action, [doubleEnergy!.ObjectId], [],
            allocations:
            [
                new ResourceAllocation(doubleEnergy.ObjectId, Cost: 0, PaidAs: "Y"),
            ]));

        Assert.Same(world.Seats[0].Hand, card!.Area);
        Assert.Same(world.Seats[0].Hand, doubleEnergy!.Area);
    }

}
