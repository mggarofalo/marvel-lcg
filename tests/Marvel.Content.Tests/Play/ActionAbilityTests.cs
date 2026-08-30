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
public sealed class ActionAbilityTests
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

    [Rule("rr:event.5.1")]
    [Fact]
    public void AnAttackModifierAppliesOnlyToAnEventsFirstAttack()
    {
        // A modifier to damage "an attack" deals applies to "only the first"
        // when an event initiates multiple attacks. The first deals three and
        // the second deals one.
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

    [Rule("rr:event.5.1")]
    [Fact]
    public void EveryDamageInstanceInsideTheFirstAttackIsModified()
    {
        // Both damage nodes belong to one attack wrapper. The one-use modifier
        // is consumed after that attack, not after its first damage instance.
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
    [Rule("rr:player-turn.5")]
    [Fact]
    public void ACardSpecificChoiceSuspendsInsideTheActionOccurrence()
    {
        Card? practice = null;
        Card? discard = null;
        var (game, world) = Playing(board =>
        {
            var scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
            scheme.PlaceTokens("k_threat", 2);
            practice = board.CreateCard("01023", board.Seats[0].Hand);
            discard = board.CreateCard("01087", board.Seats[0].Hand);
        });
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == practice!.ObjectId);

        var suspended = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(Question.Element, suspended.Prompt!.Asking);
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

    [Rule("rr:printed.1")]
    [Rule("rr:text-box.1.1")]
    [Fact]
    public void APrintedTextBoxResourceAbilityPaysAPrintedResourceCost()
    {
        // “They may use resources generated by a card's ability, so long as
        // the icon for the resource that card generates is printed in its text
        // box.” Peter Parker's Scientist icon is marked from printed card data
        // and appears beside printed hand icons in this narrower generator menu.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spendPrinted": "B" }""",
            includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                source = InPlay(board, AuthoredCards.AuntMay);
            },
            abilities: runner);
        int peter = world.Seats[0].IdentityCard.ObjectId;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId
                && option.CostOptions.Any(cost =>
                    cost.ResourceCosts.Any(component => component.Printed)));
        var price = Assert.Single(action.CostOptions);

        Assert.Contains(price.Generators, generator => generator.Effect == peter);
        Assert.True(Assert.Single(price.ResourceCosts).Printed);

        game.Resolve(Decision.Take(action.Id, [], [peter]));

        Assert.DoesNotContain(
            runner.ResourceAbilities(world, 0), generator => generator.Effect == peter);
    }

    [Rule("rr:printed.1.1")]
    [Fact]
    public void APrintedWildCannotSubstituteForAPrintedPhysicalCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spendPrinted": "R" }""",
            includeAuthored: true);
        Card? source = null;
        Card? wild = null;
        var (_, world) = Playing(
            board =>
            {
                Hand(board, Physicals, 0);
                source = InPlay(board, AuthoredCards.AuntMay);
                wild = board.CreateCard("01011", board.Seats[0].Hand);
            },
            abilities: runner);

        foreach (var other in world.Seats[0].Hand.Cards
                     .Where(card => card != wild)
                     .ToList())
        {
            World.MoveToTop(other, world.Seats[0].Deck);
        }

        Assert.DoesNotContain(
            runner.Actions(world, 0),
            ability => ability.Card == source!.ObjectId && ability.Ordinal == 1);
        Assert.Same(world.Seats[0].Hand, wild!.Area);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:printed.1")]
    [Fact]
    public void SimultaneousPrintedResourceCostsShareOneExactPayment()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "seq": [ { "spendPrinted": "B" }, { "spendPrinted": "Y" } ] }""",
            includeAuthored: true);
        Card? source = null;
        Card? energy = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                energy = board.CreateCard("01093", board.Seats[0].Hand);
            },
            abilities: runner);
        foreach (var other in world.Seats[0].Hand.Cards
                     .Where(card => card != energy)
                     .ToList())
        {
            World.MoveToTop(other, world.Seats[0].Deck);
        }
        int peter = world.Seats[0].IdentityCard.ObjectId;
        var ability = Assert.Single(
            runner.Actions(world, 0),
            pending => pending.Card == source!.ObjectId && pending.Ordinal == 1);
        var price = Assert.Single(runner.Describe(world, ability).CostOptions);

        Assert.Equal(2, price.ResourceCosts.Count);
        Assert.All(price.ResourceCosts, component => Assert.True(component.Printed));

        runner.Act(world, ability, [peter, energy!.ObjectId], []);

        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.DoesNotContain(
            runner.ResourceAbilities(world, 0), generator => generator.Effect == peter);
    }

    [Rule("rr:cost.7")]
    [Rule("rr:cost.8")]
    [Fact]
    public void AnOutOfPlayCostCanUseOnlyThePayingPlayersArea()
    {
        // A hand is out of play, and a payer "may only use game elements that
        // are in their own out-of-play areas." The prompt therefore offers
        // only the payer's hand, and forging another player's card is rejected
        // before either card moves.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "discardFromHand": 1 }""");
        Card? source = null;
        Card? otherPlayersCard = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                otherPlayersCard = board.CreateCard(
                    Physicals, board.Seats[1].Hand);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var ability = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        var targets = Assert.IsType<TargetRequest>(
            runner.Describe(world, ability).Targets);

        Assert.DoesNotContain(otherPlayersCard!.ObjectId, targets.Legal);
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world, ability, [], [otherPlayersCard.ObjectId]));
        Assert.Same(world.Seats[1].Hand, otherPlayersCard.Area);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Rule("rr:cost.7.1")]
    [Rule("rr:cost.7.2")]
    [Fact]
    public void AChosenFriendlyCostCanUseAnotherPlayersCard()
    {
        // A cost that "uses the word 'choose'" or targets a "friendly" card
        // may choose cards the payer does not control.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost:
            """
            { "exhaustChosen": {
              "from": { "query": "heroesAndAllies" },
              "count": 1
            } }
            """);
        Card? source = null;
        Card? friendly = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                friendly = board.CreateCard(
                    AuthoredCards.BlackCat,
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var ability = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        var targets = Assert.IsType<TargetRequest>(
            runner.Describe(world, ability).Targets);

        Assert.Contains(friendly!.ObjectId, targets.Legal);
        Assert.DoesNotContain(world.Seats[1].IdentityCard.ObjectId, targets.Legal);
        runner.Act(world, ability, [], [friendly.ObjectId]);

        Assert.False(friendly.Ready);
    }

    [Rule("rr:cost.9")]
    [Theory]
    [InlineData("{ \"discardUpToFromHand\": 3 }")]
    [InlineData("{ \"discardAnyFromHand\": \"yourHand\" }")]
    public void UpToAndAnyNumberCostsRequireAtLeastOne(string cost)
    {
        // A cost requiring "any number" or "up to" a number "requires a
        // minimum of one such game element."
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: cost);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var ability = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        var targets = Assert.IsType<TargetRequest>(
            runner.Describe(world, ability).Targets);
        int held = world.Seats[0].Hand.Cards.Count;

        Assert.Equal(1, targets.Min);
        Assert.True(targets.Max >= 1);
        Assert.Throws<RulesNotImplementedException>(() =>
            runner.Act(world, ability, [], []));
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);

        int paid = targets.Legal[0];
        runner.Act(world, ability, [], [paid]);
        Assert.Equal(DeckType.DiscardPile, world.Cards[paid].Area.Type);
    }

    [Rule("rr:cost.5")]
    [Rule("rr:cost.10")]
    [Fact]
    public void AnUnpayableSimultaneousCostChangesNoState()
    {
        // The resource half is invalid, so the exhaust half is not paid first.
        // The forged action is rejected with the source still ready.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "seq": [ { "exhaust": "this" }, { "spend": "B" } ] }""");
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Hand(board, Physicals, 1);
            },
            abilities: runner);
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);

        Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [world.Seats[0].Hand.Cards[0].ObjectId], []));
        Assert.True(source!.Ready);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:player-turn.6")]
    [Fact]
    public void ASharedEncounterActionIsOfferedForEveryEligiblePlayer()
    {
        // The request is implied, but the acting seat is material: that player
        // supplies the resources and resolves every reference to "you".
        Card? horn = null;
        var (game, _) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
                board.Seats[1].IdentityCard.TurnTo("01010a");
                horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));
                board.Abilities = AuthoredCards.Runner();
                Reveal.Resolve(board, Cards, horn, 0, []);

                foreach (var card in board.Seats[0].Hand.Cards.ToList())
                {
                    World.MoveToTop(card, board.Seats[0].Deck);
                }

                Hand(board, player: 1, Physicals, count: 3);
            },
            heroes: ["spider_man", "captain_marvel"]);

        var actions = game.Pending!.Affordances
            .Where(option => option.Verb == Game.ActionVerb
                && option.AnchorId == horn!.ObjectId)
            .ToList();
        Assert.Equal([0, 1], actions.Select(action => action.AnchorPlayer));
        Assert.Equal(2, actions.Select(action => action.Id).Distinct().Count());
    }

    [Rule("rr:ability.8.2")]
    [Rule("rr:action.1.1")]
    [Fact]
    public void OnlyThePlayerHoldingAnObligationMayTriggerItsAction()
    {
        // An obligation remains an encounter card, but these clauses are the
        // exception to the general permission to use encounter-card actions.
        // The second player's turn can request another player's ordinary
        // action; it cannot request the obligation sitting in player zero's
        // play area.
        var runner = Runner(
            AuthoredCards.Hunted,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""");
        Card? obligation = null;
        var (_, world) = Playing(
            board => obligation = board.CreateCard(
                AuthoredCards.Hunted,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(runner.Actions(world, 0), action => action.Card == obligation!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == obligation!.ObjectId);
    }

    [Rule("rr:ability.8.1")]
    [Fact]
    public void OnlyTheHostControllerMayUseAnAttachmentAbilityThatSaysYou()
    {
        // The attachment belongs to the scenario. Its host is a player card,
        // and “you” makes that host's controller the only permitted player.
        var runner = Runner(
            AuthoredCards.PrelateArmor,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""");
        Card? attachment = null;
        var (_, world) = Playing(
            board => attachment = board.CreateCard(
                AuthoredCards.PrelateArmor,
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == attachment!.ObjectId);
    }

    [Rule("rr:ability.8.1")]
    [Fact]
    public void YourInTheAttachmentInstructionRestrictsEveryAbilityOnTheCard()
    {
        // All Tied Up says “Attach to your identity card,” while its Action is
        // only “spend resources → discard this card.” The permission belongs
        // to the attachment's whole printed text, not only the selected
        // ability, so another player cannot trigger that Action.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "02048", "attachTo": { "query": "yourIdentity" }, "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "discard": "this" }
                } ] } ] }
                """));
        Card? attachment = null;
        var (_, world) = Playing(
            board => attachment = board.CreateCard(
                "02048",
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == attachment!.ObjectId);
    }

    [Rule("rr:ability.8.1")]
    [Rule("rr:the-golden-rules")]
    [Fact]
    public void AnExplicitAnyPlayerPermissionOverridesTheAttachmentRestriction()
    {
        // Obedience Potion says “Attach to your identity,” then its Hero
        // Action ends “Any player can do this.” The printed exception lets the
        // other player initiate the ability and pay from their own hand.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "16123", "attachTo": { "query": "yourIdentity" }, "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game", "form": "hero" },
                    "anyPlayer": true,
                    "cost": { "spend": "BB" },
                    "effect": { "discard": "this" }
                } ] } ] }
                """));
        Card? attachment = null;
        Card[] payment = [];
        var (_, world) = Playing(
            board =>
            {
                board.Seats[1].IdentityCard.TurnTo("01010a");
                attachment = board.CreateCard(
                    "16123",
                    board.AreaOf(
                        DeckType.UpgradesArea,
                        PlayArea.Of(0),
                        host: board.Seats[0].IdentityCard.ObjectId));
                Hand(board, player: 1, Mentals, count: 2);
                payment = [.. board.Seats[1].Hand.Cards.Take(2)];
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        var action = Assert.Single(
            runner.Actions(world, 1), option => option.Card == attachment!.ObjectId);
        runner.Act(world, action, [.. payment.Select(card => card.ObjectId)], []);

        Assert.Equal(DeckType.EncounterDiscardPile, attachment!.Area.Type);
        Assert.All(payment, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }

    [Rule("rr:player-turn.5")]
    [Fact]
    public void AnyPlayerMayUseAPlayerCardThatPrintsThatPermission()
    {
        // Player-turn option 5.c is exactly Plot Convenience's last line:
        // “Any player may trigger this ability.” The permission makes another
        // player's support visible, that player initiates it, and its printed
        // exhaust cost is paid before the effect resolves for that player.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "44050", "abilities": [
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "anyPlayer": true,
                    "cost": { "exhaust": "this" },
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  },
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 2 } }
                  }
                ] } ] }
                """));
        Card? support = null;
        var (_, world) = Playing(
            board => support = InPlay(board, "44050"),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;

        var action = Assert.Single(
            runner.Actions(world, 1), option => option.Card == support!.ObjectId);
        runner.Act(world, action, [], []);

        Assert.False(support!.Ready);
        Assert.Equal(held + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:interrupt.1")]
    [Rule("rr:response.1")]
    [Fact]
    public void AnyPlayerWindowAbilitiesAreEvaluatedForEachPlayer()
    {
        var runner = Runner(
            "44050",
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "spend": "B" }""",
            eventName: Steps.DamageDealt,
            anyPlayer: true);
        Card? support = null;
        Card[] payment = [];
        var (_, world) = Playing(
            board =>
            {
                support = InPlay(board, "44050");
                Hand(board, player: 0, Physicals, count: 0);
                Hand(board, player: 1, Mentals, count: 1);
                payment = [.. board.Seats[1].Hand.Cards];
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;

        var response = Assert.Single(
            runner.Waiting(
                world,
                new Occurrence(1, [Steps.DamageDealt], Player: 0),
                WindowKind.Response),
            option => option.Player == 1);

        Assert.Equal(1, response.Player);
        runner.Act(world, response, [payment[0].ObjectId], []);
        Assert.Equal(DeckType.DiscardPile, payment[0].Area.Type);
        Assert.Equal(held, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(support!.ObjectId, response.Card);
    }

    [Rule("rr:in-player-order.1")]
    [Rule("rr:response.1")]
    [Fact]
    public void AnyPlayerWindowAnswerResumesForThePlayerWhoAcceptedIt()
    {
        var runner = Runner(
            "44050",
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: Steps.DamageDealt,
            anyPlayer: true);
        var (_, world) = Playing(
            board => InPlay(board, "44050"),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var occurrence = new Occurrence(1, [Steps.DamageDealt], Player: 0);
        var events = new List<GameEvent>();
        int firstHeld = world.Seats[0].Hand.Cards.Count;
        int secondHeld = world.Seats[1].Hand.Cards.Count;

        var first = Offering.Work(
            world, runner, occurrence, WindowKind.Response, events)!;
        Assert.Equal(0, first.Player);
        Sequence.Answer(world, Cards, runner, first, Decision.Decline, events);

        var second = Offering.Work(
            world, runner, occurrence, WindowKind.Response, events)!;
        Assert.Equal(1, second.Player);
        Sequence.Answer(
            world, Cards, runner, second,
            Decision.Take(Assert.Single(second.Affordances).Id), events);

        Assert.Equal(firstHeld, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHeld + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:ability.8")]
    [Fact]
    public void TriggerPlayerStillNarrowsAnAnyPlayerWindow()
    {
        var runner = Runner(
            "44050",
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: Steps.DamageDealt,
            player: "trigger.player",
            anyPlayer: true);
        var (_, world) = Playing(
            board => InPlay(board, "44050"),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        var waiting = runner.Waiting(
            world,
            new Occurrence(1, [Steps.DamageDealt], Player: 0),
            WindowKind.Response);

        Assert.Equal(0, Assert.Single(waiting).Player);
    }

    [Rule("rr:ability.8.1")]
    [Fact]
    public void AnAttachmentsYouTriggerMatchesOnlyItsHostController()
    {
        var runner = Runner(
            AuthoredCards.PrelateArmor,
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: "WhenAttacked",
            player: "you");
        Card? attachment = null;
        var (_, world) = Playing(
            board => attachment = board.CreateCard(
                AuthoredCards.PrelateArmor,
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        var ours = runner.Waiting(
            world, new Occurrence(1, ["WhenAttacked"], Player: 0), WindowKind.Response);
        var theirs = runner.Waiting(
            world, new Occurrence(2, ["WhenAttacked"], Player: 1), WindowKind.Response);

        Assert.Equal(0, Assert.Single(ours).Player);
        Assert.Empty(theirs);
    }

    [Rule("rr:ability.8.1")]
    [Rule("rr:ability.8.2")]
    [Fact]
    public void RestrictedResourceAbilitiesBelongOnlyToTheirPermittedPlayer()
    {
        var obligationRunner = Runner(
            AuthoredCards.Hunted,
            "Resource",
            """{ "generate": "E" }""");
        Card? obligation = null;
        var (_, obligationWorld) = Playing(
            board => obligation = board.CreateCard(
                AuthoredCards.Hunted,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: obligationRunner);

        Assert.Contains(
            obligationRunner.ResourceAbilities(obligationWorld, 0),
            source => source.Effect == obligation!.ObjectId);
        Assert.DoesNotContain(
            obligationRunner.ResourceAbilities(obligationWorld, 1),
            source => source.Effect == obligation!.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => obligationRunner.UseResource(
            obligationWorld, 1, obligation!.ObjectId, []));

        // Compound bindings are still printed “you/your”: this query means
        // allies controlled by the resolving player and restricts a
        // player-hosted attachment just as the bare word “you” does.
        var attachmentRunner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [ { "card": "{{AuthoredCards.PrelateArmor}}", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Resource", "subject": "game" },
                    "when": { "exists": { "query": "alliesYouControl" } },
                    "effect": { "generate": "B" }
                } ] } ] }
                """));
        Card? attachment = null;
        var (_, attachmentWorld) = Playing(
            board =>
            {
                board.CreateCard(
                    "01002",
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                attachment = board.CreateCard(
                    AuthoredCards.PrelateArmor,
                    board.AreaOf(
                        DeckType.UpgradesArea,
                        PlayArea.Of(0),
                        host: board.Seats[0].IdentityCard.ObjectId));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: attachmentRunner);

        Assert.Contains(
            attachmentRunner.ResourceAbilities(attachmentWorld, 0),
            source => source.Effect == attachment!.ObjectId);
        Assert.DoesNotContain(
            attachmentRunner.ResourceAbilities(attachmentWorld, 1),
            source => source.Effect == attachment!.ObjectId);

        // Player-relative semantics can also be the node name rather than a
        // word value. Test it in a real response occurrence whose target
        // makes `isYourIdentity` true for the host controller and false for
        // the other player.
        var kindRunner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [ { "card": "{{AuthoredCards.PrelateArmor}}", "abilities": [ {
                    "trigger": { "event": "WhenDamageWouldBeDealt", "timing": "Response", "subject": "game" },
                    "when": { "isYourIdentity": "trigger.target" },
                    "effect": { "draw": { "player": "you", "count": 1 } }
                } ] } ] }
                """));
        Card? kindAttachment = null;
        var (_, kindWorld) = Playing(
            board => kindAttachment = board.CreateCard(
                AuthoredCards.PrelateArmor,
                board.AreaOf(
                    DeckType.UpgradesArea,
                    PlayArea.Of(0),
                    host: board.Seats[0].IdentityCard.ObjectId)),
            heroes: ["spider_man", "captain_marvel"],
            abilities: kindRunner);

        var ours = kindRunner.Waiting(
            kindWorld,
            new Occurrence(
                1,
                ["WhenDamageWouldBeDealt"],
                Player: 0,
                Target: kindWorld.Seats[0].IdentityCard.ObjectId),
            WindowKind.Response);
        var theirs = kindRunner.Waiting(
            kindWorld,
            new Occurrence(
                2,
                ["WhenDamageWouldBeDealt"],
                Player: 1,
                Target: kindWorld.Seats[1].IdentityCard.ObjectId),
            WindowKind.Response);

        Assert.Equal(kindAttachment!.ObjectId, Assert.Single(ours).Card);
        Assert.Empty(theirs);
    }

    [Rule("rr:interrupt.1.1")]
    [Rule("rr:response.1.1")]
    [Theory]
    [InlineData("Interrupt")]
    [InlineData("Response")]
    public void AnotherPlayersObligationIsExcludedFromAbilityWindows(string timing)
    {
        var runner = Runner(
            AuthoredCards.Hunted,
            timing,
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: "WhenAttacked");
        var (_, world) = Playing(
            board => board.CreateCard(
                AuthoredCards.Hunted,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        world.FirstPlayer = 1;
        var occurrence = new Occurrence(1, ["WhenAttacked"], Player: 1);

        var prompt = Offering.Work(
            world,
            runner,
            occurrence,
            timing == "Interrupt" ? WindowKind.Interrupt : WindowKind.Response,
            []);

        Assert.NotNull(prompt);
        Assert.Equal(0, prompt.Player);
    }

    [Rule("rr:action.2")]
    [Rule("rr:action.2.1")]
    [Rule("rr:forced.2")]
    [Fact]
    public void ALegalForcedActionMustResolveBeforeThePlayerPhaseEnds()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "ForcedAction",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        // It may be used at any ordinary action opportunity.
        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        // At the boundary it is no longer optional: the phase stays in the
        // player turn and the only answer is to resolve the Forced Action.
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.False(game.Pending!.Cancellable);
        var forced = Assert.Single(game.Pending.Affordances);

        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(forced.Id));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);

        // Its exhaust cost is now unpayable, so resolution continues directly
        // to the ordinary end phase. It must not reopen a normal turn.
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb is Game.ChangeForm or Game.ActionVerb);
    }

    [Rule("rr:action.2")]
    [Rule("rr:ability.8.2")]
    [Fact]
    public void APlayersForcedActionAsksThatPlayerToChooseItsPayment()
    {
        var runner = Runner(
            AuthoredCards.Hunted,
            "ForcedAction",
            """{ "discard": "this" }""",
            cost: """{ "discardFromHand": 1 }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = board.CreateCard(
                AuthoredCards.Hunted,
                board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1))),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);

        Assert.Equal(1, game.Pending!.Player);
        var forced = Assert.Single(game.Pending.Affordances);
        Assert.NotNull(forced.Targets);
        var targets = forced.Targets;
        Assert.All(
            targets.Legal,
            id => Assert.Contains(world.Cards[id], world.Seats[1].Hand.Cards));
        int p0Held = world.Seats[0].Hand.Cards.Count;
        var paid = world.Cards[targets.Legal[0]];

        game.Resolve(Decision.Take(forced.Id, [paid.ObjectId], []));

        Assert.Equal(p0Held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, paid.Area.Type);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:action.2")]
    [Fact]
    public void ACostlessForcedActionNeedOnlyResolveOnceBeforeThePhaseEnds()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "ForcedAction",
            """{ "draw": { "player": "you", "count": 1 } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        game.Resolve(Decision.Decline);
        var forced = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;

        game.Resolve(Decision.Take(forced.Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:action.2")]
    [Rule("rr:player-elimination.step.5")]
    [Fact]
    public void EliminatingTheFirstPlayerDuringTheGateMovesTheEndPhaseToTheSurvivor()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "ForcedAction",
            """{ "dealDamage": { "cards": "you", "amount": 99 } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        var forced = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(forced.Id));

        Assert.True(world.Seats[0].Eliminated);
        Assert.Equal(1, world.FirstPlayer);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(1, game.Pending!.Player);
    }

    [Rule("rr:action.2")]
    [Fact]
    public void ForcedActionsOnTwoFacesOfOneIdentityAreDistinctAbilities()
    {
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "{{AuthoredCards.SpiderMan}}", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "changeForm": { "player": "you", "to": "alter-ego" } }
                  } ] },
                  { "card": "01001b", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  } ] }
                ] }
                """));
        var (game, world) = Playing(_ => { }, hero: true, abilities: runner);

        game.Resolve(Decision.Decline);
        var heroAction = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(heroAction.Id));

        Assert.Equal("01001b", world.Seats[0].IdentityCard.FaceId);
        var alterEgoAction = Assert.Single(game.Pending!.Affordances);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(alterEgoAction.Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:action.2")]
    [Rule("rr:leaves-play.1")]
    [Fact]
    public void AForcedActionOnACardThatLeavesAndReturnsBelongsToTheNewCopy()
    {
        var runner = Runner(
            "01101",
            "ForcedAction",
            """
            { "seq": [
              { "discard": "this" },
              { "putIntoPlay": { "card": "this", "where": "engagedWithYou" } },
              { "discard": "this" }
            ] }
            """,
            limit: 1);
        Card? source = null;
        var (game, _) = Playing(
            board => source = board.CreateCard(
                "01101",
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))),
            abilities: runner);
        int firstCopy = source!.Incarnation;

        game.Resolve(Decision.Decline);
        var forced = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(forced.Id));

        Assert.True(source.Incarnation > firstCopy);
        Assert.Equal(DeckType.EngagedEnemiesArea, source.Area.Type);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.False(game.Pending!.Cancellable);
        Assert.Contains(game.Pending.Affordances, option => option.AnchorId == source.ObjectId);
    }

    [Rule("rr:limit")]
    [Fact]
    public void LimitedAbilitiesOnOneCardHaveIndependentUses()
    {
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [ { "card": "{{AuthoredCards.AuntMay}}", "abilities": [
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  },
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  }
                ] } ] }
                """));
        var (game, world) = Playing(
            board => InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Take(game.Pending!.Affordances[0].Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.False(game.Pending!.Cancellable);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));

        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:event.1")]
    [Fact]
    public void PlayingAnEventChoosesOneOfItsTriggeredAbilities()
    {
        // Both actions belong to the same event. Their affordance ids retain
        // the printed ordinal, so choosing the second resolves only its two-card
        // draw and does not also resolve the first ability.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [ { "card": "01003", "abilities": [
                  { "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 1 } } },
                  { "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 2 } } }
                ] } ] }
                """));
        Card? eventCard = null;
        var (game, world) = Playing(
            board =>
            {
                Hand(board, AuthoredCards.Backflip, 0);
                eventCard = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var choices = game.Pending!.Affordances
            .Where(option => option.AnchorId == eventCard!.ObjectId)
            .ToList();

        Assert.Equal(2, choices.Count);
        game.Resolve(Decision.Take(choices[1].Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, eventCard!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:event.3")]
    [Fact]
    public void AnEventWithNoValidTargetCannotBeOfferedOrForged()
    {
        // An event requiring a minion target cannot be initiated on a board
        // with no minions. The same check runs again at execution, before the
        // event leaves the hand or a payment source can be spent.
        var runner = Runner(
            AuthoredCards.Backflip,
            "Action",
            """{ "chooseCard": { "from": { "query": "minions" }, "effect": { "discard": "chosen" } } }""");
        Card? card = null;
        var (_, world) = Playing(
            board =>
            {
                Hand(board, AuthoredCards.Backflip, 0);
                card = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            runner.Actions(world, 0), action => action.Card == card!.ObjectId);

        var forged = new PendingAbility(card!.ObjectId, AbilityType.Action, 0);
        Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [], []));
        Assert.Same(world.Seats[0].Hand, card.Area);
    }

    [Rule("rr:limit.1")]
    [Fact]
    public void ACancelledLimitedAttackStillUsesItsLimit()
    {
        var runner = Runner(
            "01017",
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "attack": { "target": "chosen", "effect": { "dealAttackDamage": { "cards": "chosen", "amount": 1 } } } } } }""",
            limit: 1);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
            },
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(villain.ObjectId));

        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, villain.Damage);
        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:max-maximum")]
    [Rule("rr:max-maximum.1")]
    [Fact]
    public void APeriodMaximumIsSharedByTitleAcrossPlayersAndExpires()
    {
        // “Max 1 per round” is across all copies by title for all players,
        // unlike a Limit, which belongs to each instance of an ability.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            maximum: "Round");
        Card? first = null;
        Card? second = null;
        var (_, world) = Playing(
            board =>
            {
                first = InPlay(board, AuthoredCards.AuntMay);
                second = board.CreateCard(
                    AuthoredCards.AuntMay,
                    board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == first!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.DoesNotContain(
            runner.Actions(world, 1), pending => pending.Card == second!.ObjectId);
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Contains(
            runner.Actions(world, 1), pending => pending.Card == second!.ObjectId);
    }

    [Rule("rr:max-maximum.1")]
    [Fact]
    public void APhaseMaximumExpiresAtTheEndOfEitherPhase()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            maximum: "Phase");
        Card? first = null;
        Card? second = null;
        var (_, world) = Playing(
            board =>
            {
                first = InPlay(board, AuthoredCards.AuntMay);
                second = InPlay(board, AuthoredCards.AuntMay);
            },
            abilities: runner);
        runner.Act(
            world,
            Assert.Single(runner.Actions(world, 0), pending =>
                pending.Card == first!.ObjectId),
            [],
            []);

        Assert.DoesNotContain(
            runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Contains(
            runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
    }

    [Rule("rr:max-maximum.1")]
    [Fact]
    public void AGameMaximumSurvivesPhaseAndRoundBoundaries()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            maximum: "Game");
        Card? first = null;
        Card? second = null;
        var (_, world) = Playing(
            board =>
            {
                first = InPlay(board, AuthoredCards.AuntMay);
                second = InPlay(board, AuthoredCards.AuntMay);
            },
            abilities: runner);
        runner.Act(
            world,
            Assert.Single(runner.Actions(world, 0), pending =>
                pending.Card == first!.ObjectId),
            [],
            []);

        world.Effects.Expire(TimingPoints.EndOfPhase);
        world.Effects.Expire(TimingPoints.EndOfRound);

        Assert.DoesNotContain(
            runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
    }

    [Rule("rr:max-maximum.1.1")]
    [Fact]
    public void ACanceledUseStillCountsTowardACardMaximum()
    {
        var runner = Runner(
            "01017",
            "Action",
            """{ "attack": { "target": { "query": "villain" }, "effect": { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } } } }""",
            maximum: "Round");
        Card? first = null;
        Card? second = null;
        var (_, world) = Playing(
            board =>
            {
                first = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                second = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == first!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.DoesNotContain(
            runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
    }

    [Rule("rr:max-maximum.5")]
    [Fact]
    public void APerInstanceMaximumIsSharedAcrossCopiesForOneOccurrence()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Response",
            """{ "draw": { "player": "you", "count": 1 } }""",
            eventName: "WhenDamageWouldBeDealt",
            maximum: "Instance");
        Card? first = null;
        Card? second = null;
        var (_, world) = Playing(
            board =>
            {
                first = InPlay(board, AuthoredCards.AuntMay);
                second = InPlay(board, AuthoredCards.AuntMay);
            },
            abilities: runner);
        var occurrence = new Occurrence(
            91,
            ["WhenDamageWouldBeDealt"],
            Player: 0,
            Target: world.Seats[0].IdentityCard.ObjectId);
        var offered = runner.Waiting(world, occurrence, WindowKind.Response);

        Assert.Equal(2, offered.Count);
        runner.Resolve(world, occurrence, offered.Single(pending =>
            pending.Card == first!.ObjectId), [], []);

        Assert.Empty(runner.Waiting(world, occurrence, WindowKind.Response));
        Assert.Equal(
            2,
            runner.Waiting(
                world,
                new Occurrence(
                    92,
                    ["WhenDamageWouldBeDealt"],
                    Player: 0,
                    Target: world.Seats[0].IdentityCard.ObjectId),
                WindowKind.Response).Count);
        Assert.NotNull(second);
    }

    [Rule("rr:max-maximum.6")]
    [Fact]
    public void AMaximumWithinAnAbilityCapsOnlyThatResolution()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "dealDamage": {
              "cards": { "query": "villain" },
              "amount": { "min": [ 20, 10 ] }
            } }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.Equal(10, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:labeled-ability.5")]
    [Rule("rr:labeled-ability.6")]
    [Rule("rr:labeled-ability.6.2")]
    [Fact]
    public void MultiLabeledAbilityCancelsOnceAfterCostsAndBeforeAnyEffect()
    {
        // Crosscounter's attack/defense/thwart labels are one ability. A stun
        // or confusion cancels the whole post-arrow effect, removes every
        // matching status, and leaves the already-paid exhaustion cost paid.
        var runner = Runner(
            "01017",
            "Action",
            """{ "draw": { "player": "you", "count": 1 } }""",
            cost: """{ "exhaust": "this" }""",
            limit: 1,
            labels: "[ \"attack\", \"defense\", \"thwart\" ]");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Confused);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.False(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.False(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Confused));
    }

    [Rule("rr:labeled-ability.2")]
    [Rule("rr:labeled-ability.6")]
    [Rule("rr:retaliate-x.1")]
    [Fact]
    public void LabeledPowerDoesNotBeginAgainDuringItsEffect()
    {
        // A labeled ability is canceled "when the player initiates" it. The
        // stun gained after initiation therefore remains in play and cannot
        // retroactively cancel the attack child of the already-running ability.
        var runner = Runner(
            "01017",
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "giveStatus": { "card": "you", "status": "stunned" } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            labels: "[ \"attack\" ]");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.LastingEffect,
                    Kind: "retaliate",
                    Amount: 1,
                    Card: villain.ObjectId,
                    Affects: villain.ObjectId));
            },
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.True(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(1, villain.Damage);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:labeled-ability.1")]
    [Rule("rr:upgrade.4")]
    [Rule("rr:piercing.1")]
    [Fact]
    public void LabeledPerformerSurvivesAChoiceContinuation()
    {
        // An upgrade attached to "another friendly character" attributes its
        // labeled ability to that character. The chosen ally remains the
        // performer after the prompt, so its Piercing discards each Tough card.
        var runner = Runner(
            "01017",
            "Action",
            """
            { "chooseCard": {
              "from": { "query": "attackableEnemies" },
              "effect": { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            } }
            """,
            labels: "[ \"attack\" ]");
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(
            board =>
            {
                ally = board.CreateCard(
                    AuthoredCards.BlackCat,
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId, cardOwner: 0));
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.LastingEffect,
                    Kind: Keywords.Piercing,
                    Card: ally.ObjectId,
                    Affects: ally.ObjectId));
            },
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, villain, Statuses.Tough);
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(villain.ObjectId));

        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(1, villain.Damage);
    }

    [Rule("rr:labeled-ability.2")]
    [Fact]
    public void AttackEnvelopeWithoutAPowerLifecycleFailsBeforeCosts()
    {
        // The whole labeled ability "is considered to be an attack". A raw
        // damage effect has no saveable attack occurrence for interrupts,
        // responses, or Retaliate, so this unsupported shape is refused before
        // its exhaust cost instead of resolving as plausible non-attack damage.
        var runner = Runner(
            "01017",
            "Action",
            """{ "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } }""",
            cost: """{ "exhaust": "this" }""",
            labels: "[ \"attack\" ]");
        Card? source = null;
        var (_, world) = Playing(
            board => source = board.CreateCard(
                "01017",
                board.AreaOf(
                    DeckType.UpgradesArea, PlayArea.Of(0),
                    board.Seats[0].IdentityCard.ObjectId, cardOwner: 0)),
            hero: true);
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [], []));

        Assert.Contains("saveable attack power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source.Ready);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:labeled-ability.2")]
    [Fact]
    public void AutomaticAttackEnvelopeCannotBypassLifecyclePreflight()
    {
        // Automatic entry points use the same envelope gate as Actions. A When
        // Revealed ability with raw attack damage therefore raises before the
        // damage instead of bypassing the attack occurrence and Retaliate.
        var runner = Runner(
            "01017",
            "WhenRevealed",
            """{ "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } }""",
            eventName: Steps.CardRevealed,
            labels: "[ \"attack\" ]");
        Card? source = null;
        var (_, world) = Playing(
            board => source = board.CreateCard(
                "01017", board.AreaOf(DeckType.RevealingArea)),
            hero: true);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => runner.WhenRevealed(world, source!, 0));

        Assert.Contains("saveable attack power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:labeled-ability.2")]
    [Fact]
    public void EveryAttackEnvelopeBranchMustEnterTheLifecycle()
    {
        // An inactive branch cannot lend its attack node to the active branch.
        // Here the hero-form path only draws, so the envelope would not be an
        // attack on that path and is rejected before the exhaust cost.
        var runner = Runner(
            "01017",
            "Action",
            """
            { "if": {
              "test": { "inForm": { "player": "you", "form": "hero" } },
              "then": { "draw": { "player": "you", "count": 1 } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            labels: "[ \"attack\" ]");
        Card? source = null;
        var (_, world) = Playing(
            board => source = board.CreateCard(
                "01017",
                board.AreaOf(
                    DeckType.UpgradesArea, PlayArea.Of(0),
                    board.Seats[0].IdentityCard.ObjectId, cardOwner: 0)),
            hero: true);
        int held = world.Seats[0].Hand.Cards.Count;
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);

        Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [], []));

        Assert.True(source.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:labeled-ability.5")]
    [Rule("rr:labeled-ability.6")]
    [Fact]
    public void EnvelopeCannotHideAnUndeclaredPower()
    {
        // The envelope's labels are the whole set. An attack-only ability may
        // not append a thwart that skips Confused merely because the attack
        // already persisted its performer into the continuation.
        var runner = Runner(
            "01017",
            "Action",
            """
            { "seq": [
              { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } },
              { "thwart": {
                "target": { "query": "mainScheme" },
                "effect": { "removeThreat": {
                  "scheme": { "query": "mainScheme" }, "amount": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""",
            labels: "[ \"attack\" ]");
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                Statuses.Give(
                    board, board.Seats[0].IdentityCard, Statuses.Confused);
            },
            hero: true);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long threat = scheme.Tokens.GetValueOrDefault("k_threat");
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, forged, [], []));

        Assert.Contains("absent from its ability labels", thrown.Message);
        Assert.True(source.Ready);
        Assert.True(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(0, villain.Damage);
        Assert.Equal(threat, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:lasting-effects.6")]
    [Fact]
    public void AnUntilEndOfAttackEffectCannotBeginOutsideAnAttack()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "grantUntil": { "card": "this", "keyword": "attack", "amount": 1, "until": "EndOfAttack" } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:action.2.2")]
    [Rule("rr:forced.3")]
    [Rule("rr:forced.3.1")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnIllegalForcedActionDoesNotPreventPhaseCompletion(bool targetless)
    {
        string effect = targetless
            ? """{ "chooseCard": { "from": { "query": "minions" }, "effect": { "draw": { "player": "you", "count": 1 } } } }"""
            : """{ "draw": { "player": "you", "count": 1 } }""";
        string? cost = targetless ? null : """{ "spend": "BBBBBBBBBBBB" }""";
        var runner = Runner(AuthoredCards.AuntMay, "ForcedAction", effect, cost: cost);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Decline);

        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.True(source!.Ready);
    }

    [Rule("rr:cost")]
    [Rule("rr:player-turn.5")]
    [Fact]
    public void AnEncounterCardsActionIsAnybodysAndCostsResources()
    {
        // "Attach to Rhino. **Hero Action**: Spend [physical][physical][physical]
        // resources → discard this card." An encounter card in play, so
        // `rr:player-turn.5.b` is what lets a player trigger it -- it is
        // nobody's card and everybody's action.
        Card? horn = null;
        var (game, world) = Playing(
            board =>
            {
                horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));

                // Through the reveal: `rr:attach-to` makes "Attach to Rhino" a
                // rule about the card entering play rather than a "When
                // Revealed" ability, so the route in is what attaches it.
                board.Abilities = AuthoredCards.Runner();
                Reveal.Resolve(board, Cards, horn, 0, []);
                Physical(board, 3);
            },
            hero: true);

        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);

        // The cost is on the wire, with what could pay it -- a resource cost is
        // a choice of *which* cards, so the client has to be told.
        var price = Assert.Single(action.CostOptions);
        Assert.Equal("3", price.Cost);
        Assert.True(price.Generators.Count(source => source.Generates == "R") >= 3);

        int[] paying = [.. world.Seats[0].Hand.Cards
            .Where(card => card.FaceId == Physicals)
            .Take(3)
            .Select(card => card.ObjectId)];

        game.Resolve(Decision.Take(action.Id, [], paying));

        Assert.Equal(DeckType.EncounterDiscardPile, horn!.Area.Type);
        Assert.All(
            paying, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void AnActionIsNotOfferedToAHandThatCannotPayIt()
    {
        // The whole hand cannot make three physicals, so the action is not
        // offered at all. `rr:cost.4` permits generating beyond the cost, so
        // asking the whole hand is the right question rather than an
        // approximation: if everything together cannot pay, no choice among it
        // can.
        var (game, _) = Playing(
            board =>
            {
                var horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));
                AuthoredCards.Runner().WhenRevealed(board, horn, 0);
                Physical(board, 2);
            },
            hero: true);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
    }

    [Rule("rr:and")]
    [Rule("rr:and.1")]
    [Rule("rr:and.2")]
    [Rule("rr:first-player.3")]
    [Fact]
    public void AndEffectsResolveIndependentlyInsideOneAbility()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "dealDamage": { "cards": "you", "amount": 1 } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(Question.Order, game.Pending.Asking);
        game.Resolve(new Decision(order.Id, [0, 1]));

        // Tough independently prevents the damage effect. The draw connected
        // by “and” still resolves, and no response prompt separated the two.
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:first-player.3")]
    [Fact]
    public void FirstPlayerChoosesTheOrderOfAndEffects()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "exhaust": "this" }, { "ready": "this" } ] }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [1, 0]));

        Assert.False(source!.Ready);
    }

    [Rule("rr:first-player.3")]
    [Fact]
    public void AndOrderResumesThroughTheActiveConditionBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "and": [ { "exhaust": "this" }, { "ready": "this" } ] }, "else": { "and": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [1, 0]));

        Assert.False(source!.Ready);
    }

    [Rule("rr:and.1")]
    [Fact]
    public void AndResumesAfterAReachableNestedSuspender()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" }, "else": { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:then")]
    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Rule("rr:resolve.1")]
    [Theory]
    [InlineData(2, true)]
    [InlineData(1, false)]
    public void ThenRequiresThePrecedingEffectToResolveInFull(
        int threat, bool draws)
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", threat);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!
            .Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + (draws ? 1 : 0), world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:then.2")]
    [Fact]
    public void ThenIgnoresCharactersThatAreNotValidExhaustTargets()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "exhaust": { "query": "charactersYouControl" } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                ally = board.CreateCard(
                    "01002",
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                ally.Exhaust();
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.False(ally!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:then.2")]
    [Fact]
    public void ThenIgnoresSchemesThatAreNotValidThreatTargets()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "removeThreat": { "scheme": { "query": "sideSchemes" }, "amount": 2 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        Card? empty = null;
        Card? threatened = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                empty = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
                threatened = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
                threatened.PlaceTokens("k_threat", 2);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, empty!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, threatened!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AThreatPlacementBeforeThenRaisesBeforeChangingTheBoard()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("none/partial/full", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:otherwise")]
    [Rule("rr:otherwise.1")]
    [Rule("rr:otherwise.1.2")]
    [Rule("rr:otherwise.2")]
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void OtherwiseRequiresThePrecedingEffectToResolveNotAtAll(
        int threat, bool draws)
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            // `effect` explicitly delimits the preceding effect. That preserves
            // rr:otherwise.2's semicolon/sentence boundary without recovering
            // punctuation after printed text has become an ability tree.
            """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", threat);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!
            .Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + (draws ? 1 : 0), world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void OtherwiseResolvesWhenThreatRemovalIsProhibited()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "titled": "Countdown to Oblivion" }, "amount": 2 } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""",
            includeAuthored: true);
        Card? source = null;
        Card? scheme = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard("01139b", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise")]
    [Fact]
    public void OtherwiseTreatsAMissingPrecedingTargetAsNoResolution()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "discard": { "titled": "Missing Card" } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.1")]
    [Fact]
    public void OtherwiseResolvesWhenThePrecedingConditionIsFalse()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" } } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.True(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void AnUnusedOtherwiseBranchDoesNotAskForItsChoice()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "otherwise": { "choose": { "options": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] } } } }""",
            limit: 1);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 1);
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!
            .Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.True(source!.Ready);
    }

    [Rule("rr:then.1")]
    [Fact]
    public void AChoiceInAnInactiveConditionBranchDoesNotSuspendThen()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" }, "else": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADependentEffectResumesAfterItsChoice()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "choose": { "options": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] } }, "then": { "discard": "this" } } }""");
        Card? source = null;

        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }

    [Fact]
    public void ADependentEachPlayerEffectResumesTheAbility()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "draw": { "player": "you", "count": 1 } }, "then": { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADependentFirstActivationPersistsItsOuterContinuation()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "seq": [ { "then": { "effect": { "draw": { "player": "you", "count": 1 } }, "then": { "enemyAttacks": { "enemies": { "query": "villain" }, "first": "true" } } } }, { "draw": { "player": "you", "count": 1 } } ] }""",
            eventName: Steps.CardRevealed);
        Card? source = null;

        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, source!, 0);
        var attack = Assert.Single(
            world.Agenda.Outstanding, pending => pending.What == Steps.Attack);

        runner.ActivationCompleted(world, new EnemyActivation(
            attack.Subject, attack.Seat, Attacking: true, attack.ActivationId, Made: false));

        Assert.True(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AChoiceOptionCanSuspendInsideAnd()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "and": [ { "draw": { "player": "you", "count": 1 } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } ] } ] } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(1));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.False(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ACostCanSwitchAnIfIntoAResumableBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "and": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ACostCannotSwitchAnIfIntoAnUnknownDependentOutcome()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "then": { "effect": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void ACostCannotSwitchADependentPredecessorToAnUnknownOutcome()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } } } }, "then": { "draw": { "player": "you", "count": 1 } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AnEarlierStepCannotSwitchADependentPredecessorToAnUnknownOutcome()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "changeForm": { "player": "you", "to": "alterEgo" } }, { "then": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "alterEgo" } }, "then": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "else": { "draw": { "player": "you", "count": 1 } } } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void ACostCanActivateAResumableOtherwiseBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AnEarlierStepCanActivateAResumableOtherwiseBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "exhaust": "this" }, { "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } } ] }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ANestedSequenceResumesPastAnEarlierMutationBoundary()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "exhaust": "this" }, { "seq": [ { "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } } ] } ] }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADirectSequencePersistsItsNestedContinuationAfterItsFirstMutation()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "seq": [ { "draw": { "player": "you", "count": 1 } }, { "and": [ { "draw": { "player": "you", "count": 1 } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } ] } ] }""",
            eventName: Steps.CardRevealed);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, source!, 0);

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.ChooseOption, waiting.What);
        Assert.Equal(["seq:1"], waiting.AbilityPath);
    }

    [Rule("rr:and.1")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void AnOrderedCardAttackResumesTheRemainingSimultaneousEffect()
    {
        // The first player chooses the order of the independent effects. The
        // attack has its own agenda procedure, so the later draw must wait for
        // that procedure and then resume from the exact `and` branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "attack": { "target": { "query": "villain" }, "effect": { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(1, villain.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void LeavingTheFinalEachPlayerFrameRestoresTheOriginalResolver()
    {
        // Each frame reads "you" as that frame's player. Text after the frame
        // belongs to the player resolving the ability, not whichever player
        // the first player put last in the chosen order.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int firstHeld = world.Seats[0].Hand.Cards.Count;
        int secondHeld = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(
            order.Id,
            [world.Seats[0].IdentityCard.ObjectId, world.Seats[1].IdentityCard.ObjectId]));

        Assert.Equal(firstHeld + 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHeld + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:and.1")]
    [Fact]
    public void UnsupportedThreatPlacementOrderingRaisesBeforeTheActionCost()
    {
        // Threat placement has its own interrupt and response windows. Until
        // that agenda record carries a structural card continuation, accepting
        // an order that places it first would silently skip the later effect.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, { "draw": { "player": "you", "count": 1 } } ] }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("threat placement continuation", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void NestedEachPlayerFramesRaiseBeforeTheActionCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "choose": { "options": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("nests one each-player", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void ACardPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LegalPracticePowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // A labeled thwart ability "is considered to be a thwart made by that
        // player's identity." Its whole nested power must be preflighted.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "legalPractice": { "schemes": { "query": "thwartableSchemes" }, "power": { "thwart": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Rule("rr:for-each")]
    [Fact]
    public void LegalPracticeCanBindAForEachPowerAmountAfterItIsOffered()
    {
        // powerAmount is unbound while the Legal Practice prompt is built.
        // That sentinel is not an authored negative count: the selected hand
        // cards bind it before the labelled thwart effect resolves.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "legalPractice": {
              "schemes": { "query": "thwartableSchemes" },
              "power": { "thwart": {
                "target": "chosen",
                "effect": { "forEach": {
                  "count": { "powerAmount": "cardsDiscarded" },
                  "effect": { "removeThreat": {
                    "scheme": "chosen", "amount": 1
                  } }
                } }
              } }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "01151", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DifferentSchemesPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // A labeled thwart ability "is considered to be a thwart made by that
        // player's identity." Its whole nested power must be preflighted.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "thwartDifferentSchemes": { "schemes": { "query": "thwartableSchemes" }, "power": { "thwart": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void DelayedPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // The delayed subtree is still the action's effect. Preflight must see
        // its labeled attack before any cost is paid or activation state changes.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "afterActivation": { "effect": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Activation = new EnemyActivation(
                    board.TheCardIn(DeckType.VillainArea)!.ObjectId,
                    Player: 0,
                    Attacking: true,
                    Id: 41);
            },
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void AStableFormBranchIgnoresAnUnreachableSuspendingPower()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:form-change-form")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void APredecessorMutationPreflightsEveryReachableDependentBranch()
    {
        // Changing form flips which dependent branch resolves. The unsupported
        // labeled attack must be found before the form change or cost occurs.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "changeForm": { "player": "you", "to": "alterEgo" } }, "then": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void EachPlayerPreflightUsesEveryPlayersCurrentForm()
    {
        // "For each player" reaches both identities. A safe branch for the
        // initiating hero cannot hide an unsupported branch for an alter-ego.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void ChosenCardContextIsPreflightedBeforeTheActionCost()
    {
        // The choice binds "chosen" before its effect resolves. Every branch
        // that binding can open must be checked before the cost is paid.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "if": { "test": { "exists": "chosen" }, "then": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } }, "else": { "draw": { "player": "you", "count": 1 } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void ChosenCardDoesNotDestabilizeAnUnrelatedFormBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void ChosenPlayerBindingIsPreflightedAfterSelection()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "identities" }, "effect": { "if": { "test": { "inForm": { "player": "chosenPlayer", "form": "hero" } }, "then": { "draw": { "player": "chosenPlayer", "count": 1 } }, "else": { "draw": { "player": "chosenPlayer", "count": 1 } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void SequenceFormReachabilityWaitsForAChosenPlayerBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "if": {
                  "test": { "inForm": {
                    "player": "chosenPlayer", "form": "hero"
                  } },
                  "then": { "changeForm": {
                    "player": "chosenPlayer", "to": "alterEgo"
                  } },
                  "else": { "draw": {
                    "player": "chosenPlayer", "count": 1
                  } }
                } }
              } },
              { "if": {
                "test": { "inForm": {
                  "player": "chosenPlayer", "form": "alterEgo"
                } },
                "then": { "draw": {
                  "player": "chosenPlayer", "count": 1
                } },
                "else": { "draw": {
                  "player": "chosenPlayer", "count": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Fact]
    public void LaterChosenPlayerTargetsAreCheckedAgainstTheOfferedCandidates()
    {
        // “The act of choosing a game element … makes that game element a
        // target.” Both offered identities have an engaged enemy, so the
        // continuation has a valid target whichever identity is selected.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } },
              { "dealDamage": {
                "cards": { "query": "enemiesEngagedWithChosenPlayer" },
                "amount": 1
              } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01167",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void AChosenEnemyCanTargetALaterAttackWrapper()
    {
        // The earlier choice establishes the target used by the later labelled
        // attack. Every offered enemy is currently attackable and damageable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "attackableEnemies" },
                "effect": { "seq": [] }
              } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:guard")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ContinuationFilteringRemovesUnsafeEarlierBindingCandidates()
    {
        // A player-card option may be chosen if it can resolve at least
        // partially. Choosing the no-op option preserves the first selected
        // enemy, so the Guard-protected villain is removed from the first
        // prompt while the minion path keeps the costed action legal.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "enemies" },
                "effect": { "seq": [] }
              } },
              { "choose": { "options": [
                { "chooseCard": {
                  "from": { "query": "attackableMinions" },
                  "effect": { "seq": [] }
                } },
                { "seq": [] }
              ] } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(
            game.Pending!.Affordances, option => option.Id == minion!.ObjectId);
        Assert.DoesNotContain(
            game.Pending.Affordances, option => option.Id == villain.ObjectId);
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ANoOpOptionCanPreserveTheAbsenceOfABinding()
    {
        // The empty option is legal as a decline branch, but it leaves
        // `chosen` unanswered. A later attack requiring that target therefore
        // makes the whole costed action unsafe to initiate.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "choose": { "options": [
                { "chooseCard": {
                  "from": { "query": "attackableMinions" },
                  "effect": { "seq": [] }
                } },
                { "seq": [] }
              ] } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnEmptyEachPlayerFramePreservesAnEarlierBinding()
    {
        // “Each player” frames resolve in the order chosen by the first
        // player. A frame with no legal minion is a no-op, so it preserves the
        // enemy selected before the frame instead of erasing that target.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "attackableEnemies" },
                "effect": { "seq": [] }
              } },
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": { "query": "minionsEngagedWithYou" },
                "effect": { "seq": [] }
              } } } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ASelectorCanDependOnEveryEarlierBindingCandidate()
    {
        // The first target determines which engaged area the second selector
        // reads. Every offered identity has a legal minion, so preflight must
        // evaluate the selector once under each possible earlier binding.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "seq": [] }
              } },
              { "chooseCard": {
                "from": { "query": "enemiesEngagedWithChosenPlayer" },
                "effect": { "seq": [] }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01167",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnEarlierPromptFiltersBindingsWithNoLegalContinuation()
    {
        // At least one identity has a valid target for the later choice, so
        // the action can initiate. The identity without one is not a legal
        // target of the unresolved ability and must not be offered.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "seq": [] }
              } },
              { "chooseCard": {
                "from": { "query": "enemiesEngagedWithChosenPlayer" },
                "effect": { "seq": [] }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(
            game.Pending.Affordances,
            option => option.Id == world.Seats[1].IdentityCard.ObjectId);
    }

    [Rule("rr:choose-option.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnUnavailableOptionDoesNotCreateAnEmptyBindingOutcome()
    {
        // The minion option has no valid target and is unavailable. Only the
        // villain option can run, and it supplies the target for the attack
        // that follows, so the unavailable branch contributes no empty path.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "choose": { "options": [
                { "chooseCard": {
                  "from": { "query": "attackableMinions" },
                  "effect": { "seq": [] }
                } },
                { "chooseCard": {
                  "from": { "query": "attackableEnemies" },
                  "effect": { "seq": [] }
                } }
              ] } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:choose-option.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnOptionPromptFiltersBranchesThatCannotSatisfyTheContinuation()
    {
        // Both nested selectors are locally legal, but the enemy branch leaves
        // no player for the later draw. The outer option itself is therefore
        // unavailable; the costed prompt must expose only the identity branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "choose": { "options": [
                { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "seq": [] }
                } },
                { "chooseCard": {
                  "from": { "query": "attackableEnemies" },
                  "effect": { "seq": [] }
                } }
              ] } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(game.Pending!.Affordances, option => option.Id == 0);
        Assert.DoesNotContain(game.Pending.Affordances, option => option.Id == 1);
    }

    [Rule("rr:choose-option.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void SuffixValidationReplacesThePreOptionCandidateState()
    {
        // The first prompt binds an identity. The enemy option would replace
        // it with the villain, which cannot supply the chosen player required
        // by the final engaged-enemy selector. Recursive suffix validation must
        // use that post-option villain, not the stale identity candidate list.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "seq": [] }
              } },
              { "choose": { "options": [
                { "chooseCard": {
                  "from": { "query": "attackableEnemies" },
                  "effect": { "seq": [] }
                } },
                { "seq": [] }
              ] } },
              { "chooseCard": {
                "from": { "query": "enemiesEngagedWithChosenPlayer" },
                "effect": { "seq": [] }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(world.Seats[0].IdentityCard.ObjectId));

        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Id == 0);
        Assert.Contains(game.Pending.Affordances, option => option.Id == 1);
    }

    [Rule("rr:target.2.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void SuffixValidationUsesTheActiveBranchForEachConcreteBinding()
    {
        // Each identity has a legal target in only the branch selected by that
        // identity's form. Once suffix validation installs one candidate, the
        // binding is concrete: requiring both branches would reject every
        // identity after the source had already paid its exhaust cost.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "seq": [] }
              } },
              { "if": {
                "test": { "inForm": {
                  "player": "chosenPlayer", "form": "hero"
                } },
                "then": { "chooseCard": {
                  "from": { "query": "topmostTechInChosenDiscard" },
                  "effect": { "seq": [] }
                } },
                "else": { "chooseCard": {
                  "from": { "query": "enemiesEngagedWithChosenPlayer" },
                  "effect": { "seq": [] }
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? tech = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                tech = board.CreateCard(
                    "01007",
                    board.AreaOf(
                        DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
                board.CreateCard(
                    "01167",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.Contains(
            game.Pending.Affordances,
            option => option.Id == world.Seats[1].IdentityCard.ObjectId);

        game.Resolve(Decision.Take(world.Seats[0].IdentityCard.ObjectId));

        Assert.Contains(game.Pending!.Affordances, option => option.Id == tech!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AChoiceEffectContributesItsFinalBindingToTheContinuation()
    {
        // The nested enemy choice replaces the identity chosen by the outer
        // prompt. It leaves no chosen player for the later draw, so the costed
        // action must be refused before it exhausts its source.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "chooseCard": {
                  "from": { "query": "attackableEnemies" },
                  "effect": { "seq": [] }
                } }
              } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:then")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void ADependentContinuationFiltersItsPredecessorPrompt()
    {
        // The predecessor resolves fully after either identity is selected,
        // so “then” reaches the engaged-enemy choice. Only player 0 supplies a
        // target there and only that identity may be offered before payment.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "then": {
              "effect": { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "draw": {
                  "player": "you", "count": 1
                } }
              } },
              "then": { "chooseCard": {
                "from": { "query": "enemiesEngagedWithChosenPlayer" },
                "effect": { "seq": [] }
              } }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(
            game.Pending.Affordances,
            option => option.Id == world.Seats[1].IdentityCard.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void ANonFinalEachPlayerPromptIgnoresTheOuterContinuation()
    {
        // Only the final frame's binding reaches the outer continuation. The
        // first player's own mandatory choice therefore remains available even
        // though that player's binding could not satisfy the later selector.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": "you", "effect": { "seq": [] }
              } } } },
              { "chooseCard": {
                "from": { "query": "enemiesEngagedWithChosenPlayer" },
                "effect": { "seq": [] }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01167",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(Question.Order, game.Pending.Asking);
        game.Resolve(new Decision(order.Id,
        [
            world.Seats[0].IdentityCard.ObjectId,
            world.Seats[1].IdentityCard.ObjectId,
        ]));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
    }

    [Rule("rr:then")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void ANestedDependentChoiceIsRejectedBeforeItsCost()
    {
        // The outer answer does not determine whether the complete predecessor
        // resolved: its nested choice and any later siblings still have to run.
        // Until that aggregate outcome is modelled, fail before paying a cost.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "then": {
              "effect": { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "draw": { "player": "you", "count": 1 } }
                } }
              } },
              "then": { "draw": {
                "player": "chosenPlayer", "count": 1
              } }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("multiple-stage player choices", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:then")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void SiblingChoicesBeforeDependentTextAreRejectedBeforeTheirCost()
    {
        // The first answer cannot classify the complete predecessor while a
        // sibling choice and a later effect remain unresolved. Fail closed
        // before exhaustion rather than recording the first leaf's outcome.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "then": {
              "effect": { "seq": [
                { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "draw": { "player": "you", "count": 1 } }
                } },
                { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "draw": { "player": "you", "count": 1 } }
                } },
                { "heal": { "card": "you", "amount": 1 } }
              ] },
              "then": { "draw": { "player": "chosenPlayer", "count": 1 } }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("multiple-stage player choices", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnEmptyEachPlayerOutcomeInvalidatesAChosenPlayerDraw()
    {
        // Player 0 has a hero target and player 1 does not. If player 1's frame
        // resolves last, the outer draw has no chosen player; unlike a card
        // prompt, the order decision cannot filter out that reachable outcome.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": "yourHero",
                "effect": { "seq": [] }
              } } } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:target.2")]
    [Fact]
    public void AMixedBindingPromptKeepsTheDrawablePlayerPath()
    {
        // The selector includes an identity and the villain. At least one path
        // supplies the player required by the later draw, so the action is
        // legal and the scenario-owned character is filtered from its prompt.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "characters" },
                "effect": { "seq": [] }
              } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(
            game.Pending.Affordances, option => option.Id == villain.ObjectId);
    }

    [Rule("rr:for-each.3")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnEarlyForEachPromptAccountsForTheLaterIteration()
    {
        // The first iteration may choose player 0 even though only player 1 can
        // satisfy the outer selector: the second iteration asks again and its
        // answer is the binding that reaches the continuation.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "forEach": {
                "count": 2,
                "effect": { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "seq": [] }
                } }
              } },
              { "chooseCard": {
                "from": { "query": "enemiesEngagedWithChosenPlayer" },
                "effect": { "seq": [] }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01167",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
    }

    [Rule("rr:alteration-effect")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnEachTimePromptKeepsTheOuterFilterWhenLaterBodiesAreSkipped()
    {
        // The first discarded card matches and asks for a character; the next
        // card is not Kree, so no later prompt will replace that binding. The
        // first prompt must therefore apply the outer chosen-player draw and
        // exclude the scenario-owned villain before the cost is exposed.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """
            { "seq": [
              { "eachTime": {
                "effect": { "discardTop": {
                  "from": "encounterDeck", "count": 2
                } },
                "when": { "cardSet": {
                  "card": "that", "set": "kree_fanatic"
                } },
                "then": { "chooseCard": {
                  "from": { "query": "characters" },
                  "effect": { "seq": [] }
                } }
              } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """,
            eventName: Steps.CardRevealed);
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }
        world.CreateCard(AuthoredCards.ImTough, deck);
        world.CreateCard("90001", deck);
        var source = world.CreateCard(
            AuthoredCards.AuntMay, world.AreaOf(DeckType.RevealingArea));
        world.Abilities = runner;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = runner.WhenRevealed(world, source, 0).ToList();
        var prompt = Sequence.Work(world, Cards, runner, events)!;

        Assert.Contains(
            prompt.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(
            prompt.Affordances, option => option.Id == villain.ObjectId);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:activation.8")]
    [Fact]
    public void AChosenPlayerBindingSurvivesAnActivationContinuation()
    {
        // The selection is part of the unresolved ability when an activation
        // “resolves after the current … ability.” Resuming that same ability
        // must retain which identity the player selected.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "chooseCard": {
              "from": { "query": "identities" },
              "effect": { "seq": [
                { "enemyAttacks": { "enemies": { "query": "villain" } } },
                { "draw": { "player": "chosenPlayer", "count": 1 } }
              ] }
            } }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(
            runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
        runner.Act(world, action, [], []);
        var choice = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ChooseOption);
        runner.Chose(
            world, source!, 0, choice.Index,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), choice.Tier);
        var attack = Assert.Single(
            world.Agenda.Outstanding, pending => pending.What == Steps.Attack);
        runner.ActivationCompleted(world, new EnemyActivation(
            attack.Subject, attack.Seat, Attacking: true,
            attack.ActivationId, Made: false));

        Assert.Equal(held + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void ALabelledPowerRestoresTheOuterChosenBindingBeforeContinuing()
    {
        // The attack's villain target is not the identity selected by the
        // outer ability. The power uses its target while resolving, then the
        // unresolved outer sentence again refers to the selected player.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "seq": [] }
              } },
              { "attack": {
                "target": { "query": "villain" },
                "effect": { "seq": [
                  { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  { "draw": { "player": "chosenPlayer", "count": 1 } }
                ] }
              } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        long damage = villain.Damage;
        var action = Assert.Single(
            runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
        runner.Act(world, action, [], []);
        var choice = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ChooseOption);

        runner.Chose(
            world, source!, 0, choice.Index,
            Decision.Take(world.Seats[1].IdentityCard.ObjectId), choice.Tier);
        var attack = Assert.Single(
            world.Agenda.Outstanding,
            step => step.What == Steps.CharacterAttacks);
        runner.ResolveCardAttack(
            world, attack.CharacterAttack!, attack.OccurrenceOf(world, Cards), []);

        Assert.Equal(damage + 1, villain.Damage);
        Assert.Equal(held + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void EveryEachPlayerFrameContributesLaterBindingCandidates()
    {
        // The first player decides the frame order. The final frame can bind
        // either player's engaged minion, so the later attack must account for
        // Madame Hydra's “cannot take damage” prohibition before paying costs.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": { "query": "minionsEngagedWithYou" },
                "effect": { "seq": [] }
              } } } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
                var legions = board.CreateCard(
                    "01180", board.AreaOf(DeckType.SideSchemesArea));
                legions.PlaceTokens("k_threat", 1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnEmptyFinalEachPlayerFrameRemainsAReachableBindingOutcome()
    {
        // Every player frame restores the binding from before “each player.”
        // If the player with no minion resolves last, no target is persisted
        // for the later attack and the cost must not be paid first.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": { "query": "minionsEngagedWithYou" },
                "effect": { "seq": [] }
              } } } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void NestedChoiceCandidatesReplaceTheOuterChosenPlayerDuringValidation()
    {
        // Candidate validation must read the identity currently being offered,
        // not the hero chosen by the outer question. The alter-ego candidate
        // reaches an attack targeting that identity and is therefore illegal.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "chooseCard": {
              "from": { "query": "identities" },
              "effect": { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "if": {
                  "test": { "inForm": {
                    "player": "chosenPlayer", "form": "hero"
                  } },
                  "then": { "draw": {
                    "player": "chosenPlayer", "count": 1
                  } },
                  "else": { "attack": {
                    "target": "chosen",
                    "effect": { "dealAttackDamage": {
                      "cards": "chosen", "amount": 1
                    } }
                  } }
                } }
              } }
            } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(world.Seats[0].IdentityCard.ObjectId));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(
            game.Pending.Affordances,
            option => option.Id == world.Seats[1].IdentityCard.ObjectId);
    }

    [Rule("rr:then")]
    [Fact]
    public void ChosenTargetCanMakeADependentContinuationReachable()
    {
        // "Then" resolves only after its predecessor resolves in full. Binding
        // the threatened scheme makes that predecessor reachable and mutable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "thwartableSchemes" }, "effect": { "then": { "effect": { "removeThreat": { "scheme": "chosen", "amount": 1 } }, "then": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;
        long threat = -1;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.TheCardIn(DeckType.MainSchemesArea)!;
                threat = scheme.Tokens.GetValueOrDefault("k_threat");
            },
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(threat, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerPreflightIncludesMutationsFromEarlierFrames()
    {
        // The first player can remove the source before another player's frame
        // tests whether its title remains in play.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "discard": "this" }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void EachPlayerDrawDoesNotDestabilizeEveryPlayersHeroForm()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void ACardTitleContainingChosenIsNotAChoiceBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "chooseCard": {
              "from": { "query": "attackableEnemies" },
              "effect": { "if": {
                "test": { "titleInPlay": "Kang's Chosen" },
                "then": { "attack": {
                  "target": "chosen",
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } },
                "else": { "draw": { "player": "you", "count": 1 } }
              } }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerPreflightIncludesDefeatFromEarlierFrames()
    {
        // Damage tokens equal to remaining hit points defeat a minion. A later
        // player's title test therefore sees a different in-play board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Hydra Mercenary" }, "then": { "dealDamage": { "cards": { "titled": "Hydra Mercenary" }, "amount": 3 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? minion = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerPreflightUnionsEveryPlayersActiveMutationPath()
    {
        // The first player chooses the order. A hero can discard the source
        // before an alter-ego's different branch tests whether it remains.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "you", "form": "hero" } },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "titleInPlay": "Aunt May" },
                "then": { "draw": { "player": "you", "count": 1 } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
            """);
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void RepeatedMutationAnalysisDoesNotReadAnUnansweredChosenPlayer()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "if": {
                  "test": { "inForm": { "player": "chosenPlayer", "form": "hero" } },
                  "then": { "draw": { "player": "chosenPlayer", "count": 1 } },
                  "else": { "draw": { "player": "chosenPlayer", "count": 1 } }
                } }
              } },
              "else": { "draw": { "player": "you", "count": 1 } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:form-change-form")]
    [Fact]
    public void RepeatedMutationAnalysisIncludesLabelledPowerEffects()
    {
        // The first player can order another hero first. That hero's attack
        // flips the first player before the first player's own frame resolves.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "you", "form": "hero" } },
              "then": { "attack": {
                "target": { "query": "villain" },
                "effect": { "changeForm": { "player": "firstPlayer", "to": "alterEgo" } }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RepeatedMutationAnalysisReachesAFixedPoint()
    {
        // One frame removes the title, making a form change reachable; that
        // form change makes the unsupported third-frame branch reachable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alterEgo" } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
            """);
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void RepeatedMutationAnalysisIsBoundedByRemainingFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Aunt May" },
              "then": { "discard": "this" },
              "else": { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alterEgo" } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void RemovingThreatDoesNotChangeWhichTitlesAreInPlay()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void LethalDamageCanMoveTheFirstPlayerBindingBetweenFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": "you", "amount": 99 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(0, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Fact]
    public void VillainDamageCannotMoveTheFirstPlayerBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void NonlethalIdentityDamageCannotMoveTheFirstPlayerBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "dealDamage": { "cards": "you", "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void MovingLethalDamageCanMoveTheFirstPlayerBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "firstPlayer", "form": "hero" } }, "then": { "moveDamage": { "from": { "query": "villain" }, "to": "you", "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        World? world = null;
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                villain.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(1, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RemovingLastThreatCanDefeatATitleBetweenFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Bomb Scare" }, "then": { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? scheme = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(1, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void CumulativeThreatRemovalCanDefeatATitleBetweenFrames()
    {
        // All threat removed during one player's frame counts when deciding
        // whether a later player's title-dependent branch can become active.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Bomb Scare" },
              "then": { "seq": [
                { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } },
                { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void CumulativeDamageCanEliminateAPlayerBetweenFrames()
    {
        // Individually nonlethal damage instances combine before the next
        // player's frame and can move the first-player binding.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "dealDamage": { "cards": "you", "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void OrderedMutationCanExposeLethalDamageWithinARepeatedFrame()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": "this" },
                { "if": {
                  "test": { "not": { "titleInPlay": "Aunt May" } },
                  "then": { "dealDamage": { "cards": "you", "amount": 2 } },
                  "else": { "draw": { "player": "you", "count": 1 } }
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DamageToFixedTargetsAccumulatesAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": {
                "cards": { "query": "identities" }, "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Theory]
    [InlineData("\"yourHero\"")]
    [InlineData("{ \"query\": \"charactersYouControl\" }")]
    [InlineData("{ \"withTrait\": { \"cards\": \"yourHero\", \"trait\": \"AVENGER\" } }")]
    [InlineData("{ \"withTrait\": { \"cards\": { \"query\": \"charactersYouControl\" }, \"trait\": \"AVENGER\" } }")]
    [InlineData("{ \"maxBy\": { \"of\": { \"query\": \"charactersYouControl\" }, \"by\": \"attack\" } }")]
    public void PlayerRelativeDamageTargetsApplyOnlyInTheirPlayersFrame(
        string targets)
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            $$"""
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": { "cards": {{targets}}, "amount": 1 } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void MovedDamageCannotBeReusedAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "moveDamage": {
                "from": { "query": "villain" },
                "to": { "titled": "Spider-Man" },
                "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DamageBeforeAMoveReplenishesItsRepeatedSource()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Hydra Mercenary" }, "amount": 2 } },
                { "discard": "this" },
                { "if": {
                  "test": { "not": { "titleInPlay": "Aunt May" } },
                  "then": { "moveDamage": {
                    "from": { "titled": "Hydra Mercenary" },
                    "to": { "titled": "Spider-Man" },
                    "amount": 2
                  } },
                  "else": { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Spider-Man" },
                    "amount": 1
                  } }
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        Card? minion = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void DamageAfterAMoveIsAvailableOnlyToLaterRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } },
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void DamageMovedAwayDoesNotAccumulateAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "titled": "Spider-Man" },
                  "to": { "query": "villain" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void DamageHealedAfterEachMoveDoesNotAccumulateAcrossFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } },
                { "heal": { "card": { "titled": "Spider-Man" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(3);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void DistinctPlayersCanSupplyDamageAcrossRepeatedFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "moveDamage": {
                "from": "you",
                "to": { "titled": "Spider-Man" },
                "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
                board.Seats[1].IdentityCard.TakeDamage(1);
                board.Seats[2].IdentityCard.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk", "iron_man"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[2].IdentityCard.Damage);
    }

    [Fact]
    public void PlayerRelativeDamageRebindsBetweenFixedTargetFrames()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 } },
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "heal": { "card": { "titled": "Spider-Man" }, "amount": 1 } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(7);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void ToughPreventsLethalDamageInAnEarlierRepeatedFrame()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "dealDamage": {
                "cards": { "titled": "Spider-Man" }, "amount": 1
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void ForEachDamageIsFullyTracedBeforeARepeatedFrameCanMutate()
    {
        // The first frame's two points are one combined for-each instance and
        // eliminate Spider-Man at two remaining hit points. That changes the
        // first player before the next frame and exposes the unsupported
        // branch. Initiation must trace both points and refuse before the
        // exhaust cost or damage can mutate the board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "forEach": { "count": 2, "effect": {
                "dealDamage": { "cards": { "titled": "Spider-Man" }, "amount": 1 }
              } } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:for-each.3")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void MutableForEachAmountsFailClosedDuringRepeatedTrace()
    {
        // Each chosen instance would read damageOn again: three damage first,
        // then six, not the same three copied twice. The trace cannot yet
        // evaluate that expression against its intermediate board, so it must
        // refuse before an earlier frame can eliminate the first player and
        // expose the unsupported branch for the next one.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "forEach": { "count": 2, "effect": { "chooseCard": {
                "from": { "query": "villain" },
                "effect": { "dealDamage": {
                  "cards": { "titled": "Spider-Man" },
                  "amount": { "damageOn": { "titled": "Spider-Man" } }
                } }
              } } } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(3);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("between traced iterations", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(3, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void MultiTargetForEachIsNotOfferedBeforeItsCost()
    {
        // Without “choose,” for-each applies to one target. Two matching
        // minions make this authored selector unsupported. The exact-one
        // boundary is an initiation check so the action cannot first exhaust
        // its source and only then discover the problem.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "forEach": { "count": 2, "effect": {
              "dealDamage": { "cards": { "query": "minions" }, "amount": 1 }
            } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01121", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:for-each.1")]
    [Fact]
    public void AStableVillainTargetSurvivesAnExhaustCost()
    {
        // Exhausting this support cannot change which single card occupies the
        // villain area. The conservative mutation boundary therefore keeps
        // this supported target shape available.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "forEach": { "count": 2, "effect": {
              "dealDamage": { "cards": { "query": "villain" }, "amount": 1 }
            } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AForEachTargetCannotAppearAfterPaymentOrAnEarlierStep()
    {
        // The action starts with one minion, but the preceding step would put
        // a second into play. Because the no-choice target is not yet a
        // persisted binding, initiation refuses this changing-cardinality
        // shape before either exhausting Aunt May or moving Sandman.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDiscardPile" ], "title": "Sandman"
                } },
                "where": "engagedWithYou"
              } },
              { "forEach": { "count": 2, "effect": {
                "dealDamage": { "cards": { "query": "minions" }, "amount": 1 }
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        Card? sandman = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                sandman = board.CreateCard(
                    "01102", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("after state may change", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, sandman!.Area.Type);
        Assert.Equal(0, world!.Cards[source.ObjectId].Damage);
    }

    [Rule("rr:for-each")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void NegativeForEachCountIsRejectedBeforeItsCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "forEach": { "count": -1, "effect": { "draw": { "player": "you", "count": 1 } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var refused = Assert.Throws<AbilityException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("non-negative", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:alteration-effect")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void NegativeEachTimeCountIsRejectedBeforeItsCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachTime": {
              "effect": { "discardTop": { "from": "encounterDeck", "count": -1 } },
              "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
              "then": { "draw": { "player": "you", "count": 1 } }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var refused = Assert.Throws<AbilityException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("non-negative", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:alteration-effect")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void MutableEachTimeCountAfterAnEarlierEffectIsRejectedBeforeItsCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "heal": { "cards": "you", "amount": 1 } },
              { "eachTime": {
                "effect": { "discardTop": {
                  "from": "encounterDeck",
                  "count": { "add": [ -1, { "damageOn": "you" } ] }
                } },
                "when": { "cardSet": { "card": "that", "set": "kree_fanatic" } },
                "then": { "draw": { "player": "you", "count": 1 } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(1);
            },
            hero: true,
            abilities: runner));

        Assert.Contains("each-time count after state may change", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(1, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:for-each")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void DynamicNegativeForEachCountIsRejectedBeforeALabelledPowerCost()
    {
        // A changing count still has a definite value at initiation. It must
        // be validated before suspension analysis can schedule the attack and
        // pay its cost; mutability only determines whether zero can prune the
        // body.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "forEach": {
                "count": { "add": [ -1, { "damageOn": "you" } ] },
                "effect": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var refused = Assert.Throws<AbilityException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("non-negative", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:for-each.3")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void BindingDependentForEachCountCannotPruneItsBodyBeforePayment()
    {
        // Before chooseCard binds `chosen`, the count appears to be zero. The
        // chosen card makes it one, so the unsupported labelled continuation
        // must be found before the exhaust cost rather than hidden as a
        // zero-count body during initiation.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "chooseCard": {
              "from": { "query": "minions" },
              "effect": { "forEach": {
                "count": { "count": "chosen" },
                "effect": { "seq": [
                  { "choose": { "options": [
                    { "draw": { "player": "you", "count": 1 } },
                    { "seq": [] }
                  ] } },
                  { "attack": {
                    "target": { "query": "villain" },
                    "effect": { "enemyAttacks": {
                      "enemies": { "query": "villain" }
                    } }
                  } }
                ] }
              } }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("for-each count after state may change", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:for-each")]
    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void CurrentDynamicZeroHasNoChoiceForOtherwisePreflight()
    {
        // No payment, prior step, or binding can change damageOn before this
        // predecessor executes. Its current zero count makes the nested choice
        // unreachable, so otherwise may resolve the draw.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "otherwise": {
              "effect": { "forEach": {
                "count": { "damageOn": "you" },
                "effect": { "chooseCard": {
                  "from": { "query": "minions" },
                  "effect": { "discard": "chosen" }
                } }
              } },
              "otherwise": { "draw": { "player": "you", "count": 1 } }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void InactiveStablePowerBranchDoesNotValidateItsForEachCount()
    {
        // Form cannot change between offering and paying this action. Only the
        // hero branch can execute, so an alter-ego-only count must not reject
        // the labelled attack from an unreachable branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                "else": { "forEach": {
                  "count": { "add": [ -1, { "damageOn": "you" } ] },
                  "effect": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } }
              } }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Rule("rr:form-change-form.2")]
    [Fact]
    public void EarlierPowerStepCanExposeASuspendingBranch()
    {
        // The first step changes the fact tested by the second. Suspension
        // preflight must therefore inspect both reachable branches and refuse
        // the enemy activation before the labelled attack is scheduled.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": { "player": "you", "to": "alterEgo" } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Rule("rr:for-each")]
    [Fact]
    public void PowerTargetBindingCannotHideAForEachContinuation()
    {
        // The villain becomes `chosen` when the labelled attack is scheduled.
        // Its existing damage makes this count one, so the nested threat
        // continuation must be refused while the action is still only offered.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "forEach": {
                "count": { "damageOn": "chosen" },
                "effect": { "placeThreat": {
                  "scheme": { "query": "mainScheme" }, "amount": 1
                } }
              } }
            } }
            """);
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void PowerAmountBranchIsRefusedBeforeLegalPracticeDiscards()
    {
        // The selected card count binds powerAmount. Every branch that binding
        // can open must be checked before Legal Practice discards a hand card.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "legalPractice": {
              "schemes": { "query": "thwartableSchemes" },
              "power": { "thwart": {
                "target": "chosen",
                "effect": { "if": {
                  "test": { "atLeast": {
                    "value": { "powerAmount": "cardsDiscarded" },
                    "count": 1
                  } },
                  "then": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } },
                  "else": { "removeThreat": {
                    "scheme": "chosen", "amount": 1
                  } }
                } }
              } }
            } }
            """);
        World? world = null;
        Card? source = null;
        int handBefore = -1;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "01151", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
                handBefore = board.Seats[0].Hand.Cards.Count;
            },
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(handBefore, world!.Seats[0].Hand.Cards.Count);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void HarmlessEarlierPowerStepDoesNotSwitchAFormBranch()
    {
        // Drawing changes state but cannot change form. The hero-only branch
        // therefore remains the only executable branch of this labelled power.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void InactiveEarlierBranchDoesNotChangePowerForm()
    {
        // The first condition's alter-ego branch cannot execute while the hero
        // branch merely draws. Its unreachable form change must not make the
        // later hero-only condition appear switchable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "draw": { "player": "you", "count": 1 } },
                  "else": { "changeForm": {
                    "player": "you", "to": "hero"
                  } }
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ZeroForEachBodyDoesNotChangePowerForm()
    {
        // A zero-count form change never executes. It cannot make the later
        // form condition switch or expose its suspending alter-ego branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "forEach": {
                  "count": 0,
                  "effect": { "changeForm": {
                    "player": "you", "to": "alterEgo"
                  } }
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NoOpFormChangeDoesNotSwitchALaterPowerBranch()
    {
        // Changing to the form already showing does nothing. The later hero
        // condition therefore cannot switch to its suspending alter-ego branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": { "player": "you", "to": "hero" } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void AnotherPlayersFormChangeDoesNotSwitchYourPowerBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": {
                  "player": "firstPlayer", "to": "alterEgo"
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.FirstPlayer = 1;
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void StableFirstPlayerNoOpDoesNotSwitchALaterPowerBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "changeForm": {
                  "player": "firstPlayer", "to": "hero"
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.FirstPlayer = 1;
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LethalPowerDamageCanRebindAFormTestedFirstPlayer()
    {
        // Eliminating the hero holding the first-player token moves that
        // selector to the alter-ego player. The newly reachable activation is
        // refused before the lethal damage mutates the board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 99 } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DeterministicFormRestorationKeepsTheLaterPowerBranchStable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "changeForm": { "player": "you", "to": "alterEgo" } },
                { "changeForm": { "player": "you", "to": "hero" } },
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:form-change-form.2")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void ChosenPlayerRestorationKeepsEveryOtherFormOutcomeReachable()
    {
        // A choice must be made among “eligible game elements.” Restoring the
        // chosen identity to hero form changes only that identity; another
        // identity that a prior option changed can remain in alter-ego form.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "choose": { "options": [
                { "changeForm": {
                  "player": "firstPlayer", "to": "alterEgo"
                } },
                { "changeForm": { "player": "you", "to": "alterEgo" } }
              ] } },
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "changeForm": {
                  "player": "chosenPlayer", "to": "hero"
                } }
              } },
              { "if": {
                "test": { "inForm": {
                  "player": "firstPlayer", "form": "hero"
                } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1, "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.FirstPlayer = 1;
                board.Seats[1].IdentityCard.TurnTo("01010a");
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:form-change-form.2")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void EveryLegalEachPlayerOrderContributesFormReachability()
    {
        // “The first player decides the order” for an each-player effect. If
        // player one resolves first, both identities can change form; the
        // later target check must include that legal ordering.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": { "if": {
                "test": { "inForm": {
                  "player": "firstPlayer", "form": "hero"
                } },
                "then": { "changeForm": {
                  "player": "you", "to": "alterEgo"
                } },
                "else": { "seq": [] }
              } } } },
              { "if": {
                "test": { "inForm": {
                  "player": "you", "form": "hero"
                } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1, "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TurnTo("01001a");
                board.Seats[1].IdentityCard.TurnTo("01010a");
                source = board.CreateCard(
                    AuthoredCards.AuntMay,
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            runner.Actions(world, 1), ability => ability.Card == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void CumulativePowerDamageCanRebindTheFirstPlayer()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LethalDamageToAnotherPlayerDoesNotRebindFirstPlayer()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Carol Danvers" }, "amount": 99
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DamagePlacedEarlierCanBeMovedToEliminateFirstPlayer()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:for-each.2")]
    [Rule("rr:tough.2")]
    [Fact]
    public void CombinedForEachDamageUsesOneToughInPowerPreflight()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "forEach": {
                  "count": 2,
                  "effect": { "dealDamage": { "cards": "you", "amount": 1 } }
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                Statuses.Give(
                    board, board.Seats[0].IdentityCard, Statuses.Tough);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NonToughStatusDoesNotEnterPowerDamageSimulation()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "giveStatus": { "card": "you", "status": "stunned" } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void MutablePowerAmountAfterDamageFailsBeforeMutation()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "dealDamage": {
                  "cards": "you", "amount": { "damageOn": "you" }
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(4);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("mutable power amount", refused.Message);
        Assert.Equal(4, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:and")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void UnsupportedPowerAndIsRejectedWithoutReplayingDamage()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "and": [
                  { "dealDamage": { "cards": "you", "amount": 1 } },
                  { "draw": { "player": "you", "count": 1 } }
                ] }
              ] }
            } }
            """);
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:and")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void SingletonPowerAndIsSimulatedOnce()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "and": [
                { "dealDamage": { "cards": "you", "amount": 1 } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ToughOnMovedDamageSourcePreventsPhantomInventory()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                Statuses.Give(
                    board, board.TheCardIn(DeckType.VillainArea)!, Statuses.Tough);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NoOpMoveDoesNotMakeALaterAmountLookMutable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" },
                  "amount": { "damageOn": "you" }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(2);
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void BranchMergeKeepsLiveDamageWhenAnotherBranchHeals()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "if": {
                  "test": { "titleInPlay": "Nonexistent" },
                  "then": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void BranchMergeDoesNotInventToughOnTheUntakenPath()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": "this" },
                { "if": {
                  "test": { "titleInPlay": "Aunt May" },
                  "then": { "giveStatus": {
                    "card": "you", "status": "tough"
                  } },
                  "else": { "draw": { "player": "you", "count": 1 } }
                } },
                { "dealDamage": { "cards": "you", "amount": 1 } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Rule("rr:heal")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void GuaranteedBranchDoesNotKeepItsPreBranchDamageState()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "if": {
                  "test": { "inForm": {
                    "player": "you", "form": "hero"
                  } },
                  "then": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:and")]
    [Rule("rr:heal")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void SingletonAndDoesNotKeepItsPreChildDamageState()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "and": [
                  { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                ] },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:otherwise")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void UncertainOtherwisePreservesThePathWithoutItsFallback()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "otherwise": {
                  "effect": { "draw": { "player": "you", "count": 1 } },
                  "otherwise": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:exhausted")]
    [Rule("rr:otherwise")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ExhaustedPredecessorMakesItsOtherwiseFallbackGuaranteed()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "exhaust": "this" },
                { "otherwise": {
                  "effect": { "exhaust": "this" },
                  "otherwise": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:damage.step.1")]
    [Rule("rr:replacement-effect.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ForcedReplacementCanLeaveALabelledPowerMoveSourceEmpty()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    AuthoredCards.ArmoredSuit,
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RemovedExhaustTargetCannotSatisfyThen()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "then": {
                  "effect": { "exhaust": { "titled": "Hydra Mercenary" } },
                  "then": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:damage.step.1")]
    [Rule("rr:tough.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void AlternativeDamageProtectionPathsRemainCorrelated()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "if": {
                  "test": { "titleInPlay": "Nonexistent" },
                  "then": { "seq": [
                    { "dealDamage": {
                      "cards": { "query": "villain" }, "amount": 5
                    } },
                    { "giveStatus": {
                      "card": { "query": "villain" }, "status": "tough"
                    } }
                  ] },
                  "else": { "seq": [] }
                } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    AuthoredCards.ArmoredSuit,
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:cannot.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ProhibitedLabelledMoveLeavesDamageForALaterMove()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DependentOutcomeStaysCorrelatedWithItsConditionalState()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "then": {
                  "effect": { "if": {
                    "test": { "titleInPlay": "Nonexistent" },
                    "then": { "giveStatus": {
                      "card": { "query": "villain" }, "status": "tough"
                    } },
                    "else": { "seq": [] }
                  } },
                  "then": { "dealDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:cannot.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DiscardedProhibitionMakesLaterLabelledDamageLegal()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Legions of Hydra" } },
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
                  "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:heal")]
    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NoOpHealCannotSatisfyThenAfterAnEarlierStep()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "then": {
                  "effect": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } },
                  "then": { "dealDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RepeatedDiscardOfAnAlreadyDiscardedCardResolvesNone()
    {
        // The second discard cannot affect Helicarrier after the first one has
        // moved it out of play. It therefore does not satisfy "then," and the
        // dependent villain damage never becomes available to move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Helicarrier" } },
                { "then": {
                  "effect": { "discard": { "titled": "Helicarrier" } },
                  "then": { "dealDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        Card? helicarrier = null;

        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                helicarrier = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(DeckType.SupportsArea, helicarrier!.Area.Type);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:form-change-form")]
    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void NoOpFormChangeCannotSatisfyThenAfterAnEarlierStep()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "draw": { "player": "you", "count": 1 } },
                { "then": {
                  "effect": { "changeForm": { "player": "you", "to": "hero" } },
                  "then": { "dealDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:villain-defeat.2")]
    [Rule("rr:then")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DependentHealUsesTheNewVillainStage()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "then": {
                  "effect": { "heal": {
                    "card": { "query": "villain" }, "amount": 1
                  } },
                  "then": { "heal": {
                    "card": { "titled": "Weapons Runner" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "titled": "Weapons Runner" },
                  "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;
        Card? minion = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01121",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, minion!.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:villain-defeat.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledRankedSelectorDropsTheNewVillainStage()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "minBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:villain-defeat.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledRankedSelectorCanGainTheNewVillainStage()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:lasting-effects")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledNumericGrantChangesALaterRankedTargetSet()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "grantUntil": {
                  "card": { "query": "villain" },
                  "keyword": "attack", "amount": 2, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:villain-defeat.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RankedSelectorAfterFinalVillainDefeatDoesNotCrash()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "minBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LabelledGuardEntryImmediatelyProtectsTheVillain()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "putIntoPlay": {
                  "card": { "cardsIn": {
                    "areas": [ "encounterDiscardPile" ],
                    "title": "Hydra Mercenary"
                  } },
                  "where": "engagedWithYou"
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void OutOfPlayMinionDoesNotJoinALabelledRankedSelector()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01102", board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteringConstantAbilityRaisesBeforeALabelledPowerMutates()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "putIntoPlay": {
                  "card": { "cardsIn": {
                    "areas": [ "encounterDiscardPile" ], "title": "Titania"
                  } },
                  "where": "engagedWithYou"
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Titania" }, "to": "you", "amount": 1
                } }
              ] }
            } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? titania = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                titania = board.CreateCard(
                    "01162", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("constant abilities", refused.Message);
        Assert.Equal(DeckType.EncounterDiscardPile, titania!.Area.Type);
        Assert.True(source!.Ready);
        Assert.NotNull(world);
    }

    [Rule("rr:defeat.1")]
    [Rule("rr:guard.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DefeatedGuardStopsProtectingTheVillainInALabelledPower()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        World? world = null;
        Card? source = null;
        Card? guard = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, guard.Area.Type);
        Assert.True(source!.Ready);
    }

    [Rule("rr:permanent.1")]
    [Fact]
    public void RankedDiscardCanTargetAPermanentFromTheSourcesSet()
    {
        // Permanent prevents a card from leaving play "except by card
        // abilities in the same set." Both S.H.I.E.L.D. Tech cards carry the
        // same printed set, so the target remains in the ranked candidates
        // while the action is traced.
        var runner = Runner(
            "27182a",
            "Action",
            """
            { "chooseCard": {
              "from": { "minBy": {
                "of": { "titled": "Wrist Navigator" }, "by": "cost"
              } },
              "effect": { "discard": "chosen" }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, "27182a");
                InPlay(board, "27189a");
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:permanent.4")]
    [Rule("rr:target.3.4")]
    [Theory]
    [InlineData("discard")]
    [InlineData("removeFromGame")]
    public void InvalidPermanentRemovalComponentDoesNotAbortAValidSibling(
        string removal)
    {
        // One valid target makes the combined effect legal, but an invalid
        // component does not resolve against that target. Exhaust succeeds;
        // the cross-set Permanent neither leaves play nor turns the component
        // into an exception after mutation.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            $$"""
            { "chooseCard": {
              "from": { "titled": "Compact Darts" },
              "effect": { "seq": [
                { "exhaust": "chosen" },
                { "{{removal}}": "chosen" }
              ] }
            } }
            """);
        Card? source = null;
        Card? permanent = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                permanent = board.CreateCard(
                    "27182a", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner);

        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        var suspended = game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Element, suspended.Prompt!.Asking);

        game.Resolve(Decision.Take(permanent!.ObjectId));

        Assert.False(permanent.Ready);
        Assert.Equal(DeckType.UpgradesArea, permanent.Area.Type);
    }

    [Rule("rr:removed-from-the-game.2")]
    [Rule("rr:target.4.1")]
    [Fact]
    public void LaterComponentSkipsACardAlreadyRemovedFromTheGame()
    {
        // The first component removes the bound source. It is no longer a
        // valid target when the second component resolves, so that component
        // does nothing rather than trying to move the terminal card again.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeFromGame": "this" },
              { "discard": "this" }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        var resolved = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.RemovedArea, source!.Area.Type);
        Assert.Single(resolved.Events.OfType<CardsMoved>(), moved =>
            moved.Cards.Any(card => card.Card == source.ObjectId));
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Fact]
    public void RetainedBindingCannotFollowACardIntoTheHand()
    {
        // Returning the source to hand makes it out of play. The following
        // component retains `this`, but it does not expressly refer to the
        // hand and therefore cannot discard the card from there.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "returnToHand": "this" },
              { "discard": "this" }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Fact]
    public void ChosenBindingRemembersTheAreaWhereItWasSelected()
    {
        // `chosen` is an express reference to the selected in-play card, not
        // permission to follow it into a later out-of-play area. Its selection
        // origin therefore survives the first component's hand move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "chooseCard": {
              "from": { "titled": "Helicarrier" },
              "effect": { "seq": [
                { "returnToHand": "chosen" },
                { "discard": "chosen" }
              ] }
            } }
            """);
        Card? source = null;
        Card? target = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = InPlay(board, "01092");
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));

        game.Resolve(Decision.Take(target!.ObjectId));

        Assert.Equal(DeckType.HandsArea, target.Area.Type);
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:removed-from-the-game")]
    [Fact]
    public void AreaNamedSelectorCanRemoveAnOutOfPlayCard()
    {
        // cardsIn expressly names the encounter discard pile, so the ability
        // may affect its matching out-of-play card. This is distinct from a
        // stale binding that merely follows a target after an earlier move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "removeFromGame": { "cardsIn": {
              "area": "encounterDiscardPile", "title": "Hydra Mercenary"
            } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? target = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
        Assert.Contains(target, world.AreaOf(DeckType.RemovedArea).Cards);
        Assert.False(source!.Ready);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:search.1")]
    [Fact]
    public void AmbiguousOutOfPlayRemovalRaisesBeforeActionCost()
    {
        // A singular remove node cannot choose between two matching search
        // results. The ambiguity is found while the action is offered, before
        // its exhaust cost can change the source.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "removeFromGame": { "cardsIn": {
              "area": "encounterDiscardPile", "title": "Hydra Mercenary"
            } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.CreateCard("08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:search.1")]
    [Fact]
    public void EarlierMoveCannotCreateRemovalAmbiguityAfterActionCost()
    {
        // The first component would add a second Hydra Mercenary to the named
        // discard pile. The singular second component cannot choose between
        // them, so the engine refuses the action before its exhaust cost or
        // the first discard changes the board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "discard": { "titled": "Hydra Mercenary" } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? inPlay = null;
        Card? discarded = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                inPlay = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                discarded = board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, inPlay!.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, discarded!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:search.1")]
    [Fact]
    public void EarlierMoveCannotCreateDiscardAmbiguityAfterActionCost()
    {
        // Every singular cardsIn consumer has the same atomicity boundary.
        // The second discard would need a player choice after the first adds a
        // matching card, so the action is refused before either component or
        // its exhaust cost runs.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "discard": { "titled": "Hydra Mercenary" } },
              { "discard": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? inPlay = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                inPlay = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, inPlay!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:search.1")]
    [Fact]
    public void AmbiguousSearchRaisesBeforeActionCost()
    {
        // Searching two named areas finds two copies and therefore requires
        // the player's rr:search.1 choice. Until that prompt exists, the
        // unsupported branch raises while the source can still remain ready.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "search": {
              "in": [ { "encounterDeck": 1 }, { "encounterDiscardPile": 1 } ],
              "for": "08028"
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("08028", board.AreaOf(DeckType.EncounterDeck));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:search.1")]
    [Fact]
    public void ChoiceFiltersAnOptionThatWouldMakeTheSuffixAmbiguous()
    {
        // The discard option is locally legal but would add a second matching
        // card before the singular suffix. It is filtered while the harmless
        // draw remains available, before either option changes the board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "choose": { "options": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "draw": { "player": "you", "count": 1 } }
              ] } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? inPlay = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                inPlay = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Id == 0);
        Assert.Contains(game.Pending.Affordances, option => option.Id == 1);
        Assert.Equal(DeckType.EngagedEnemiesArea, inPlay!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:choose-option.2")]
    [Fact]
    public void ChoiceWithNoContinuationSafeOptionIsNotOfferedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "choose": { "options": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "discard": { "titled": "Hydra Mercenary" } }
              ] } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:choose-option.2")]
    [Fact]
    public void NestedChoiceWithNoContinuationSafeOptionIsNotOfferedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "choose": { "options": [
                  { "discard": { "titled": "Hydra Mercenary" } },
                  { "discard": { "titled": "Hydra Mercenary" } }
                ] } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void PluralCardsInChoiceDoesNotRequireSingularAreaStability()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "choose": { "options": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "discard": { "titled": "Hydra Mercenary" } }
              ] } },
              { "chooseCard": {
                "from": { "cardsIn": {
                  "area": "encounterDiscardPile", "title": "Hydra Mercenary"
                } },
                "effect": { "removeFromGame": "chosen" }
              } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);

        Assert.Contains(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:each-player")]
    [Fact]
    public void EachPlayerAreaMutationChecksEverySeatBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": {
                "discard": { "query": "minionsEngagedWithYou" }
              } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? secondPlayersMinion = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                secondPlayersMinion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(
            DeckType.EngagedEnemiesArea, secondPlayersMinion!.Area.Type);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:search.1")]
    [Fact]
    public void ToughMinionDoesNotMakeEncounterDiscardUnstable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        Card? discarded = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                discarded = board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(DeckType.RemovedArea, discarded!.Area.Type);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:for-each.1")]
    [Fact]
    public void CombinedRepeatedDamageIsOneToughPreventedInstance()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "forEach": { "count": 2, "effect": {
                "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                }
              } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        Card? discarded = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                discarded = board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(DeckType.RemovedArea, discarded!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:move.1")]
    [Fact]
    public void DamageCreatedBeforeAMoveIsProjectedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hawkeye" }, "amount": 1
              } },
              { "moveDamage": {
                "from": { "titled": "Hawkeye" },
                "to": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? ally = null;
        Card? minion = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                ally = board.CreateCard(
                    "01066", board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(0, ally!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:move.1")]
    [Fact]
    public void RepeatedMoveCannotReuseHealedDamage()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "moveDamage": {
                "from": "you", "to": { "titled": "Hydra Mercenary" },
                "amount": 1
              } },
              { "moveDamage": {
                "from": "you", "to": { "titled": "Hydra Mercenary" },
                "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        Card? discarded = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(1);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 2);
                discarded = board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        var action = Assert.Single(
            runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(
            Damage.Health(world, world.Facts, minion!) - 1, minion!.Damage);
        Assert.Equal(DeckType.RemovedArea, discarded!.Area.Type);
    }

    [Rule("rr:and")]
    [Rule("rr:first-player.3")]
    [Rule("rr:cost.6")]
    [Fact]
    public void ReorderableDamageProjectsEveryPermittedOrderBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "and": [
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } }
              ] },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 3);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);

        Assert.Throws<RulesNotImplementedException>(() =>
            runner.Actions(world, 0));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:and")]
    [Rule("rr:first-player.3")]
    [Rule("rr:cost.6")]
    [Fact]
    public void ReorderableDamageInventoryFeedsALaterMoveBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "and": [
                { "dealDamage": { "cards": "you", "amount": 3 } },
                { "dealDamage": { "cards": "you", "amount": 1 } }
              ] },
              { "moveDamage": {
                "from": "you", "to": { "titled": "Hydra Mercenary" },
                "amount": 3
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(
            world, world.Seats[0].IdentityCard, Statuses.Tough);

        Assert.Throws<RulesNotImplementedException>(() =>
            runner.Actions(world, 0));
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
        Assert.Equal(0, minion.Damage);
    }

    [Rule("rr:and")]
    [Rule("rr:first-player.3")]
    [Fact]
    public void ReorderableDamageInventoryKeepsCardsCorrelated()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "and": [
                { "moveDamage": {
                  "from": { "titled": "Hawkeye" },
                  "to": { "titled": "Black Cat" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Black Cat" },
                  "to": { "titled": "Hawkeye" }, "amount": 1
                } }
              ] },
              { "moveDamage": {
                "from": { "titled": "Hawkeye" },
                "to": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "moveDamage": {
                "from": { "titled": "Black Cat" },
                "to": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var hawkeye = board.CreateCard(
                    "01066", board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                hawkeye.TakeDamage(1);
                board.CreateCard(
                    "01002", board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                var minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 2);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Fact]
    public void GuaranteedToughProtectsALaterAreaSensitiveDamageStep()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "Hydra Mercenary" }, "status": "tough"
              } },
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:and")]
    [Rule("rr:cost.6")]
    [Fact]
    public void RepeatedAndGroupKeepsItsWholeIterationCountBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "forEach": { "count": 2, "effect": { "and": [
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } },
                { "draw": { "player": "you", "count": 1 } }
              ] } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 2);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:tough.2")]
    [Fact]
    public void ConditionalUsesTheProjectedToughState()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "if": {
                "test": { "hasStatus": {
                  "card": { "titled": "Hydra Mercenary" }, "status": "tough"
                } },
                "then": { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } },
                "else": { "draw": { "player": "you", "count": 1 } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:vulnerable.1")]
    [Rule("rr:cost.6")]
    [Fact]
    public void VulnerableStatusDiscardIsProjectedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "Scientist Supreme" },
                "status": "stunned"
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Scientist Supreme"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? vulnerable = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                vulnerable = board.CreateCard(
                    "50125", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "50125", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw"));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, vulnerable!.Area.Type);
    }

    [Rule("rr:otherwise.1")]
    [Fact]
    public void DependentContinuationUsesTheProjectedOutcome()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "otherwise": {
                "effect": { "heal": { "card": "you", "amount": 1 } },
                "otherwise": { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(1);
                var minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Fact]
    public void ProjectedBooleanTestsKeepDecisiveShortCircuits()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "if": {
                "test": { "and": [
                  { "hasStatus": {
                    "card": { "titled": "Hydra Mercenary" }, "status": "tough"
                  } },
                  { "inForm": { "player": "you", "form": "hero" } }
                ] },
                "then": { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } },
                "else": { "draw": { "player": "you", "count": 1 } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        Statuses.Give(world, minion!, Statuses.Tough);

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Rule("rr:cost.6")]
    [Fact]
    public void LabelledAttackBodyIsProjectedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "attack": {
                "target": { "titled": "Hydra Mercenary" },
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:then.1")]
    [Rule("rr:cost.6")]
    [Fact]
    public void WrappedDependentOutcomeUsesProjectedStateBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": { "cards": "you", "amount": 1 } },
              { "then": {
                "effect": { "seq": [
                  { "heal": { "card": "you", "amount": 1 } }
                ] },
                "then": { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 1
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:vulnerable.1")]
    [Rule("rr:attach-to.1")]
    [Rule("rr:cost.6")]
    [Fact]
    public void VulnerableDiscardProjectsHostedCardsBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "Scientist Supreme" },
                "status": "stunned"
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? vulnerable = null;
        Card? attachment = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                vulnerable = board.CreateCard(
                    "50125", board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                attachment = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        vulnerable.ObjectId));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw"));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.AlliesArea, vulnerable!.Area.Type);
        Assert.Equal(vulnerable.ObjectId, attachment!.Area.Host);
    }

    [Rule("rr:defeat.1")]
    [Rule("rr:attach-to.1")]
    [Rule("rr:cost.6")]
    [Fact]
    public void LethalDamageProjectsHostedCardsBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hawkeye" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? ally = null;
        Card? attachment = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                ally = board.CreateCard(
                    "01066", board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                ally.TakeDamage(
                    Damage.Health(board, board.Facts, ally) - 1);
                attachment = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.AlliesArea, ally!.Area.Type);
        Assert.Equal(ally.ObjectId, attachment!.Area.Host);
    }

    [Rule("rr:otherwise.1")]
    [Fact]
    public void ProjectedIfWithoutAnActiveBranchResolvesNone()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "otherwise": {
                "effect": { "if": {
                  "test": { "hasStatus": {
                    "card": { "titled": "Hydra Mercenary" }, "status": "tough"
                  } },
                  "then": { "heal": { "card": "you", "amount": 1 } }
                } },
                "otherwise": { "draw": { "player": "you", "count": 1 } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:victory-x.1")]
    [Fact]
    public void VictoryMinionDoesNotProjectToEncounterDiscard()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Badoon Headhunter" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Badoon Headhunter"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var minion = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "16183", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:victory-x.1.2")]
    [Fact]
    public void NestedVictoryAttachmentProjectsItsOrdinaryDiscard()
    {
        // Only a Victory attachment directly on the defeated character uses
        // the Forced Interrupt that moves it to the victory display. Its own
        // hosted card leaves through ordinary attachment cleanup, even when
        // that nested card also has Victory X.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Hydra Mercenary" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        Card? direct = null;
        Card? nested = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                direct = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId));
                nested = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), direct.ObjectId));
                foreach (var attachment in new[] { direct, nested })
                {
                    board.Effects.Register(new ContinuousEffect(
                        EffectSource.ConstantAbility,
                        "victory",
                        Amount: 1,
                        Card: attachment.ObjectId,
                        Affects: attachment.ObjectId,
                        Lasts: Duration.WhileInPlay));
                }
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
        Assert.Equal(minion.ObjectId, direct!.Area.Host);
        Assert.Equal(direct.ObjectId, nested!.Area.Host);
    }

    [Rule("rr:victory-x")]
    [Fact]
    public void VictorySideSchemeDoesNotProjectToEncounterDiscard()
    {
        // A side scheme with Victory X enters the victory display when it is
        // defeated instead of entering the encounter discard pile.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Kree Supremacy" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Kree Supremacy"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "16182a", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
                board.CreateCard(
                    "16182a", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:victory-x.1")]
    [Fact]
    public void LaterSelectorCannotSeeProjectedDepartedVictoryMinion()
    {
        // After the Victory minion leaves play, a later title reference no
        // longer denotes it. Its no-op discard therefore cannot destabilize an
        // unrelated singular encounter-discard query.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Badoon Headhunter" }, "amount": 1
              } },
              { "discard": { "titled": "Badoon Headhunter" } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var minion = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:otherwise.1")]
    [Rule("rr:victory-x.1")]
    [Fact]
    public void DepartedTargetMakesProjectedOtherwiseFallbackReachable()
    {
        // The defeated Victory minion is no longer an in-play title reference,
        // so healing it resolves nothing and the otherwise discard applies.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Badoon Headhunter" }, "amount": 1
              } },
              { "otherwise": {
                "effect": { "heal": {
                  "card": { "titled": "Badoon Headhunter" }, "amount": 1
                } },
                "otherwise": { "discard": {
                  "titled": "Hydra Mercenary"
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? victory = null;
        Card? hydra = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                victory = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                victory.TakeDamage(
                    Damage.Health(board, board.Facts, victory) - 1);
                hydra = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, victory!.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, hydra!.Area.Type);
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Fact]
    public void ExplicitMovementUpdatesLaterProjectedTitleReferences()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeFromGame": { "titled": "Hydra Mercenary" } },
              { "discard": { "titled": "Hydra Mercenary" } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:villain-defeat.4.2")]
    [Fact]
    public void ConsecutiveVillainStagesProjectCarriedAttachmentDeparture()
    {
        // Rhino I carries the attachment to Rhino II. Defeating the final
        // stage then discards it ordinarily, even when it has Victory X.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? villain = null;
        Card? attachment = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                attachment = board.CreateCard(
                    "01098", board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.ConstantAbility,
                    "victory",
                    Amount: 1,
                    Card: attachment.ObjectId,
                    Affects: attachment.ObjectId,
                    Lasts: Duration.WhileInPlay));
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.True(DeckTypes.IsInPlay(attachment!.Area.Type));
        Assert.Equal(villain.ObjectId, attachment.Area.Host);
    }

    [Rule("rr:enters-play")]
    [Fact]
    public void EnteredMinionParticipatesInLaterProjectedQueries()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDiscardPile" ], "title": "Sandman"
                } },
                "where": "engagedWithYou"
              } },
              { "dealDamage": {
                "cards": { "query": "minions" }, "amount": 100
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Sandman"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? sandman = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var protectedMinion = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, protectedMinion, Statuses.Tough);
                sandman = board.CreateCard(
                    "01102", board.AreaOf(DeckType.EncounterDeck));
                board.CreateCard(
                    "01102", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDeck, sandman!.Area.Type);
    }

    [Rule("rr:attachment.1")]
    [Rule("rr:victory-x.1")]
    [Fact]
    public void ProjectedAttachmentJoinsItsHostsDefeatTree()
    {
        var runner = Runner(
            "01098",
            "Action",
            """
            { "seq": [
              { "attachTo": { "titled": "Badoon Headhunter" } },
              { "dealDamage": {
                "cards": { "titled": "Badoon Headhunter" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Armored Rhino Suit"
              } } }
            ] }
            """);
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01098", board.AreaOf(DeckType.UpgradesArea));
                minion = board.CreateCard(
                    "16183", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "01098", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Throws<RulesNotImplementedException>(() => runner.Act(
            world,
            new PendingAbility(source!.ObjectId, AbilityType.Action, 0),
            [],
            []));

        Assert.Equal(DeckType.UpgradesArea, source!.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:thwart")]
    [Rule("rr:victory-x")]
    [Fact]
    public void ThwartSchemesSkipsProjectedDepartedSchemes()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Kree Supremacy" }, "amount": 1
              } },
              { "thwartSchemes": {
                "schemes": { "query": "thwartableSchemes" },
                "power": { "thwart": {
                  "target": "chosen",
                  "effect": { "discard": {
                    "titled": "Hydra Mercenary"
                  } }
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """);
        Card? source = null;
        var (_, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "16182a", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
                board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: AuthoredCards.Runner());

        Assert.Contains(runner.Actions(world, 0), action =>
            action.Card == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.6")]
    [Fact]
    public void CancelledLabelSkipsAreaSensitivePostArrowEffects()
    {
        var runner = Runner(
            "01017",
            "Action",
            """
            { "seq": [
              { "attack": {
                "target": { "titled": "Hydra Mercenary" },
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""",
            labels: "[ \"attack\" ]");
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(
            board =>
            {
                source = board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
                Statuses.Give(
                    board, board.Seats[0].IdentityCard, Statuses.Stunned);
            },
            hero: true,
            abilities: AuthoredCards.Runner());
        var action = Assert.Single(runner.Actions(world, 0), option =>
            option.Card == source!.ObjectId);

        runner.Act(world, action, [], []);

        Assert.False(source!.Ready);
        Assert.False(Statuses.Has(
            world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:thwart")]
    [Rule("rr:cost.6")]
    [Fact]
    public void ThwartSchemesPowerIsProjectedBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "thwartSchemes": {
                "schemes": { "query": "thwartableSchemes" },
                "power": { "thwart": {
                  "target": "chosen",
                  "effect": { "removeThreat": {
                    "scheme": { "query": "powerTargets" }, "amount": 1
                  } }
                } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Breakin' & Takin'"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""",
            labels: "[ \"thwart\" ]");
        Card? source = null;
        Card? scheme = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01107", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
                board.CreateCard(
                    "01107", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(1, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:search.1")]
    [Fact]
    public void InactiveBranchDoesNotMakeALaterAreaQueryUnstable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "if": {
                "test": { "inForm": { "player": "you", "form": "alterEgo" } },
                "then": { "discard": { "titled": "Hydra Mercenary" } }
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? target = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:search.1")]
    [Fact]
    public void ZeroIterationDoesNotMakeALaterAreaQueryUnstable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "forEach": { "count": 0, "effect": {
                "discard": { "titled": "Hydra Mercenary" }
              } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? target = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
    }

    [Rule("rr:damage.3")]
    [Rule("rr:search.1")]
    [Fact]
    public void NonlethalVillainDamageDoesNotMakeEncounterDiscardUnstable()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? target = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(1, villain.Damage);
        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Rule("rr:search.1")]
    [Fact]
    public void DefeatedSchemeCannotCreateAreaAmbiguityAfterActionCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Breakin' & Takin'" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Breakin' & Takin'"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01107", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
                board.CreateCard(
                    "01107", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SideSchemesArea, scheme!.Area.Type);
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Rule("rr:for-each.1")]
    [Fact]
    public void RepeatedThreatRemovalProjectsItsCombinedAmountBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "forEach": { "count": 2, "effect": {
                "removeThreat": {
                  "scheme": { "titled": "Breakin' & Takin'" }, "amount": 1
                }
              } } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Breakin' & Takin'"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01107", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
                board.CreateCard(
                    "01107", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Fact]
    public void SequentialThreatRemovalProjectsItsCombinedAmountBeforeCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Breakin' & Takin'" }, "amount": 1
              } },
              { "removeThreat": {
                "scheme": { "titled": "Breakin' & Takin'" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Breakin' & Takin'"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? scheme = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard(
                    "01107", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
                board.CreateCard(
                    "01107", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Rule("rr:move.1")]
    [Fact]
    public void LethalMovedDamageRaisesBeforeHealingOrActionCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "moveDamage": {
                "from": "you", "to": { "titled": "Hydra Mercenary" },
                "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Hydra Mercenary"
              } } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        Card? identity = null;

        Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                identity = board.Seats[0].IdentityCard;
                identity.TakeDamage(1);
                minion = board.CreateCard(
                    "01101", board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                minion.TakeDamage(
                    Damage.Health(board, board.Facts, minion) - 1);
                board.CreateCard(
                    "08028", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner));

        Assert.True(source!.Ready);
        Assert.Equal(1, identity!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:defeat.1")]
    [Rule("rr:event.5")]
    [Fact]
    public void EventThreatModifierIsIncludedInAreaMutationPreflight()
    {
        var runner = Runner(
            "01005",
            "Action",
            """
            { "seq": [
              { "removeThreat": {
                "scheme": { "titled": "Breakin' & Takin'" }, "amount": 1
              } },
              { "removeFromGame": { "cardsIn": {
                "area": "encounterDiscardPile", "title": "Breakin' & Takin'"
              } } }
            ] }
            """);
        Card? played = null;
        Card? scheme = null;
        var (_, world) = Playing(
            board =>
            {
                played = board.CreateCard("01005", board.Seats[0].Hand);
                scheme = board.CreateCard(
                    "01107", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
                board.CreateCard(
                    "01107", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect, "eventThreatRemoval", Amount: 1,
            Card: played!.ObjectId, Affects: played.ObjectId));

        Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));

        Assert.Equal(DeckType.HandsArea, played.Area.Type);
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:permanent.4")]
    [Rule("rr:removed-from-the-game")]
    [Fact]
    public void PermanentDoesNotProtectAnExplicitOutOfPlayTarget()
    {
        // Permanent prevents an effect from making a card leave play. This
        // target is already in an expressly named discard pile, so a card from
        // another set may remove it from the game.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "removeFromGame": { "cardsIn": {
              "area": "encounterDiscardPile", "title": "Compact Darts"
            } } }
            """);
        Card? source = null;
        Card? target = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                target = board.CreateCard(
                    "27182a", board.AreaOf(DeckType.EncounterDiscardPile));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.RemovedArea, target!.Area.Type);
    }

    [Rule("rr:permanent.4")]
    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Theory]
    [InlineData("then", 0)]
    [InlineData("otherwise", 1)]
    public void SkippedPermanentDiscardHasTheCorrectDependentOutcome(
        string dependency, int cardsDrawn)
    {
        // The cross-set Permanent is not a legal discard target, so that
        // component resolves none. "Then" does not run; "otherwise" does.
        // A valid exhaust sibling keeps the overall target legal.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            $$"""
            { "chooseCard": {
              "from": { "titled": "Compact Darts" },
              "effect": { "seq": [
                { "exhaust": "chosen" },
                { "{{dependency}}": {
                  "effect": { "discard": "chosen" },
                  "{{dependency}}": { "draw": { "player": "you", "count": 1 } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        Card? permanent = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                permanent = board.CreateCard(
                    "27182a", board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner);
        int hand = world.Seats[0].Hand.Cards.Count;

        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(permanent!.ObjectId));

        Assert.False(permanent.Ready);
        Assert.Equal(DeckType.UpgradesArea, permanent.Area.Type);
        Assert.Equal(hand + cardsDrawn, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:permanent.5")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void PermanentOnLethalTargetRaisesBeforeALabelledActionCostMutates()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? guard = null;
        Card? permanent = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                permanent = board.CreateCard(
                    "27189a",
                    board.AreaOf(
                        DeckType.UpgradesArea, guard.Area.PlayArea,
                        guard.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("rr:permanent.5 is not implemented", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, guard.Area.Type);
        Assert.Equal(guard.ObjectId, permanent!.Area.Host);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:target.3.8")]
    [Fact]
    public void GuardMakesTheVillainInvalidBeforeTracingALabelledAttack()
    {
        // Guard says “The engaged player cannot attack any villain,” and a
        // target that cannot be attacked is not valid for an attack-labeled
        // ability. Later effects cannot first remove that initiation limit.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Hydra Mercenary" },
                  "keyword": "health", "amount": 2, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:defeat.1")]
    [Rule("rr:guard.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DiscardedConstantHealthGrantCannotKeepAGuardAliveInTheTrace()
    {
        // Constant abilities are active only while their source remains in
        // play. Discarding Genetically Enhanced removes its +3 hit points, so
        // the following 3 damage defeats Hydra Mercenary and removes Guard.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Genetically Enhanced" } },
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? guard = null;
        Card? enhanced = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                enhanced = board.CreateCard(
                    "01163",
                    board.AreaOf(
                        DeckType.UpgradesArea, guard.Area.PlayArea,
                        guard.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(guard.ObjectId, enhanced!.Area.Host);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DiscardedConstantHealthGrantCannotKeepAVillainStageAliveInTheTrace()
    {
        // Constant abilities are active only while their source remains in
        // play. Discarding The "Immortal" Klaw removes +10 hit points, so the
        // next damage defeats this stage and the new stage begins undamaged.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "The \"Immortal\" Klaw" } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        Card? immortal = null;

        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                immortal = board.CreateCard(
                    "01127", board.AreaOf(DeckType.SideSchemesArea));
                // Rhino I has 28 hit points in this two-player game. Leave him
                // one below that printed maximum; Immortal Klaw raises the
                // live maximum to 38 until the first effect discards it.
                villain.TakeDamage(27);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(27, villain!.Damage);
        Assert.Equal(DeckType.SideSchemesArea, immortal!.Area.Type);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:defeat.1")]
    [Rule("rr:side-scheme.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DefeatedSideSchemeStopsItsProhibitionInADirectLabelledPower()
    {
        // A side scheme with no threat is defeated and discarded. Removing
        // Legions of Hydra's final threat therefore ends the prohibition that
        // kept Madame Hydra from taking the following damage.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Legions of Hydra" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
                  "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? legions = null;
        Card? madame = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                legions = board.CreateCard(
                    "01180", board.AreaOf(DeckType.SideSchemesArea));
                legions.PlaceTokens("k_threat", 1);
                madame = board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(1, legions!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, madame!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:crisis-icon.1")]
    [Rule("rr:defeat.1")]
    [Rule("rr:side-scheme.2")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DefeatedCrisisSchemeUnlocksMainSchemeRemovalInTheTrace()
    {
        // While a crisis icon is in play, player cards cannot remove threat
        // from the main scheme. Crowd Control is discarded when its last
        // threat is removed, so the following main-scheme removal resolves.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Crowd Control" }, "amount": 1
                } },
                { "then": {
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } },
                  "then": { "dealDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? crowd = null;
        Card? main = null;
        Card? villain = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                crowd = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crowd.PlaceTokens("k_threat", 1);
                main = board.TheCardIn(DeckType.MainSchemesArea)!;
                main.PlaceTokens("k_threat", 1);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(1, crowd!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(1, main!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:crisis-icon.1")]
    [Rule("rr:then")]
    [Fact]
    public void ExplicitCrisisExceptionIsHonoredByInitiationResolutionAndThen()
    {
        // Crisis says a player card cannot remove threat from the main scheme.
        // This exact instruction says it ignores crisis, so the explicit card
        // exception wins and the fully resolved removal permits its `then`.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "then": {
              "effect": { "removeThreat": {
                "scheme": { "query": "mainScheme" },
                "amount": 1,
                "ignoresCrisis": "true"
              } },
              "then": { "draw": { "player": "you", "count": 1 } }
            } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var crisis = board.CreateCard(
                    "01108", board.AreaOf(DeckType.SideSchemesArea));
                crisis.PlaceTokens("k_threat", 1);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 2);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(
            1,
            world.TheCardIn(DeckType.MainSchemesArea)!
                .Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:permanent.5")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DirectDiscardPreflightsPermanentHostedCardsBeforeCost()
    {
        // A Permanent card cannot leave play. Discarding its host would require
        // attachment cleanup, so eligibility must refuse before exhausting the
        // source rather than discovering the unsupported cleanup afterwards.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? guard = null;
        Card? permanent = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                permanent = board.CreateCard(
                    "27189a",
                    board.AreaOf(
                        DeckType.UpgradesArea, guard.Area.PlayArea,
                        guard.ObjectId));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("rr:permanent.5 is not implemented", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, guard!.Area.Type);
        Assert.Equal(guard.ObjectId, permanent!.Area.Host);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void CharacterWhenDefeatedRaisesBeforeALabelledPowerMutates()
    {
        // When Defeated resolves before the defeated card leaves play. Advanced
        // Ultron Drone creates another Drone at that point, so eligibility must
        // refuse rather than trace the later effects against an empty board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Advanced Ultron Drone" }, "amount": 100
                } },
                { "grantUntil": {
                  "card": { "query": "dronesEngagedWithYou" },
                  "keyword": "health", "amount": 1, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "query": "drones" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "dronesEngagedWithYou" },
                  "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? advanced = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                advanced = board.CreateCard(
                    "01143",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("defeat-triggered ability", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, advanced!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ExternalDefeatInterruptRaisesBeforeALabelledPowerMutates()
    {
        // Damage step 7 resolves every forced interrupt that answers the
        // defeat before step 8 discards the character. Spider-Tracer answers
        // its host's defeat and asks the player to choose a scheme.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "titled": "Shocker" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "minionsEngagedWithYou" }, "amount": 100
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? minion = null;
        Card? tracer = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                tracer = board.CreateCard(
                    "01007",
                    board.AreaOf(
                        DeckType.UpgradesArea, minion.Area.PlayArea,
                        minion.ObjectId, 0));
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 1);
            },
            hero: true,
            abilities: runner));

        Assert.Contains("defeat-triggered ability", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(minion.ObjectId, tracer!.Area.Host);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EarlierDiscardedDefeatInterruptDoesNotCauseAFalseRefusal()
    {
        // Spider-Tracer only answers while it remains attached. Once the first
        // effect discards it, defeating its former host has no step-7 ability
        // to resolve and the labelled sequence is safe to advertise.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "titled": "Hydra Mercenary" },
              "effect": { "seq": [
                { "discard": { "titled": "Spider-Tracer" } },
                { "dealDamage": {
                  "cards": { "query": "minionsEngagedWithYou" }, "amount": 100
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? guard = null;
        Card? tracer = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                tracer = board.CreateCard(
                    "01007",
                    board.AreaOf(
                        DeckType.UpgradesArea, guard.Area.PlayArea,
                        guard.ObjectId, 0));
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 1);
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(guard.ObjectId, tracer!.Area.Host);
    }

    [Rule("rr:damage.step.6")]
    [Rule("rr:would.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void WouldBeDefeatedInterruptRaisesBeforeALabelledPowerMutates()
    {
        // Step 6 resolves "would be defeated" interrupts after damage is
        // placed and before defeat. Biomechanical Upgrades heals its host and
        // discards itself, invalidating the imminent defeat under rr:would.1.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Hydra Mercenary" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? guard = null;
        Card? upgrade = null;
        Card? villain = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                guard = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                upgrade = board.CreateCard(
                    "01185",
                    board.AreaOf(
                        DeckType.UpgradesArea, guard.Area.PlayArea,
                        guard.ObjectId));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("step-6 interrupt", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, guard!.Damage);
        Assert.Equal(guard.ObjectId, upgrade!.Area.Host);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:damage.step.7")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void UnauthoredPrintedDefeatAbilityRaisesBeforeLabelledCost()
    {
        // Goblin Soldier prints a When Defeated ability that has no authored
        // behavior. The engine raises rather than guessing, and eligibility
        // must do so before either the cost or lethal damage mutates the board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Goblin Soldier" }, "amount": 100
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? soldier = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                soldier = board.CreateCard(
                    "02023",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("defeat-triggered ability", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, soldier!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, soldier.Area.Type);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:hit-points.2.3")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ChangedThreatConditionCannotLeaveStaleConstantHealthInTheTrace()
    {
        // Constant abilities update whenever the game state changes. Infinite
        // Soldier has +3 hit points only while Gene Pool has at least 9 threat;
        // removing one threat makes the following 3 damage lethal.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Gene Pool" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Infinite Soldier" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? pool = null;
        Card? soldier = null;
        Card? villain = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                pool = board.CreateCard(
                    "45071", board.AreaOf(DeckType.SideSchemesArea));
                pool.PlaceTokens("k_threat", 9);
                soldier = board.CreateCard(
                    "45069",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(9, pool!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, soldier!.Damage);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:hit-points.2.3")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RepeatedTraceUsesHealthAfterAConditionalConstantEnds()
    {
        // The repeated-frame tracer sees the same continuous update as direct
        // reachability: at eight Gene Pool threat, Infinite Soldier has three
        // hit points, so its defeat removes Guard before the next frame.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": {
                "player": "firstPlayer", "form": "hero"
              } },
              "then": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Gene Pool" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Infinite Soldier" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": {
                  "enemies": { "query": "villain" }
                } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? pool = null;
        Card? soldier = null;
        Card? villain = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                pool = board.CreateCard(
                    "45071", board.AreaOf(DeckType.SideSchemesArea));
                pool.PlaceTokens("k_threat", 9);
                soldier = board.CreateCard(
                    "45069",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(9, pool!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, soldier!.Damage);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:target.3.8")]
    [Fact]
    public void GuardPreventsTracingAnOtherwiseSafeLabelledAttack()
    {
        // Guard says “The engaged player cannot attack any villain,” and a
        // target that cannot be attacked is not valid for an attack-labeled
        // ability. Trace safety cannot make the current target legal.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Gene Pool" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Infinite Soldier" }, "amount": 1
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? pool = null;
        Card? soldier = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                pool = board.CreateCard(
                    "45071", board.AreaOf(DeckType.SideSchemesArea));
                pool.PlaceTokens("k_threat", 10);
                soldier = board.CreateCard(
                    "45069",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(10, pool!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, soldier!.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:hit-points.2.3")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void OneEndingConditionalGrantDoesNotRemoveAnotherFromTheSameSource()
    {
        // At eight threat the >=9 grant ends while the >=5 grant from the same
        // source remains. Trace health is therefore 13, not the printed 10,
        // and one damage at nine does not eliminate Spider-Man.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "attack": {
                      "target": { "query": "villain" },
                      "effect": { "seq": [
                        { "removeThreat": {
                          "scheme": { "titled": "Gene Pool" }, "amount": 1
                        } },
                        { "dealDamage": {
                          "cards": { "titled": "Spider-Man" }, "amount": 1
                        } },
                        { "if": {
                          "test": { "inForm": {
                            "player": "firstPlayer", "form": "hero"
                          } },
                          "then": { "dealAttackDamage": {
                            "cards": { "query": "villain" }, "amount": 1
                          } },
                          "else": { "enemyAttacks": {
                            "enemies": { "query": "villain" }
                          } }
                        } }
                      ] }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [
                    {
                      "trigger": { "timing": "Constant", "subject": "this" },
                      "effect": { "if": {
                        "test": { "atLeast": {
                          "value": { "tokensOn": { "titled": "Gene Pool" } },
                          "count": 9
                        } },
                        "then": { "grant": {
                          "card": "you", "keyword": "health", "amount": 3
                        } }
                      } }
                    },
                    {
                      "trigger": { "timing": "Constant", "subject": "this" },
                      "effect": { "if": {
                        "test": { "atLeast": {
                          "value": { "tokensOn": { "titled": "Gene Pool" } },
                          "count": 5
                        } },
                        "then": { "grant": {
                          "card": "you", "keyword": "health", "amount": 3
                        } }
                      } }
                    }
                  ] }
                ] }
                """));
        Card? source = null;
        Card? pool = null;
        Card? grants = null;

        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                pool = board.CreateCard(
                    "45071", board.AreaOf(DeckType.SideSchemesArea));
                pool.PlaceTokens("k_threat", 9);
                grants = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, pool!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.SupportsArea, grants!.Area.Type);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:hit-points.2.3")]
    [Rule("rr:player-elimination")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DiscardedIdentityHealthGrantRebindsFirstPlayerInTheTrace()
    {
        // Mark V Armor raises Iron Man from 9 to 15 hit points. Once the first
        // effect discards it, one more damage at eight is lethal and the first
        // player token moves before the following form-dependent branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "discard": { "titled": "Mark V Armor" } },
                { "dealDamage": {
                  "cards": { "titled": "Iron Man" }, "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? armor = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                board.Seats[0].IdentityCard.TurnTo("01029a");
                source = InPlay(board, AuthoredCards.AuntMay);
                armor = board.CreateCard(
                    "01036",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            heroes: ["iron_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.UpgradesArea, armor!.Area.Type);
        Assert.Equal(8, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:permanent.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void PermanentOnDepartingVillainRaisesBeforeALabelledCostMutates()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "enemyAttacks": { "enemies": { "query": "villain" } } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? villain = null;
        Card? permanent = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                permanent = board.CreateCard(
                    "27189a",
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("rr:permanent.5 is not implemented", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(villain.ObjectId, permanent!.Area.Host);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteringVillainConstantRaisesBeforeALabelledPowerMutates()
    {
        // Constant abilities apply as soon as their source enters play. Ultron
        // III therefore gives each Drone +1 hit point when the stage advances;
        // the eligibility trace refuses to guess at that changed board.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "query": "drones" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        Card? drone = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                drone = FacedownDrones.EngageTop(
                    board, 0, "test", "Create_Drone", []);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("constant abilities", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(0, drone!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RetargetingVillainConstantRaisesBeforeALabelledPowerMutates()
    {
        // When Klaw I is defeated, Klaw II enters play and becomes the
        // villain. The Immortal Klaw's continuous +10 hit points therefore
        // applies to Klaw II before the next effect resolves; the eligibility
        // trace refuses to keep that modifier bound to the defeated stage.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 36
                } },
                { "moveDamage": {
                  "from": { "query": "villain" }, "to": "you", "amount": 1
                } },
                { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "dealAttackDamage": {
                    "cards": { "query": "villain" }, "amount": 1
                  } },
                  "else": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        Card? immortal = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                immortal = board.CreateCard(
                    "01127", board.AreaOf(DeckType.SideSchemesArea));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(DeckType.SideSchemesArea, immortal!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void InactiveConstantVillainGrantDoesNotPreventStageAdvancement()
    {
        // Bomb Scare is not in play before or after Klaw I is defeated, so
        // this conditional constant never grants hit points to either stage.
        // Only an active branch can retarget and make the trace unsafe.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "attack": {
                      "target": { "query": "villain" },
                      "effect": { "seq": [
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 100
                        } },
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 1
                        } }
                      ] }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "titleInPlay": "Bomb Scare" },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
        Card? source = null;
        Card? conditional = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void InvariantAmountTestDoesNotActivateVillainGrantBranch()
    {
        // One is at least one regardless of damage placed during the trace.
        // The live branch grants this support attack; the unreachable else
        // branch cannot retarget its villain health grant during advancement.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "attack": {
                      "target": { "query": "villain" },
                      "effect": { "seq": [
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 100
                        } },
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 1
                        } }
                      ] }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "atLeast": { "value": 1, "count": 1 } },
                      "then": { "grant": {
                        "card": "this", "keyword": "attack", "amount": 1
                      } },
                      "else": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
        Card? source = null;
        Card? conditional = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void VillainExistenceRemainsTrueAcrossStageAdvancement()
    {
        // Klaw II replaces Klaw I during advancement, so a villain exists on
        // both sides of the transition. The impossible else branch cannot
        // contribute a continuous villain health grant.
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                """
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "attack": {
                      "target": { "query": "villain" },
                      "effect": { "seq": [
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 100
                        } },
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 1
                        } }
                      ] }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "exists": { "query": "villain" } },
                      "then": { "grant": {
                        "card": "this", "keyword": "attack", "amount": 1
                      } },
                      "else": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
        Card? source = null;
        Card? conditional = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void OldVillainTitleEndingActivatesGrantBeforeNewStageContinues()
    {
        // Rhino leaves play before Ultron III enters. The absence of a card
        // titled Rhino activates this conditional villain health grant, so the
        // continuation cannot use the unchanged board's old-title answer.
        var runner = VillainTitleExistenceGrantRunner(grantWhenExists: false);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            },
            hero: true,
            abilities: runner));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01094", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void OldVillainTitleEndingDeactivatesGrantBeforeNewStageContinues()
    {
        // This inverse grant is active only while Rhino exists. Moving to
        // Ultron III ends it, so the unreachable branch does not prevent the
        // otherwise traceable continuation from being advertised.
        var runner = VillainTitleExistenceGrantRunner(grantWhenExists: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DecisiveBooleanConstantBranchRemainsTraceable(bool useOr)
    {
        // A known true decides OR and a known false decides AND even when the
        // other operand reads the advancing villain's status. In either case
        // the branch containing the villain health grant is unreachable.
        var runner = BooleanShortCircuitVillainGrantRunner(useOr);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteringSameTitleStageKeepsVillainGrantActive()
    {
        // Klaw II enters as Klaw I leaves, so a card titled Klaw remains in
        // play throughout advancement. The title-gated health grant follows
        // the new villain and must be refused before the labelled cost.
        var runner = TitleInPlayVillainGrantRunner(grantWhenPresent: true);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteringSameTitleStageKeepsInverseGrantInactive()
    {
        // The inverse branch stays inactive because Klaw II preserves the
        // title's in-play truth. Its unreachable villain grant does not block
        // advertising the continuation.
        var runner = TitleInPlayVillainGrantRunner(grantWhenPresent: false);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnrelatedToughChangeDoesNotActivateSupportGrant(bool repeated)
    {
        // Giving an identity Tough does not make this support Tough. Its
        // conditional villain health grant stays inactive in both direct and
        // repeated traces, so unrelated status state cannot block the action.
        var runner = UnrelatedStatusVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToughChangeDoesNotInvalidateStunnedPredicate(bool repeated)
    {
        // Spider-Man gains Tough, but his Stunned state remains false. Status
        // invalidation is keyed by both card and status, so the unreachable
        // villain health grant remains inactive in either trace shape.
        var runner = UnrelatedStatusVillainGrantRunner(
            repeated, sameCardDifferentStatus: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StunnedGainActivatesVillainGrantBeforeAdvancement(bool repeated)
    {
        // Giving Spider-Man Stunned makes the matching conditional constant
        // active before Klaw advances. The new-stage health grant cannot be
        // projected from the unchanged board, so refusal precedes the cost.
        var runner = UnrelatedStatusVillainGrantRunner(
            repeated, sameCardDifferentStatus: true, giveStunned: true);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnrelatedTraitChangeDoesNotActivateSupportGrant(bool repeated)
    {
        // Giving an identity Aerial does not give this support Brute. Its
        // conditional villain health grant stays inactive in direct and
        // repeated traces, so an unrelated trait cannot block the action.
        var runner = UnrelatedTraitVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AerialGainDoesNotInvalidateBrutePredicate(bool repeated)
    {
        // Spider-Man gains Aerial, but his Brute predicate remains false.
        // Trait invalidation is keyed by both card and trait, so its inactive
        // villain grant cannot hide either shape of the legal action.
        var runner = UnrelatedTraitVillainGrantRunner(
            repeated, sameCardDifferentTrait: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attach-to.1")]
    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LeavingHostedCardInvalidatesItsTraitPredicate(bool repeated)
    {
        // A different-title villain stage discards the old stage's hosted
        // attachment. Enhanced Ivory Horn therefore stops being an in-play
        // Weapon and activates the new-stage grant before either traced cost.
        var runner = DiscardedTraitVillainGrantRunner(repeated);
        Card? source = null;
        Card? horn = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                horn = board.CreateCard(
                    "01100",
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(villain.ObjectId, horn!.Area.Host);
    }

    [Rule("rr:attach-to.1")]
    [Rule("rr:villain-defeat.3.2")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false, "kind")]
    [InlineData(true, "kind")]
    [InlineData(false, "title")]
    [InlineData(true, "title")]
    public void LeavingHostedCardInvalidatesItsIdentityPredicate(
        bool repeated, string predicate)
    {
        // Once Rocket Boots leaves with the old villain stage, it is neither
        // an in-play upgrade nor the in-play card of that title.
        // Both exact predicates therefore activate the new-stage grant.
        var runner = DiscardedTraitVillainGrantRunner(repeated, predicate);
        Card? source = null;
        Card? boots = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                boots = board.CreateCard(
                    "01039",
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(villain.ObjectId, boots!.Area.Host);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CappedStatusGrantDoesNotChangeStatusPredicate(bool repeated)
    {
        // A character cannot receive a second status of the same type. Giving
        // Spider-Man Stunned while he already carries it is a no-op, so the
        // inverse predicate and its villain grant remain inactive.
        var runner = UnrelatedStatusVillainGrantRunner(
            repeated, sameCardDifferentStatus: true,
            giveStunned: true, grantWhenStatusAbsent: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                Statuses.Give(
                    board, board.Seats[0].IdentityCard, Statuses.Stunned);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CappedToughGrantDoesNotChangeStatusPredicate(bool repeated)
    {
        // Spider-Man already has Tough, so another grant is capped and leaves
        // the inverse predicate false. Neither direct nor repeated preflight
        // may explore its inactive villain-health branch.
        var runner = CappedToughVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Statuses.Give(
                    board, board.Seats[0].IdentityCard, Statuses.Tough);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:damage.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VillainDamageDoesNotInvalidateHeroDamagePredicate(bool repeated)
    {
        // Damage on the villain does not put damage on Spider-Man. His numeric
        // predicate remains false, so the inactive villain-health grant cannot
        // reject either otherwise legal trace shape.
        var runner = UnrelatedDamageVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:discard.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MinionDepartureDoesNotInvalidateAllyCount(bool repeated)
    {
        // Discarding an engaged minion does not change the number of allies
        // this player controls. The ally-count condition remains false, so its
        // inactive villain grant cannot reject the legal action.
        var runner = UnrelatedMinionCountVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01167",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:engage.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnteredMinionInvalidatesEngagementCount(bool repeated)
    {
        // Putting Hydra Mercenary into play engaged with the resolving player
        // changes that player's engaged-minion count from zero to one. The
        // resulting villain grant must be recognized before paying the cost.
        var runner = EnteredEngagementCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormChangeInvalidatesHeroCount(bool repeated)
    {
        // Spider-Man is the only hero in play. Changing him to alter-ego makes
        // the hero count zero and activates the conditional villain grant
        // before Klaw advances, so the cost must remain unpaid.
        var runner = FormHeroCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormChangeInvalidatesYourHeroCount(bool repeated)
    {
        // The resolving player begins in alter-ego, so "yourHero" names no
        // card. Changing to hero makes its count one and activates the villain
        // grant before Klaw advances.
        var runner = YourHeroCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeroEliminationDeactivatesHeroCountGrant(bool repeated)
    {
        // Spider-Man is the only hero. His elimination removes him from the
        // player-order-backed hero query, so the live villain grant ends before
        // Klaw advances and cannot reject the otherwise legal action.
        var runner = EliminatedHeroCountVillainGrantRunner(repeated);
        Card? source = null;

        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EliminationReengagementActivatesMinionCountGrant(bool repeated)
    {
        // Hydra Mercenary begins with player one. Eliminating that player
        // makes the minion engage player zero, activating player zero's
        // engagement-count villain grant before Klaw advances.
        var runner = EliminationEngagementCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), mercenary!.Area.PlayArea);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EliminationReengagementMovesHostedUpgradeForCount(bool repeated)
    {
        // Re-engagement moves the minion's complete hosted tree. The hosted
        // upgrade therefore enters player zero's play area and activates that
        // player's upgrade-count villain grant before Klaw advances.
        var runner = EliminationHostedUpgradeCountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? implant = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
                implant = board.CreateCard(
                    "04119",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(1),
                        mercenary.ObjectId));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), mercenary!.Area.PlayArea);
        Assert.Equal(PlayArea.Of(1), implant!.Area.PlayArea);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ConstantYouUsesItsControllersFormDuringPreflight()
    {
        // Player one controls the constant, so its "you" reads player one even
        // though player zero initiates the labelled action.
        var runner = ControllerFormVillainGrantRunner();
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                board.Seats[1].IdentityCard.TurnTo("01010a");
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:ability.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void ConstantYouDoesNotUseInitiatorsFormDuringPreflight()
    {
        // Player zero is in hero form, but player one's alter-ego controls the
        // constant. Its inactive branch cannot be borrowed from the initiator.
        var runner = ControllerFormVillainGrantRunner();
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:ownership-and-control.5")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RelocatedUpgradeControllerUsesItsProjectedPlayArea()
    {
        // Spider-Tracer is a player upgrade hosted by player one's minion.
        // Re-engagement moves it to hero player zero, changing its controller
        // and activating its controller-form villain grant.
        var runner = RelocatedUpgradeControllerVillainGrantRunner();
        Card? source = null;
        Card? tracer = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
                tracer = board.CreateCard(
                    "01007",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(1),
                        mercenary.ObjectId, cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), tracer!.Area.PlayArea);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:referential-ability.step.3")]
    [Fact]
    public void PlayerCardReferenceDoesNotTrackSameTitledEncounterCards()
    {
        // A player card's ambiguous title reference resolves only among player
        // cards. Neither Shocker is therefore the numeric target, so
        // removing one cannot retarget the reference to the other. The
        // independently valid villain target keeps the action initiable.
        var runner = SameTitleNumericRebindingVillainGrantRunner();
        Card? source = null;
        Card? first = null;
        Card? villain = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                first = board.CreateCard(
                    "01103",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                var second = board.CreateCard(
                    "01103",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                second.TakeDamage(1);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, first!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:player-elimination.5")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RemovedIdentityRemainingHealthIsExactlyZero()
    {
        // Spider-Man's removal makes the selector absent, so remainingHealth
        // is zero and the surviving support's villain grant ends.
        var runner = RemovedIdentityHealthVillainGrantRunner();
        Card? source = null;

        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TakeDamage(9);
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-elimination.1")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EliminationPermanentAttachmentRaisesBeforePayment()
    {
        // Power Stone is a Permanent attachment. Eliminating its hero would
        // require resolving its attach-to text, which is intentionally
        // unsupported, so eligibility must refuse before the exhaust cost.
        var runner = PermanentEliminationRunner();
        Card? source = null;
        Card? stone = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var identity = board.Seats[0].IdentityCard;
                stone = board.CreateCard(
                    "16149",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0),
                        identity.ObjectId, cardOwner: -1));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("permanent attachment", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.UpgradesArea, stone!.Area.Type);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HealthModifierInvalidatesRemainingHealthPredicate(bool repeated)
    {
        // The lasting +1 health makes undamaged Spider-Man's remaining health
        // eleven before Klaw advances. That activates the retargeting constant,
        // which must be refused before the labelled cost or lasting state lands.
        var runner = HealthModifierVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:discard.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DepartureInvalidatesRemainingHealthPredicate(bool repeated)
    {
        // Discarding Vulture removes it from play, so its queried remaining
        // health becomes zero. The inverse constant then grants health to the
        // new villain and must be recognized before the action exhausts.
        var runner = DepartedAmountVillainGrantRunner(repeated);
        Card? source = null;
        Card? vulture = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                vulture = board.CreateCard(
                    "01167",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, vulture!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ZeroHealthModifierDoesNotInvalidatePredicate(bool repeated)
    {
        // A zero modifier does not alter Spider-Man's remaining health. The
        // threshold remains false, so its inactive villain grant cannot make
        // either legal action shape look unsupported.
        var runner = ZeroHealthModifierVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VillainDamageDoesNotInvalidateHeroModifiedField(bool repeated)
    {
        // Damaging Klaw does not modify Spider-Man's attack. His threshold
        // remains false, so unrelated damage cannot expose the inactive
        // villain-health branch in either preflight shape.
        var runner = UnrelatedModifiedVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EntryInvalidatesRemainingHealthPredicate(bool repeated)
    {
        // Hydra Mercenary begins out of play with queried remaining health
        // zero. Putting it into play makes that amount positive and activates
        // the villain grant before advancement, so cost must remain unpaid.
        var runner = EnteredAmountVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrossCardConditionalModifierInvalidatesModifiedField(bool repeated)
    {
        // Damage on Vulture activates one constant that grants Spider-Man +1
        // attack. A second constant then reaches its threshold and retargets
        // health to the new villain, a dependency chain preflight must follow.
        var runner = CrossCardModifierVillainGrantRunner(repeated);
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01167",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01091",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnteredTraitActivatesConditionalModifierDependency(bool repeated)
    {
        // Hydra Mercenary's entry makes its printed HYDRA trait query true.
        // That activates Spider-Man's attack grant, which in turn activates
        // the villain-health grant before Klaw advances.
        var runner = EnteredTraitModifierVillainGrantRunner(repeated);
        Card? source = null;
        Card? mercenary = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.CreateCard(
                    "01091",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(0, villain!.Damage);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecisiveFalseBranchIgnoresChangingEnteredTrait(bool repeated)
    {
        // A villain exists before and after advancement, so the first false
        // conjunct decisively keeps the modifier inactive even though Hydra
        // Mercenary enters and makes the second conjunct true.
        var runner = EnteredTraitModifierVillainGrantRunner(
            repeated, decisiveFalse: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.CreateCard(
                    "01091",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:vulnerable.1")]
    [Rule("rr:permanent.5")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VulnerableStatusDiscardPreflightsPermanentBeforeCost(bool repeated)
    {
        // Becoming Stunned discards a Vulnerable character. Its Permanent
        // attachment makes that cleanup unsupported, so both trace shapes
        // refuse before the labelled action exhausts its source.
        var runner = VulnerableStatusRunner(repeated);
        World? world = null;
        Card? source = null;
        Card? scientist = null;
        Card? permanent = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                scientist = board.CreateCard(
                    "50083",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                permanent = board.CreateCard(
                    "27189a",
                    board.AreaOf(
                        DeckType.UpgradesArea, scientist.Area.PlayArea,
                        scientist.ObjectId));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("rr:permanent.5 is not implemented", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, scientist!.Area.Type);
        Assert.Equal(scientist.ObjectId, permanent!.Area.Host);
        Assert.False(Statuses.Has(world!, scientist, Statuses.Stunned));
    }

    [Rule("rr:target.4")]
    [Rule("rr:target.4.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyFinalStatusGroupDoesNotInvalidateEarlierTargets(bool repeated)
    {
        // The earlier status effects have a valid target. The final effect's
        // empty group is simply skipped: an ability that targets multiple game
        // elements can initiate with one valid target and does not resolve
        // against an element that is no longer valid.
        var runner = ReenteredVulnerableStatusRunner(repeated);
        Card? source = null;
        Card? scientist = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scientist = board.CreateCard(
                    "50083",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, scientist!.Area.Type);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestoredStatusInventoryDoesNotRemainMarkedChanged(bool repeated)
    {
        // Vulture begins Stunned, loses that attachment while discarded, then
        // re-enters and regains Stunned. The final predicate equals the live
        // board again, so its inactive inverse grant cannot block the action.
        var runner = RestoredStatusVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var vulture = board.CreateCard(
                    "01167",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, vulture, Statuses.Stunned);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReentryWithoutToughDoesNotCreateAStatusChange(bool repeated)
    {
        // Vulture has no Tough before or after leaving and re-entering play.
        // A zero trace override is equivalent to the live board and cannot
        // make the inactive Tough-conditioned villain grant appear reachable.
        var runner = ReenteredNoToughVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01167",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:play-put-into-play")]
    [Rule("rr:status-cards.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EntryWithoutToughDoesNotCreateAStatusChange(bool repeated)
    {
        // Hydra Mercenary enters play without Tough. The trace must preserve
        // that absence, so an inactive Tough-conditioned villain grant does
        // not make the otherwise legal action appear unsafe.
        var runner = EnteredNoToughVillainGrantRunner(repeated);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attach-to.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LeavingHostInvalidatesItsStatusPredicate(bool repeated)
    {
        // A status is an attachment and leaves with its host. Discarding the
        // Stunned scientist therefore activates the inverse constant before
        // Klaw advances, which must be recognized before paying the cost.
        var runner = DiscardedStatusVillainGrantRunner(repeated);
        World? world = null;
        Card? source = null;
        Card? scientist = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                scientist = board.CreateCard(
                    "50083",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, scientist, Statuses.Stunned);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, scientist!.Area.Type);
        Assert.True(Statuses.Has(world!, scientist, Statuses.Stunned));
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormChangeEndingVillainGrantDoesNotPreventStageAdvancement(
        bool repeated)
    {
        // Changing to alter-ego ends this hero-only continuous hit-point grant
        // before Klaw I is defeated. Klaw II therefore enters without the
        // modifier in both a direct and an each-player trace.
        var runner = FormConditionalVillainGrantRunner(repeated);
        Card? source = null;
        Card? conditional = null;

        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
        Assert.Equal(AuthoredCards.SpiderMan, world.Seats[0].IdentityCard.FaceId);
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FormChangeEndingHealthGrantActivatesVillainGrantBeforePayment()
    {
        // Spider-Man's hero-only hit-point grant keeps his remaining health at
        // 11. Changing to alter-ego ends it, which activates the conditional
        // villain grant before Klaw advances; refusal must precede the cost.
        var runner = FormConditionalHealthDependencyRunner();
        Card? source = null;
        Card? conditional = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FirstPlayerRebindActivatesVillainGrantBeforeAdvancement()
    {
        // Eliminating alter-ego Spider-Man passes the first-player token to
        // Captain Marvel in hero form. The first-player hero condition then
        // activates and its villain health grant must retarget to Klaw II.
        var runner = FirstPlayerVillainGrantRunner();
        Card? source = null;
        Card? conditional = null;
        Card? villain = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                board.Seats[1].IdentityCard.TurnTo("01010a");
                board.Seats[0].IdentityCard.TakeDamage(9);
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), conditional!.Area.PlayArea);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FirstPlayerRebindEndsVillainGrantBeforeAdvancement()
    {
        // Eliminating hero Spider-Man passes the first-player token to Carol
        // Danvers in alter-ego form. The hero-only villain health grant ends
        // before Klaw II enters and therefore does not retarget.
        var runner = FirstPlayerVillainGrantRunner();
        Card? source = null;
        Card? conditional = null;

        var (game, world) = Playing(
            board =>
            {
                board.Seats[0].IdentityCard.TakeDamage(9);
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw");

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(1), conditional!.Area.PlayArea);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-elimination.5")]
    [Rule("rr:modifiers.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FirstPlayerRebindEndsConditionalHealthGrantBeforePayment()
    {
        // Hero Spider-Man initially makes the first-player condition grant
        // Carol +1 health. His elimination rebinds first player to alter-ego
        // Carol, ending that grant and activating the villain-health branch.
        var runner = FirstPlayerConditionalHealthDependencyRunner();
        Card? source = null;
        Card? villain = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                board.Seats[0].IdentityCard.TakeDamage(9);
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01091",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:each-player.1")]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EachPlayerOrderingChecksEveryVillainGrantPath()
    {
        // The first player chooses the each-player order. Resolving Spider-Man
        // first eliminates him and ends the hero-first-player grant; resolving
        // Carol Danvers first leaves Spider-Man and the grant active when Klaw
        // advances. Eligibility must include that legal ordering.
        var runner = OrderedFirstPlayerVillainGrantRunner();
        Card? source = null;
        Card? villain = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                board.Seats[0].IdentityCard.TakeDamage(9);
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
                villain = board.TheCardIn(DeckType.VillainArea)!;
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.3")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RepeatedAdvanceDiscardsOldVillainConstantAttachment()
    {
        // Rhino's hosted attachment leaves when different-title Ultron III
        // enters play. Its continuous villain health grant is therefore gone
        // before the repeated continuation reads the new stage.
        var runner = DepartingVillainAttachmentRunner();
        Card? source = null;
        Card? attachment = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                attachment = board.CreateCard(
                    AuthoredCards.Charge,
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.NotNull(attachment);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteredCardActivatingVillainGrantRaisesBeforePowerMutates()
    {
        // Putting Hydra Mercenary into play makes the conditional constant
        // active before Klaw I is defeated. Its +10 hit points follows Klaw II,
        // so the trace must not test that branch against the unchanged discard.
        var runner = ConditionalVillainGrantRunner(repeated: false);
        Card? source = null;
        Card? conditional = null;
        Card? mercenary = null;
        Card? villain = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                mercenary = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FinalVillainAdvanceNeedNotModelEnteringConstants()
    {
        // Ultron III's constants become active when it enters play, but this
        // labelled effect ends at that point. No continuation reads them, so
        // the action is legal and can be advertised without projecting them.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void FinalVillainAdvanceAfterAnotherStepNeedNotModelEnteringConstants()
    {
        // The same boundary holds when advancement is the last of several
        // effects: only a later sibling would observe the entering constant.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "heal": { "card": "you", "amount": 1 } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } }
              ] }
            } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;

        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.Seats[0].IdentityCard.TakeDamage(1);
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Fact]
    public void ZeroForEachDoesNotHideALaterResolvableStep()
    {
        // Zero count means the repeated effect does not run; it does not make
        // the enclosing sequence unresolvable. The draw remains a meaningful
        // action and must still be advertised.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "forEach": { "count": 0, "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": { "chooseCard": {
                  "from": { "query": "minions" },
                  "effect": { "discard": "chosen" }
                } }
              } } } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Fact]
    public void ZeroForEachBodyIsUnreachableToContinuationPreflight()
    {
        // The zero-count body contains simultaneous threat placement, a shape
        // that would require a continuation if it ran. It cannot run, so
        // branch preflight must skip it and preserve the later draw action.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "if": {
                "test": { "titleInPlay": "Aunt May" },
                "then": { "forEach": { "count": 0, "effect": { "and": [
                  { "placeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } },
                  { "draw": { "player": "you", "count": 1 } }
                ] } } }
              } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:and")]
    [Fact]
    public void ZeroForEachDoesNotMakeASimultaneousSiblingSuspend()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "and": [
              { "forEach": { "count": 0, "effect": { "placeThreat": {
                "scheme": { "query": "mainScheme" }, "amount": 1
              } } } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void ZeroForEachHasNoResolutionForOtherwise()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "otherwise": {
                "effect": { "forEach": { "count": 0, "effect": {
                  "draw": { "player": "you", "count": 1 }
                } } },
                "otherwise": { "forEach": { "count": 0, "effect": {
                  "placeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  }
                } } }
              } },
              { "draw": { "player": "you", "count": 1 } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:for-each")]
    [Fact]
    public void ZeroForEachChoiceDoesNotMakeALabelledPowerSuspend()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "forEach": { "count": 0, "effect": { "chooseCard": {
                  "from": { "query": "minions" },
                  "effect": { "discard": "chosen" }
                } } } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:tough.2")]
    [Rule("rr:each-player.1")]
    [Fact]
    public void ToughGrantedBeforeEachRepeatedDamagePreventsIt()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "giveStatus": {
                  "card": { "titled": "Spider-Man" }, "status": "tough"
                } },
                { "dealDamage": {
                  "cards": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(8);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void HealthGrantedBeforeRepeatedDamageRaisesItsLethalThreshold()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Spider-Man" },
                  "keyword": "health", "amount": 1, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void ProhibitedMoveLeavesDamageForALaterRepeatedMove()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Madame Hydra" },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? villain = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                villain.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(1, villain!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void ProhibitedDamageCannotReplenishARepeatedMoveSource()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void EarlierDiscardCanRemoveARepeatedMoveProhibition()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "if": {
                  "test": { "titleInPlay": "Legions of Hydra" },
                  "then": { "discard": { "titled": "Legions of Hydra" } }
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Madame Hydra" },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01180", board.AreaOf(DeckType.SideSchemesArea));
                board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.TheCardIn(DeckType.VillainArea)!.TakeDamage(1);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void SideSchemeDefeatCanRemoveARepeatedDamageProhibition()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Legions of Hydra" }, "amount": 1
                } },
                { "dealDamage": {
                  "cards": { "titled": "Madame Hydra" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Madame Hydra" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? legions = null;
        Card? madame = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                legions = board.CreateCard(
                    "01180", board.AreaOf(DeckType.SideSchemesArea));
                legions.PlaceTokens("k_threat", 1);
                madame = board.CreateCard(
                    "01181",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(1, legions!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, madame!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void MinionDefeatCanRemoveARepeatedDamageProhibition()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "drones" }, "amount": 100 } },
                { "dealDamage": { "cards": { "titled": "Ultron" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "titled": "Ultron" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? drone = null;
        Card? ultron = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                ultron = board.CreateCard(
                    "01136", board.AreaOf(DeckType.VillainArea));
                drone = FacedownDrones.EngageTop(
                    board, 0, "test", "Create_Drone", []);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.NotNull(drone);
        Assert.Equal(0, drone!.Damage);
        Assert.Equal(0, ultron!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void ARepeatedFrameUsesTheNewVillainStageAfterDefeat()
    {
        // "Excess damage that is dealt to defeat a villain stage does not
        // carry over to the new stage." The later move therefore reads zero
        // damage from the newly revealed stage, not the defeated card's dial.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "moveDamage": {
                  "from": { "titled": "Rhino" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void AFilteredVillainSelectorMutatesTheNewStageBeforeTheNextFrame()
    {
        // The new stage is the current in-play card titled Rhino. It begins
        // without the defeated stage's excess damage, then receives this
        // effect's next point. The following move must see that point or the
        // trace would offer an ability whose first frame defeats the player.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "villain" }, "trait": "BRUTE"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.4")]
    [Fact]
    public void ATitleSelectorDoesNotFollowADifferentVillainCharacter()
    {
        // A different-title stage is not Rhino. The live title selector finds
        // nothing after Ultron replaces Rhino, so it cannot put damage on
        // Ultron for the following move to carry to the wounded hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "dealDamage": { "cards": { "titled": "Rhino" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void ARankedSelectorIsRecomputedForTheNewVillainStage()
    {
        // Rhino I and Shocker tie for the lowest attack, but Rhino II does
        // not. The selector is evaluated after the stage changes, so only
        // Shocker receives the point and the new villain has none to move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 100 } },
                { "dealDamage": {
                  "cards": { "minBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.2")]
    [Fact]
    public void ARankedSelectorCanGainTheNewVillainStage()
    {
        // Rhino I is below Sandman's attack, while Rhino II ties it. The live
        // maximum therefore gains the new stage after the defeat; its damage
        // must be visible to the following move before the action is offered.
        var runner = RepeatedDynamicTargetRunner(
            """{ "query": "villain" }""",
            """{ "maxBy": { "of": { "query": "enemies" }, "by": "attack" } }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void DefeatingGuardCanMakeTheVillainADynamicDamageTarget()
    {
        // "The engaged player cannot attack any villain" only while Guard is
        // present. Defeating Hydra Mercenary makes Rhino attackable before the
        // next effect resolves, so the subsequent move has damage to carry.
        var runner = RepeatedDynamicTargetRunner(
            """{ "titled": "Hydra Mercenary" }""",
            """{ "query": "attackableEnemies" }""");
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:villain-defeat.3.2")]
    [Fact]
    public void CarriedAttachmentTraitsApplyToTheNewVillainStage()
    {
        // Attachments and their constant abilities carry to a same-title
        // stage. Flight therefore still gives Rhino II AERIAL before the
        // filtered damage resolves, leaving a point for the following move.
        var runner = RepeatedDynamicTargetRunner(
            """{ "query": "villain" }""",
            """{ "withTrait": { "cards": { "query": "villain" }, "trait": "AERIAL" } }""",
            includeAuthored: true);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    "40151",
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability")]
    [Fact]
    public void DiscardedAttachmentStopsGrantingItsTraitDuringTheTrace()
    {
        // A constant ability remains active only while its card is in play.
        // Discarding Cosmic Flight removes AERIAL before the filtered damage,
        // so the wounded identity is no longer one of that effect's targets.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Cosmic Flight" } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "characters" }, "trait": "AERIAL"
                  } },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01017",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void DiscardedAttachmentKeepsItsLastingTraitDuringTheTrace()
    {
        // A lasting effect continues for its specified duration whether or not
        // its source remains in play. Discarding Rocket Boots therefore does
        // not remove the AERIAL it already granted until the phase ends.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Rocket Boots" } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "characters" }, "trait": "AERIAL"
                  } },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var boots = board.CreateCard(
                    "01039",
                    board.AreaOf(
                        DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
                board.Effects.Register(new ContinuousEffect(
                    EffectSource.LastingEffect,
                    Traits.Granted + "AERIAL",
                    Card: boots.ObjectId,
                    Affects: board.Seats[0].IdentityCard.ObjectId,
                    Lasts: new Duration(Until: TimingPoints.EndOfPlayerPhase)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:attachment.1")]
    [Fact]
    public void DiscardedAttachmentStopsModifyingARankedField()
    {
        // An attachment may modify its attached character's ATK "as indicated
        // by the values in the associated fields on the attachment card."
        // Discarding Enhanced Ivory Horn therefore drops Rhino to Shocker's
        // ATK, so both are minimum targets and Rhino's point is available for
        // the lethal move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Enhanced Ivory Horn" } },
                { "dealDamage": {
                  "cards": { "minBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    "01100",
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:traits.1")]
    [Fact]
    public void EarlierTraitGrantChangesALaterDynamicTargetSet()
    {
        // A granted trait is immediately part of what later card abilities
        // query. Granting Rhino AERIAL makes both it and the already-AERIAL
        // Vulture targets before the villain's point is moved to the hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "query": "villain" },
                  "trait": "AERIAL", "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "enemies" }, "trait": "AERIAL"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "27163",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:traits.1")]
    [Fact]
    public void EarlierTraitGrantChangesAMinionOnlyTargetSet()
    {
        // Dynamic membership is not limited to sets that can contain the
        // villain. Giving Shocker AERIAL adds it to the later minion-only set,
        // so its damage is present for the move to the wounded hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Shocker" },
                  "trait": "AERIAL", "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "withTrait": {
                    "cards": { "query": "minions" }, "trait": "AERIAL"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Shocker" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01103",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "27163",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void EarlierNumericGrantChangesALaterRankedTargetSet()
    {
        // Rhino begins below Sandman's attack. The +2 ATK grant makes Rhino
        // the unique maximum before the ranked damage resolves, leaving a
        // point for the following move to carry to the wounded hero.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "query": "villain" },
                  "keyword": "attack", "amount": 2, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void RankedSelectorCanDropAnInitiallySelectedNonVillain()
    {
        // Sandman begins as the maximum attack enemy, but Ultron's next stage
        // exceeds him. The live maximum drops Sandman, so no damage is present
        // on him for the following move and the ability remains legal.
        var runner = RepeatedDynamicTargetRunner(
            """{ "query": "villain" }""",
            """{ "maxBy": { "of": { "query": "enemies" }, "by": "attack" } }""",
            """{ "titled": "Sandman" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void RankedPlayerCharacterSelectorRetainsItsControllerScope()
    {
        // The dynamic rank is over characters the resolving player controls,
        // not over enemies. Hulk is in that set and has the highest attack, so
        // its damage is available for the following move to Spider-Man.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "charactersYouControl" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Hulk" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01050",
                    board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:dash-value.3")]
    [Fact]
    public void ATraceLocalModifierCannotChangeADashPowerForRanking()
    {
        // A referenced dash is "treated as having a value of 0" and "cannot
        // be modified." Giving alter-ego Carol +5 ATK therefore leaves Hulk
        // as the maximum-ATK character and makes his damage available for the
        // lethal move that follows.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Carol Danvers" },
                  "keyword": "attack", "amount": 5, "until": "EndOfRound"
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "characters" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Hulk" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01050",
                    board.AreaOf(
                        DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:enters-play")]
    [Rule("rr:toughness.1")]
    [Fact]
    public void AMinionPutIntoPlayJoinsLaterRankedTargetSets()
    {
        // A card enters play when it moves from an out-of-play area into play,
        // and Toughness gives it a tough status at that point. Sandman joins
        // the enemy set before the ranked damage: the first point consumes
        // tough, the second frame damages Sandman, and Rhino has none to move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "if": {
                  "test": { "not": { "titleInPlay": "Sandman" } },
                  "then": { "putIntoPlay": {
                    "card": { "cardsIn": {
                      "areas": [ "encounterDiscardPile" ], "title": "Sandman"
                    } },
                    "where": "engagedWithYou"
                  } }
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void AGuardMinionPutIntoPlayImmediatelyProtectsTheVillain()
    {
        // Guard means "the engaged player cannot attack any villain."
        // Putting Hydra Mercenary into play engaged with the resolving hero
        // therefore removes Rhino from attackableEnemies before damage lands.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "seq": [
                  { "if": {
                    "test": { "not": { "titleInPlay": "Hydra Mercenary" } },
                    "then": { "putIntoPlay": {
                      "card": { "cardsIn": {
                        "areas": [ "encounterDiscardPile" ],
                        "title": "Hydra Mercenary"
                      } },
                      "where": "engagedWithYou"
                    } }
                  } },
                  { "dealDamage": {
                    "cards": { "query": "attackableEnemies" }, "amount": 1
                  } },
                  { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Spider-Man" }, "amount": 1
                  } }
                ] }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void AOneTimeGuardEntryKeepsItsOriginalEngagementAcrossFrames()
    {
        // Guard protects only the engaged player. Hydra Mercenary enters
        // engaged with player zero once; the next frame does not put it into
        // play again or move that engagement to player one, so Rhino takes the
        // later damage and the lethal continuation must be rejected up front.
        var runner = GuardEntryRunner();
        World? world = null;
        Card? source = null;
        Card? mercenary = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void TraceLocalEngagementOverridesTheCardsFrozenBoardArea()
    {
        // Guard protects only the engaged player. After the hero frame moves
        // Hydra Mercenary from player zero to player one, the unchanged board
        // area must not also leave player zero guarded in the trace. Their
        // later frame damages Rhino, eliminates them, and exposes the third
        // player's unsupported branch before any real mutation occurs.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": {
                "player": "firstPlayer", "form": "alterEgo"
              } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "seq": [
                  { "discard": { "titled": "Hydra Mercenary" } },
                  { "putIntoPlay": {
                    "card": { "titled": "Hydra Mercenary" },
                    "where": "engagedWithYou"
                  } }
                ] },
                "else": { "seq": [
                  { "dealDamage": {
                    "cards": { "query": "attackableEnemies" }, "amount": 1
                  } },
                  { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Spider-Man" }, "amount": 1
                  } }
                ] }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        Card? mercenary = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(0), mercenary!.Area.PlayArea);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:engage.1")]
    [Rule("rr:guard.1")]
    [Fact]
    public void EngagementRelativeQueriesUseTheTraceLocalPlayer()
    {
        // Engagement is the player's play area at runtime. During eligibility,
        // trace-local entry is that area's synthetic equivalent: after the
        // hero re-engages Hydra, minionsEngagedWithYou must find and defeat it,
        // exposing Rhino before the lethal move and unsupported third frame.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": {
                "player": "firstPlayer", "form": "alterEgo"
              } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "seq": [
                  { "discard": { "titled": "Hydra Mercenary" } },
                  { "putIntoPlay": {
                    "card": { "titled": "Hydra Mercenary" },
                    "where": "engagedWithYou"
                  } },
                  { "dealDamage": {
                    "cards": { "query": "minionsEngagedWithYou" }, "amount": 3
                  } },
                  { "dealDamage": {
                    "cards": { "query": "attackableEnemies" }, "amount": 1
                  } },
                  { "moveDamage": {
                    "from": { "query": "villain" },
                    "to": { "titled": "Spider-Man" }, "amount": 1
                  } }
                ] }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        Card? mercenary = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, mercenary!, Statuses.Tough);
                board.Seats[0].IdentityCard.TakeDamage(9);
                board.Seats[1].IdentityCard.TurnTo("01010a");
            },
            heroes: ["spider_man", "captain_marvel", "she_hulk"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(PlayArea.Of(0), mercenary!.Area.PlayArea);
        Assert.True(Statuses.Has(world!, mercenary, Statuses.Tough));
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:lasting-effects.1")]
    [Fact]
    public void TraceLocalGuardImmediatelyProtectsTheVillain()
    {
        // Guard means the engaged player cannot attack the villain, and a
        // lasting effect persists for its specified duration. Granting Sandman
        // Guard therefore removes Rhino from attackableEnemies before damage,
        // leaving no villain damage for the later move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "seq": [
                { "grantUntil": {
                  "card": { "titled": "Sandman" },
                  "keyword": "guard", "amount": 1,
                  "until": "EndOfPlayerPhase"
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
                ] }
              } },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01102",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:guard.1")]
    [Rule("rr:damage.step.7")]
    [Fact]
    public void DefeatingATraceEnteredGuardImmediatelyExposesTheVillain()
    {
        // Guard prevents attacks only while the minion remains engaged. Three
        // damage defeats Hydra Mercenary, so Guard leaves play before the next
        // effect damages Rhino and makes the following lethal move reachable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "if": {
                  "test": { "not": { "titleInPlay": "Hydra Mercenary" } },
                  "then": { "putIntoPlay": {
                    "card": { "cardsIn": {
                      "areas": [ "encounterDiscardPile" ],
                      "title": "Hydra Mercenary"
                    } },
                    "where": "engagedWithYou"
                  } }
                } },
                { "dealDamage": {
                  "cards": { "enemiesWithTrait": "HYDRA" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        Card? mercenary = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:status-cards.2")]
    [Rule("rr:guard.1")]
    [Fact]
    public void DiscardingAndReenteringAMinionDoesNotKeepHostedTough()
    {
        // A tough status card is placed on its character. Discarding Hydra
        // Mercenary discards that hosted status too, so re-entering without
        // printed Toughness leaves it vulnerable: three damage defeats it,
        // Guard leaves, and the later lethal move is reachable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "putIntoPlay": {
                  "card": { "titled": "Hydra Mercenary" },
                  "where": "engagedWithYou"
                } },
                { "dealDamage": {
                  "cards": { "enemiesWithTrait": "HYDRA" }, "amount": 3
                } },
                { "dealDamage": {
                  "cards": { "query": "attackableEnemies" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """);
        World? world = null;
        Card? source = null;
        Card? mercenary = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                Statuses.Give(board, mercenary!, Statuses.Tough);
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, mercenary!.Area.Type);
        Assert.True(Statuses.Has(world!, mercenary, Statuses.Tough));
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:attachment.1")]
    [Fact]
    public void ReenteringAMinionDoesNotKeepItsDiscardedAttachmentModifier()
    {
        // An attachment modifies the character it is attached to. Discarding
        // Hydra Mercenary also discards Gobbler Glider, so the re-entered
        // minion has ATK 1 and is the sole minimum below Rhino's ATK 2.
        var runner = ReenteredAttachmentRankRunner("minBy");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "30027",
                    board.AreaOf(
                        DeckType.UpgradesArea, mercenary.Area.PlayArea,
                        mercenary.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:attachment.1")]
    [Fact]
    public void DiscardedHostedModifierCannotHideALethalRankedContinuation()
    {
        // Neurological Implants gives its attached minion +2 ATK only while
        // attached. Once Hydra Mercenary and the attachment are discarded,
        // re-entered Hydra is ATK 1 and Rhino is the maximum-ATK enemy whose
        // damage makes the unsupported later frame reachable.
        var runner = ReenteredAttachmentRankRunner("maxBy");
        World? world = null;
        Card? source = null;
        Card? mercenary = null;
        Card? implants = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                implants = board.CreateCard(
                    "04119",
                    board.AreaOf(
                        DeckType.UpgradesArea, mercenary.Area.PlayArea,
                        mercenary.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, mercenary!.Area.Type);
        Assert.Equal(mercenary.ObjectId, implants!.Area.Host);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:permanent.5")]
    [Fact]
    public void PermanentHostedDescendantRaisesBeforeAnActionCostMutates()
    {
        // When a permanent attachment loses its host, its attach-to text must
        // resolve and it is removed only if no valid target exists. That path
        // is explicitly unimplemented, so the complete hosted tree is checked
        // before exhausting the action source or discarding its host.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "discard": { "titled": "Hydra Mercenary" } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""");
        World? world = null;
        Card? source = null;
        Card? mercenary = null;
        Card? navigator = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                mercenary = board.CreateCard(
                    "01101",
                    board.AreaOf(
                        DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                navigator = board.CreateCard(
                    "27189a",
                    board.AreaOf(
                        DeckType.UpgradesArea, mercenary.Area.PlayArea,
                        mercenary.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("rr:permanent.5 is not implemented", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EngagedEnemiesArea, mercenary!.Area.Type);
        Assert.Equal(mercenary.ObjectId, navigator!.Area.Host);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Fact]
    public void EnteringConstantAbilityRaisesBeforeARepeatedEffectMutates()
    {
        // Constant abilities are active while their source is in play. The
        // eligibility trace does not move cards, so it must raise rather than
        // rank Titania without her remaining-health ATK constant.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "if": {
                  "test": { "not": { "titleInPlay": "Titania" } },
                  "then": { "putIntoPlay": {
                    "card": { "cardsIn": {
                      "areas": [ "encounterDiscardPile" ], "title": "Titania"
                    } },
                    "where": "engagedWithYou"
                  } }
                } },
                { "dealDamage": {
                  "cards": { "maxBy": {
                    "of": { "query": "enemies" }, "by": "attack"
                  } },
                  "amount": 1
                } },
                { "moveDamage": {
                  "from": { "titled": "Titania" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        World? world = null;
        Card? source = null;
        Card? titania = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                titania = board.CreateCard(
                    "01162",
                    board.AreaOf(DeckType.EncounterDiscardPile));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("constant abilities", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, titania!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void RetargetingVillainConstantRaisesBeforeARepeatedEffectMutates()
    {
        // Defeating Klaw I makes Klaw II the villain, so The Immortal Klaw's
        // continuous +10 hit points follows the new stage. A repeated-effect
        // trace refuses before payment rather than evaluate its next frame
        // with that modifier still bound to Klaw I.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": {
                "player": "firstPlayer", "form": "hero"
              } },
              "then": { "seq": [
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 100
                } },
                { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 36
                } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" }, "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": {
                  "enemies": { "query": "villain" }
                } }
              } }
            } } } }
            """,
            cost: """{ "exhaust": "this" }""",
            includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        Card? immortal = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                villain = board.TheCardIn(DeckType.VillainArea)!;
                immortal = board.CreateCard(
                    "01127", board.AreaOf(DeckType.SideSchemesArea));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(DeckType.SideSchemesArea, immortal!.Area.Type);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:ability.step.1")]
    [Rule("rr:villain-defeat.4")]
    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void EnteredCardActivatingVillainGrantRaisesBeforeRepeatedEffectMutates()
    {
        // The repeated-frame trace also treats Hydra Mercenary as in play
        // after its entry. That activates the continuous villain hit-point
        // grant before Klaw advances, so refusal precedes the exhaust cost.
        var runner = ConditionalVillainGrantRunner(repeated: true);
        Card? source = null;
        Card? conditional = null;
        Card? mercenary = null;
        Card? villain = null;
        World? world = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                world = board;
                source = InPlay(board, AuthoredCards.AuntMay);
                conditional = board.CreateCard(
                    "01092",
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                mercenary = board.CreateCard(
                    "01101", board.AreaOf(DeckType.EncounterDiscardPile));
                villain = board.TheCardIn(DeckType.VillainArea)!;
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner,
            scenario: "klaw"));

        Assert.Contains("retargeting constant", thrown.Message);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, conditional!.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, mercenary!.Area.Type);
        Assert.Equal("01113", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:damage.step.1")]
    [Rule("rr:replacement-effect.1")]
    [Fact]
    public void ForcedReplacementCanLeaveARepeatedMoveSourceEmpty()
    {
        // Damage replacement abilities resolve before damage is placed. Once
        // Armored Rhino Suit puts that damage on itself "instead", "the effect
        // is no longer considered imminent" and Rhino has nothing to move.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
              "then": { "seq": [
                { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } },
                { "moveDamage": {
                  "from": { "query": "villain" },
                  "to": { "titled": "Spider-Man" },
                  "amount": 1
                } }
              ] },
              "else": { "attack": {
                "target": { "query": "villain" },
                "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
              } }
            } } } }
            """,
            includeAuthored: true);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var villain = board.TheCardIn(DeckType.VillainArea)!;
                board.CreateCard(
                    AuthoredCards.ArmoredSuit,
                    board.AreaOf(
                        DeckType.UpgradesArea, villain.Area.PlayArea,
                        villain.ObjectId));
                board.Seats[0].IdentityCard.TakeDamage(9);
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
    }

    [Fact]
    public void BoundedThreatRemovalCannotDefeatADistantSideScheme()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "titleInPlay": "Bomb Scare" }, "then": { "removeThreat": { "scheme": { "titled": "Bomb Scare" }, "amount": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 3);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void InactiveRemovalBranchDoesNotInflateARepeatedMutationBudget()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "if": {
              "test": { "titleInPlay": "Bomb Scare" },
              "then": { "removeThreat": {
                "scheme": { "titled": "Bomb Scare" }, "amount": 1
              } },
              "else": { "seq": [
                { "removeThreat": {
                  "scheme": { "titled": "Bomb Scare" }, "amount": 100
                } },
                { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              ] }
            } } } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "01109", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 3);
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void RepeatedMutationAnalysisFlowsThroughOnePlayersSequence()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "eachPlayer": { "effect": { "seq": [
              { "if": {
                "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
                "then": { "draw": { "player": "you", "count": 1 } },
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
                } }
              } },
              { "discard": "this" },
              { "if": {
                "test": { "not": { "titleInPlay": "Aunt May" } },
                "then": { "changeForm": { "player": "firstPlayer", "to": "alterEgo" } }
              } }
            ] } } }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
    }

    [Rule("rr:choose-option")]
    [Fact]
    public void AChoiceContinuationPreservesEarlierEffectResults()
    {
        // "This way" is scoped to the one ability resolution. Asking a
        // question cannot replace that resolution with a fresh result map.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "heal": { "card": "you", "amount": 1 } }, { "choose": { "options": [ { "exhaust": "this" }, { "ready": "this" } ] } }, { "if": { "test": { "atLeast": { "value": { "result": "healed" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Seats[0].IdentityCard.TakeDamage(1);
            },
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void PayOrExhaustContinuesTheContainingSequence()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "payOrExhaust": { "resources": "YBR", "otherwise": { "exhaust": "this" } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(1));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:activation.7")]
    [Fact]
    public void SequentialActivationWaitsUseTheOriginatingFaceAndFreshResults()
    {
        // The first attack changes neither the identity's current face nor the
        // authored face that owns this ability. Its damage result must not leak
        // into the second wait's later condition.
        var runner = Runner(
            AuthoredCards.SpiderMan,
            "WhenRevealed",
            """{ "seq": [ { "changeForm": { "player": "you", "to": "alter-ego" } }, { "seq": [ { "enemyAttacks": { "enemies": { "query": "villain" } } }, { "enemyAttacks": { "enemies": { "query": "villain" } } } ] }, { "if": { "test": { "atLeast": { "value": { "result": "activationDamage" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""",
            eventName: Steps.CardRevealed);
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, identity, 0);
        Assert.Equal("01001b", identity.FaceId);
        var first = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.Attack);

        runner.ActivationCompleted(world, new EnemyActivation(
            first.Subject, first.Seat, Attacking: true, first.ActivationId,
            Made: true, DamageDealt: 2));
        var second = Assert.Single(
            world.Agenda.Outstanding,
            step => step.What == Steps.Attack && step.ActivationId != first.ActivationId);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);

        runner.ActivationCompleted(world, new EnemyActivation(
            second.Subject, second.Seat, Attacking: true, second.ActivationId,
            Made: true, DamageDealt: 0));

        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void EachPlayerReconstructionUsesTheOriginatingIdentityFace()
    {
        var runner = Runner(
            AuthoredCards.SpiderMan,
            "WhenRevealed",
            """{ "seq": [ { "heal": { "card": "you", "amount": 1 } }, { "changeForm": { "player": "you", "to": "alter-ego" } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "if": { "test": { "atLeast": { "value": { "result": "healed" }, "count": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""",
            eventName: Steps.CardRevealed);
        var (_, world) = Playing(
            board => board.Seats[0].IdentityCard.TakeDamage(1),
            hero: true,
            abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, identity, 0);
        var frame = Assert.Single(
            world.Agenda.Outstanding, step => step.What == Steps.ResolveEachPlayer);
        for (int advances = 0;
             world.Agenda.Current?.What != Steps.ResolveEachPlayer && advances < 20;
             advances++)
        {
            world.Agenda.Advance();
        }
        Assert.Equal(Steps.ResolveEachPlayer, world.Agenda.Current?.What);

        runner.ResolveEachPlayer(
            world, identity, frame.Seat, frame.Index, frame.Tier,
            frame.FinalStep, frame.FinalPlayer);

        Assert.Equal("01001b", identity.FaceId);
        Assert.Equal(0, identity.Damage);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:and.1")]
    [Fact]
    public void AFinalOrderedPowerKeepsNestedAncestorWorkPending()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "seq": [ { "and": [ { "draw": { "player": "you", "count": 1 } }, { "attack": { "target": { "query": "villain" }, "effect": { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } } } } ] }, { "draw": { "player": "you", "count": 1 } } ] } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(1, villain.Damage);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void InvalidOrderedContinuationIsRejectedBeforeRunningAnySibling()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var forged = new PhaseStep(
            Steps.ResumeAbility, 1, 2, Subject: source!.ObjectId, Seat: 0,
            Tier: AbilityType.Action, AbilityOrdinal: 0,
            AbilityPath: ["and:0:0,1:"], AbilityFace: source.FaceId,
            AbilityHasContinuation: true);

        Assert.Throws<RulesNotImplementedException>(
            () => runner.ResumeAbility(world, forged));

        Assert.True(source.Ready);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void LegacyContinuationWithoutSourceProvenanceRaisesInsteadOfSkipping()
    {
        // Continuation metadata from before card-incarnation bindings cannot
        // prove that `this` still means the same copy. That ambiguity raises;
        // it must not silently turn the remaining discard into a no-op.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "draw": { "player": "you", "count": 1 } }, { "discard": "this" } ] }""");
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var legacy = new PhaseStep(
            Steps.ResumeAbility, 1, 2, Subject: source!.ObjectId, Seat: 0,
            Tier: AbilityType.Action, AbilityOrdinal: 0,
            AbilityPath: ["seq:0"], AbilityFace: source.FaceId,
            AbilityHasContinuation: false);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => runner.ResumeAbility(world, legacy));

        Assert.Contains("source-card provenance", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source.Area.Type);
    }

    [Fact]
    public void ADirectLastingEffectCannotBeginOutsideItsPeriod()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "grantUntil": { "card": "this", "trait": "AERIAL", "until": "EndOfAttack" } }""",
            eventName: Steps.CardRevealed);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);

        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => runner.WhenRevealed(world, source!, 0));

        Assert.Contains("outside its named period", thrown.Message, StringComparison.Ordinal);
        Assert.Empty(world.Effects.Active());
    }

    [Fact]
    public void PaymentCannotSwitchIntoALastingEffectOutsideItsPeriod()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "you", "trait": "AERIAL", "until": "EndOfAttack" } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("outside its named period", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AStableFormBranchIgnoresAnUnreachableLastingConstraint()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "this", "trait": "AERIAL", "until": "EndOfAttack" } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void PaymentCannotSwitchIntoALastingEffectWithNoTarget()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "grantUntil": { "card": "attachedTo", "trait": "AERIAL", "until": "EndOfRound" } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("no target after payment", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AFirstActivationResumesTheRestOfASequence()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "seq": [ { "enemyAttacks": { "enemies": { "query": "villain" }, "first": "true" } }, { "draw": { "player": "you", "count": 1 } } ] }""",
            eventName: Steps.CardRevealed);
        Card? source = null;

        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, source!, 0);
        var attack = Assert.Single(
            world.Agenda.Outstanding, pending => pending.What == Steps.Attack);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);

        runner.ActivationCompleted(world, new EnemyActivation(
            attack.Subject, attack.Seat, Attacking: true, attack.ActivationId, Made: false));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AChoiceCannotOfferALastingEffectOutsideItsPeriod()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "choose": { "options": [ { "grantUntil": { "card": "this", "keyword": "attack", "amount": 1, "until": "EndOfAttack" } }, { "draw": { "player": "you", "count": 1 } } ] } }""");
        Card? source = null;

        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        var option = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(1, option.Id);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AResourceCostInsideASequenceIsAdvertised()
    {
        // Tenacity pays one physical resource and discards itself. Discarding
        // the upgrade is automatic, but the resource is a player choice and
        // therefore has to survive the surrounding cost sequence onto the
        // affordance.
        Card? tenacity = null;
        var (game, _) = Playing(
            board =>
            {
                tenacity = InPlay(board, "01093");
                board.Seats[0].IdentityCard.Exhaust();
                Physical(board, 1);
            },
            hero: true);

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == tenacity!.ObjectId);
        var price = Assert.Single(action.CostOptions);

        Assert.Equal("1", price.Cost);
        Assert.Equal(["R"], price.Rule);
    }

    /// <summary>`01003` Backflip — a Spider-Man card printing a physical.</summary>
    private const string Physicals = "01003";

    /// <summary>`01004` Enhanced Spider-Sense — the same count, a mental.</summary>
    private const string Mentals = "01004";

    /// <summary>Empties the hand and fills it with physical resources.</summary>
    private static void Physical(World world, int count) =>
        Hand(world, Physicals, count);

    private static void Hand(World world, string faceId, int count) =>
        Hand(world, player: 0, faceId, count);

    private static void Hand(World world, int player, string faceId, int count)
    {
        foreach (var card in world.Seats[player].Hand.Cards.ToList())
        {
            World.MoveToTop(card, world.Seats[player].Deck);
        }

        for (int made = 0; made < count; made++)
        {
            world.CreateCard(faceId, world.Seats[player].Hand);
        }
    }

    [Rule("rr:resource.4")]
    [Fact]
    public void ThreeOfTheWrongResourceIsNotThreePhysicals()
    {
        // Enough cards and the wrong kind. `rr:resource.4`: "many abilities
        // require specific resource types, and the specified types in the
        // specified quantities must be generated **in order to pay the cost**"
        // -- so a cost of three physicals is not a cost of three.
        //
        // The board is the same as the paying one except for the letter on the
        // cards, which is what makes this a test of the types rather than of
        // the count.
        Card? horn = null;
        var (game, world) = Playing(
            board =>
            {
                horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));

                // Through the reveal: `rr:attach-to` makes "Attach to Rhino" a
                // rule about the card entering play rather than a "When
                // Revealed" ability, so the route in is what attaches it.
                board.Abilities = AuthoredCards.Runner();
                Reveal.Resolve(board, Cards, horn, 0, []);
                Hand(board, Mentals, 3);
            },
            hero: true);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);

        // And paying with them anyway is refused by name rather than half-paid:
        // `rr:initiating-abilities.step.5` aborts "without paying any costs".
        var ability = Assert.Single(AuthoredCards.Runner().Actions(world, 0)
            .Where(pending => pending.Card == horn!.ObjectId)
            .DefaultIfEmpty(new PendingAbility(horn!.ObjectId, AbilityType.Action, 0)));

        int before = world.Seats[0].Hand.Cards.Count;
        var thrown = Assert.Throws<RulesNotImplementedException>(
            () => AuthoredCards.Runner().Act(
                world,
                ability,
                [.. world.Seats[0].Hand.Cards.Select(card => card.ObjectId)],
                []));

        Assert.Contains("requiring 'RRR'", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(before, world.Seats[0].Hand.Cards.Count);
    }

    /// <summary>Puts a support into play under the first player.</summary>
    private static Card InPlay(World world, string faceId) => world.CreateCard(
        faceId, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));

    private static Marvel.Cards.Run.AbilityRunner Runner(
        string card,
        string timing,
        string effect,
        string? cost = null,
        string eventName = "WhenActionTriggered",
        string? player = null,
        long? limit = null,
        bool anyPlayer = false,
        bool includeAuthored = false,
        string? labels = null,
        string? maximum = null)
    {
        var local = Marvel.Cards.Dsl.AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{card}}", "abilities": [ {
                "trigger": { "event": "{{eventName}}", "timing": "{{timing}}", "subject": "game"{{(player is null ? string.Empty : $", \"player\": \"{player}\"")}} },
                {{(cost is null ? string.Empty : $"\"cost\": {cost},")}}
                {{(limit is null ? string.Empty : $"\"limitPerRound\": {limit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},")}}
                {{(anyPlayer ? "\"anyPlayer\": true," : string.Empty)}}
                {{(labels is null ? string.Empty : $"\"labels\": {labels},")}}
                {{(maximum is null ? string.Empty : $"\"maxPer{maximum}\": 1,")}}
                "effect": {{effect}}
            } ] } ] }
            """);
        if (!includeAuthored)
        {
            return new Marvel.Cards.Run.AbilityRunner(local);
        }

        var book = new Marvel.Cards.Dsl.AbilityBook(
            [.. AuthoredCards.Book.Abilities, .. local.Abilities],
            AuthoredCards.Book.Authored.Concat(local.Authored)
                .ToHashSet(StringComparer.Ordinal),
            AuthoredCards.Book.AttachTo);
        return new Marvel.Cards.Run.AbilityRunner(book);
    }

    private static Marvel.Cards.Run.AbilityRunner GuardEntryRunner() => Runner(
        AuthoredCards.AuntMay,
        "Action",
        """
        { "eachPlayer": { "effect": { "if": {
          "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
          "then": { "seq": [
            { "if": {
              "test": { "not": { "titleInPlay": "Hydra Mercenary" } },
              "then": { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDiscardPile" ],
                  "title": "Hydra Mercenary"
                } },
                "where": "engagedWithYou"
              } }
            } },
            { "dealDamage": {
              "cards": { "query": "attackableEnemies" }, "amount": 1
            } },
            { "moveDamage": {
              "from": { "query": "villain" },
              "to": { "titled": "Spider-Man" }, "amount": 1
            } }
          ] },
          "else": { "attack": {
            "target": { "query": "villain" },
            "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
          } }
        } } } }
        """);

    private static Marvel.Cards.Run.AbilityRunner ReenteredAttachmentRankRunner(
        string rank) => Runner(
        AuthoredCards.AuntMay,
        "Action",
        $$"""
        { "eachPlayer": { "effect": { "if": {
          "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
          "then": { "seq": [
            { "discard": { "titled": "Hydra Mercenary" } },
            { "putIntoPlay": {
              "card": { "titled": "Hydra Mercenary" },
              "where": "engagedWithYou"
            } },
            { "dealDamage": {
              "cards": { "{{rank}}": {
                "of": { "query": "enemies" }, "by": "attack"
              } },
              "amount": 1
            } },
            { "moveDamage": {
              "from": { "query": "villain" },
              "to": { "titled": "Spider-Man" }, "amount": 1
            } }
          ] },
          "else": { "attack": {
            "target": { "query": "villain" },
            "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
          } }
        } } } }
        """);

    private static Marvel.Cards.Run.AbilityRunner UnrelatedStatusVillainGrantRunner(
        bool repeated, bool sameCardDifferentStatus = false,
        bool giveStunned = false, bool grantWhenStatusAbsent = false)
    {
        string givenStatus = giveStunned ? "stunned" : "tough";
        string sequence = $$"""
            { "seq": [
              { "giveStatus": { "card": "you", "status": "{{givenStatus}}" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string statusTest = sameCardDifferentStatus
            ? """
              { "hasStatus": {
                "card": { "titled": "Spider-Man" }, "status": "stunned"
              } }
              """
            : """
              { "hasStatus": { "card": "this", "status": "tough" } }
              """;
        if (grantWhenStatusAbsent)
        {
            statusTest = $$"""{ "not": {{statusTest}} }""";
        }
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": {{statusTest}},
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner UnrelatedTraitVillainGrantRunner(
        bool repeated, bool sameCardDifferentTrait = false)
    {
        const string sequence = """
            { "seq": [
              { "grantUntil": {
                "card": "you", "trait": "AERIAL", "until": "EndOfAttack"
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string traitTest = sameCardDifferentTrait
            ? """
              { "hasTrait": {
                "card": { "titled": "Spider-Man" }, "trait": "BRUTE"
              } }
              """
            : """
              { "hasTrait": { "card": "this", "trait": "BRUTE" } }
              """;
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": {{traitTest}},
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner VulnerableStatusRunner(
        bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "A.I.M. Scientist" },
                "status": "stunned"
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return Runner(
            AuthoredCards.AuntMay, "Action", effect,
            cost: """{ "exhaust": "this" }""");
    }

    private static Marvel.Cards.Run.AbilityRunner DiscardedTraitVillainGrantRunner(
        bool repeated, string predicate = "trait")
    {
        const string sequence = """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        string test = predicate switch
        {
            "kind" => """
              { "isKind": {
                "card": { "titled": "Rocket Boots" },
                "kind": "upgrade"
              } }
              """,
            "title" => """
              { "isTitle": {
                "card": { "titled": "Rocket Boots" },
                "title": "Rocket Boots"
              } }
              """,
            _ => """
              { "hasTrait": {
                "card": { "titled": "Enhanced Ivory Horn" },
                "trait": "WEAPON"
              } }
              """,
        };
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": {{test}},
                      "then": { "grant": {
                        "card": "this", "keyword": "attack", "amount": 1
                      } },
                      "else": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner ReenteredVulnerableStatusRunner(
        bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "giveStatus": {
                "card": { "titled": "A.I.M. Scientist" },
                "status": "stunned"
              } },
              { "putIntoPlay": {
                "card": { "titled": "A.I.M. Scientist" },
                "where": "engagedWithYou"
              } },
              { "giveStatus": {
                "card": { "titled": "A.I.M. Scientist" },
                "status": "stunned"
              } },
              { "giveStatus": {
                "card": { "titled": "A.I.M. Scientist" },
                "status": "confused"
              } }
            ] }
            """;
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return Runner(
            AuthoredCards.AuntMay, "Action", effect,
            cost: """{ "exhaust": "this" }""");
    }

    private static Marvel.Cards.Run.AbilityRunner RestoredStatusVillainGrantRunner(
        bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "discard": { "titled": "Vulture" } },
              { "putIntoPlay": {
                "card": { "titled": "Vulture" },
                "where": "engagedWithYou"
              } },
              { "giveStatus": {
                "card": { "titled": "Vulture" }, "status": "stunned"
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "not": { "hasStatus": {
                        "card": { "titled": "Vulture" },
                        "status": "stunned"
                      } } },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner CappedToughVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "giveStatus": { "card": "you", "status": "tough" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "not": { "hasStatus": {
          "card": { "titled": "Spider-Man" }, "status": "tough"
        } } }
        """);

    private static Marvel.Cards.Run.AbilityRunner UnrelatedDamageVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "damageOn": { "titled": "Spider-Man" } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner UnrelatedMinionCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "discard": { "titled": "Vulture" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "count": { "query": "alliesYouControl" } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EnteredEngagementCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "putIntoPlay": {
            "card": { "cardsIn": {
              "areas": [ "encounterDiscardPile" ],
              "title": "Hydra Mercenary"
            } },
            "where": "engagedWithYou"
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "count": {
            "query": "minionsEngagedWithYou"
          } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner FormHeroCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "changeForm": { "player": "you", "to": "alterEgo" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "not": { "atLeast": {
          "value": { "count": { "query": "heroes" } },
          "count": 1
        } } }
        """);

    private static Marvel.Cards.Run.AbilityRunner YourHeroCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "changeForm": { "player": "you", "to": "hero" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "count": "yourHero" },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EliminatedHeroCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "dealDamage": {
            "cards": { "titled": "Spider-Man" }, "amount": 99
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "count": { "query": "heroes" } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EliminationEngagementCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "dealDamage": {
            "cards": { "titled": "Carol Danvers" }, "amount": 99
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "count": {
            "query": "minionsEngagedWithYou"
          } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EliminationHostedUpgradeCountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "dealDamage": {
            "cards": { "titled": "Carol Danvers" }, "amount": 99
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "count": { "query": "upgradesYouControl" } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner ControllerFormVillainGrantRunner() =>
        ConditionalVillainGrantRunner(
            false,
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """,
            """
            { "inForm": { "player": "you", "form": "hero" } }
            """);

    private static Marvel.Cards.Run.AbilityRunner RemovedIdentityHealthVillainGrantRunner() =>
        ConditionalVillainGrantRunner(
            false,
            """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Spider-Man" }, "amount": 1
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """,
            """
            { "atLeast": {
              "value": { "remainingHealth": { "titled": "Spider-Man" } },
              "count": 1
            } }
            """);

    private static Marvel.Cards.Run.AbilityRunner RelocatedUpgradeControllerVillainGrantRunner() =>
        new(Marvel.Cards.Dsl.AbilityCatalog.Parse(
            """
            { "cards": [
              { "card": "01006", "abilities": [ {
                "trigger": {
                  "event": "WhenActionTriggered", "timing": "Action",
                  "subject": "game"
                },
                "cost": { "exhaust": "this" },
                "effect": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "seq": [
                    { "dealDamage": {
                      "cards": { "titled": "Carol Danvers" }, "amount": 99
                    } },
                    { "dealDamage": {
                      "cards": { "query": "villain" }, "amount": 100
                    } },
                    { "dealDamage": {
                      "cards": { "query": "villain" }, "amount": 1
                    } }
                  ] }
                } }
              } ] },
              { "card": "01007", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "if": {
                  "test": { "inForm": {
                    "player": "controller", "form": "hero"
                  } },
                  "then": { "grant": {
                    "card": { "query": "villain" },
                    "keyword": "health", "amount": 10
                  } }
                } }
              } ] }
            ] }
            """));

    private static Marvel.Cards.Run.AbilityRunner SameTitleNumericRebindingVillainGrantRunner() =>
        ConditionalVillainGrantRunner(
            false,
            """
            { "seq": [
              { "discard": { "titled": "Shocker" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """,
            """
            { "atLeast": {
              "value": { "damageOn": { "titled": "Shocker" } },
              "count": 1
            } }
            """);

    private static Marvel.Cards.Run.AbilityRunner PermanentEliminationRunner() =>
        new(Marvel.Cards.Dsl.AbilityCatalog.Parse(
            """
            { "cards": [
              { "card": "01006", "abilities": [ {
                "trigger": {
                  "event": "WhenActionTriggered", "timing": "Action",
                  "subject": "game"
                },
                "cost": { "exhaust": "this" },
                "effect": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "seq": [
                    { "dealDamage": {
                      "cards": { "titled": "Spider-Man" }, "amount": 99
                    } },
                    { "draw": { "player": "you", "count": 1 } }
                  ] }
                } }
              } ] }
            ] }
            """));

    private static Marvel.Cards.Run.AbilityRunner ReenteredNoToughVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "discard": { "titled": "Vulture" } },
          { "putIntoPlay": {
            "card": { "titled": "Vulture" },
            "where": "engagedWithYou"
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "hasStatus": {
          "card": { "titled": "Vulture" }, "status": "tough"
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EnteredNoToughVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "putIntoPlay": {
            "card": { "cardsIn": {
              "areas": [ "encounterDiscardPile" ],
              "title": "Hydra Mercenary"
            } },
            "where": "engagedWithYou"
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "hasStatus": {
          "card": { "titled": "Hydra Mercenary" }, "status": "tough"
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner HealthModifierVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "grantUntil": {
            "card": { "titled": "Spider-Man" },
            "keyword": "health", "amount": 1,
            "until": "EndOfRound"
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "remainingHealth": { "titled": "Spider-Man" } },
          "count": 11
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner DepartedAmountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "discard": { "titled": "Vulture" } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "not": { "atLeast": {
          "value": { "remainingHealth": { "titled": "Vulture" } },
          "count": 1
        } } }
        """);

    private static Marvel.Cards.Run.AbilityRunner ZeroHealthModifierVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "grantUntil": {
            "card": { "titled": "Spider-Man" },
            "keyword": "health", "amount": 0,
            "until": "EndOfRound"
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "remainingHealth": { "titled": "Spider-Man" } },
          "count": 11
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner UnrelatedModifiedVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "modified": {
            "card": { "titled": "Spider-Man" }, "field": "attack"
          } },
          "count": 3
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner EnteredAmountVillainGrantRunner(
        bool repeated) => ConditionalVillainGrantRunner(
        repeated,
        """
        { "seq": [
          { "putIntoPlay": {
            "card": { "cardsIn": {
              "areas": [ "encounterDiscardPile" ],
              "title": "Hydra Mercenary"
            } },
            "where": "engagedWithYou"
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 100
          } },
          { "dealDamage": {
            "cards": { "query": "villain" }, "amount": 1
          } }
        ] }
        """,
        """
        { "atLeast": {
          "value": { "remainingHealth": {
            "titled": "Hydra Mercenary"
          } },
          "count": 1
        } }
        """);

    private static Marvel.Cards.Run.AbilityRunner CrossCardModifierVillainGrantRunner(
        bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "dealDamage": {
                "cards": { "titled": "Vulture" }, "amount": 1
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01091", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "atLeast": {
                        "value": { "damageOn": { "titled": "Vulture" } },
                        "count": 1
                      } },
                      "then": { "grant": {
                        "card": { "titled": "Spider-Man" },
                        "keyword": "attack", "amount": 1
                      } }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "atLeast": {
                        "value": { "modified": {
                          "card": { "titled": "Spider-Man" },
                          "field": "attack"
                        } },
                        "count": 3
                      } },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner EnteredTraitModifierVillainGrantRunner(
        bool repeated, bool decisiveFalse = false)
    {
        const string sequence = """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDiscardPile" ],
                  "title": "Hydra Mercenary"
                } },
                "where": "engagedWithYou"
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        string modifierTest = decisiveFalse
            ? """
              { "and": [
                { "not": { "exists": { "query": "villain" } } },
                { "hasTrait": {
                  "card": { "titled": "Hydra Mercenary" },
                  "trait": "HYDRA"
                } }
              ] }
              """
            : """
              { "hasTrait": {
                "card": { "titled": "Hydra Mercenary" },
                "trait": "HYDRA"
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01091", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": {{modifierTest}},
                      "then": { "grant": {
                        "card": { "titled": "Spider-Man" },
                        "keyword": "attack", "amount": 1
                      } }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "atLeast": {
                        "value": { "modified": {
                          "card": { "titled": "Spider-Man" },
                          "field": "attack"
                        } },
                        "count": 3
                      } },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner ConditionalVillainGrantRunner(
        bool repeated, string sequence, string test)
    {
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": {{test}},
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner DiscardedStatusVillainGrantRunner(
        bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "discard": { "titled": "A.I.M. Scientist" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "not": { "hasStatus": {
                        "card": { "titled": "A.I.M. Scientist" },
                        "status": "stunned"
                      } } },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner TitleInPlayVillainGrantRunner(
        bool grantWhenPresent)
    {
        string branches = grantWhenPresent
            ? """
              "then": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
              } }
              """
            : """
              "then": { "grant": {
                "card": "this", "keyword": "attack", "amount": 1
              } },
              "else": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "attack": {
                      "target": { "query": "villain" },
                      "effect": { "seq": [
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 100
                        } },
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 1
                        } }
                      ] }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "titleInPlay": "Klaw" },
                      {{branches}}
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner DepartingVillainAttachmentRunner()
        => new(Marvel.Cards.Dsl.AbilityCatalog.Parse(
            """
            { "cards": [
              { "card": "01006", "abilities": [ {
                "trigger": {
                  "event": "WhenActionTriggered", "timing": "Action",
                  "subject": "game"
                },
                "cost": { "exhaust": "this" },
                "effect": { "eachPlayer": { "effect": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "seq": [
                    { "dealDamage": {
                      "cards": { "query": "villain" }, "amount": 100
                    } },
                    { "dealDamage": {
                      "cards": { "query": "villain" }, "amount": 1
                    } }
                  ] }
                } } } }
              } ] },
              { "card": "01099", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "grant": {
                  "card": { "query": "villain" },
                  "keyword": "health", "amount": 10
                } }
              } ] }
            ] }
            """));

    private static Marvel.Cards.Run.AbilityRunner BooleanShortCircuitVillainGrantRunner(
        bool useOr)
    {
        string test = useOr
            ? """
              { "or": [
                { "exists": { "query": "villain" } },
                { "hasStatus": {
                  "card": { "query": "villain" }, "status": "tough"
                } }
              ] }
              """
            : """
              { "and": [
                { "not": { "exists": { "query": "villain" } } },
                { "hasStatus": {
                  "card": { "query": "villain" }, "status": "tough"
                } }
              ] }
              """;
        string branches = useOr
            ? """
              "then": { "grant": {
                "card": "this", "keyword": "attack", "amount": 1
              } },
              "else": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
              } }
              """
            : """
              "then": { "grant": {
                "card": { "query": "villain" },
                "keyword": "health", "amount": 10
              } },
              "else": { "grant": {
                "card": "this", "keyword": "attack", "amount": 1
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "attack": {
                      "target": { "query": "villain" },
                      "effect": { "seq": [
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 100
                        } },
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 1
                        } }
                      ] }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": {{test}},
                      {{branches}}
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner OrderedFirstPlayerVillainGrantRunner()
        => new(Marvel.Cards.Dsl.AbilityCatalog.Parse(
            """
            { "cards": [
              { "card": "01006", "abilities": [ {
                "trigger": {
                  "event": "WhenActionTriggered", "timing": "Action",
                  "subject": "game"
                },
                "cost": { "exhaust": "this" },
                "effect": { "eachPlayer": { "effect": { "seq": [
                  { "dealDamage": { "cards": "you", "amount": 1 } },
                  { "attack": {
                    "target": { "query": "villain" },
                    "effect": { "seq": [
                      { "dealDamage": {
                        "cards": { "query": "villain" }, "amount": 100
                      } },
                      { "dealDamage": {
                        "cards": { "query": "villain" }, "amount": 1
                      } }
                    ] }
                  } }
                ] } } }
              } ] },
              { "card": "01092", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "if": {
                  "test": { "inForm": {
                    "player": "firstPlayer", "form": "hero"
                  } },
                  "then": { "grant": {
                    "card": { "query": "villain" },
                    "keyword": "health", "amount": 10
                  } }
                } }
              } ] }
            ] }
            """));

    private static Marvel.Cards.Run.AbilityRunner VillainTitleExistenceGrantRunner(
        bool grantWhenExists)
    {
        string test = grantWhenExists
            ? """{ "exists": { "titled": "Rhino" } }"""
            : """{ "not": { "exists": { "titled": "Rhino" } } }""";
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "attack": {
                      "target": { "query": "villain" },
                      "effect": { "seq": [
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 100
                        } },
                        { "dealDamage": {
                          "cards": { "query": "villain" }, "amount": 1
                        } }
                      ] }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": {{test}},
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner FirstPlayerVillainGrantRunner()
    {
        const string sequence = """
            { "seq": [
              { "dealDamage": {
                "cards": "you", "amount": 1
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = $$"""
            { "attack": {
              "target": { "query": "villain" },
              "effect": {{sequence}}
            } }
            """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "inForm": {
                        "player": "firstPlayer", "form": "hero"
                      } },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner FirstPlayerConditionalHealthDependencyRunner()
    {
        const string sequence = """
            { "seq": [
              { "dealDamage": { "cards": "you", "amount": 1 } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": { "attack": {
                      "target": { "query": "villain" },
                      "effect": {{sequence}}
                    } }
                  } ] },
                  { "card": "01091", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "inForm": {
                        "player": "firstPlayer", "form": "hero"
                      } },
                      "then": { "grant": {
                        "card": { "titled": "Carol Danvers" },
                        "keyword": "health", "amount": 1
                      } }
                    } }
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "not": { "atLeast": {
                        "value": { "remainingHealth": {
                          "titled": "Carol Danvers"
                        } },
                        "count": 13
                      } } },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner FormConditionalVillainGrantRunner(
        bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "changeForm": { "player": "you", "to": "alterEgo" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = repeated
            ? $$"""{ "eachPlayer": { "effect": {{sequence}} } }"""
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "inForm": {
                        "player": "you", "form": "hero"
                      } },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner FormConditionalHealthDependencyRunner()
    {
        const string sequence = """
            { "seq": [
              { "changeForm": { "player": "you", "to": "alterEgo" } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 1
              } }
            ] }
            """;
        string effect = $$"""
            { "attack": {
              "target": { "query": "villain" },
              "effect": {{sequence}}
            } }
            """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [
                    {
                      "trigger": { "timing": "Constant", "subject": "this" },
                      "effect": { "if": {
                        "test": { "inForm": {
                          "player": "you", "form": "hero"
                        } },
                        "then": { "grant": {
                          "card": { "titled": "Spider-Man" },
                          "keyword": "health", "amount": 1
                        } }
                      } }
                    },
                    {
                      "trigger": { "timing": "Constant", "subject": "this" },
                      "effect": { "if": {
                        "test": { "not": { "atLeast": {
                          "value": { "remainingHealth": {
                            "titled": "Spider-Man"
                          } },
                          "count": 11
                        } } },
                        "then": { "grant": {
                          "card": { "query": "villain" },
                          "keyword": "health", "amount": 10
                        } }
                      } }
                    }
                  ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner ConditionalVillainGrantRunner(
        bool repeated)
    {
        const string sequence = """
            { "seq": [
              { "putIntoPlay": {
                "card": { "cardsIn": {
                  "areas": [ "encounterDiscardPile" ],
                  "title": "Hydra Mercenary"
                } },
                "where": "engagedWithYou"
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 100
              } },
              { "dealDamage": {
                "cards": { "query": "villain" }, "amount": 36
              } },
              { "moveDamage": {
                "from": { "query": "villain" },
                "to": { "titled": "Spider-Man" }, "amount": 1
              } },
              { "if": {
                "test": { "inForm": {
                  "player": "firstPlayer", "form": "hero"
                } },
                "then": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                "else": { "enemyAttacks": {
                  "enemies": { "query": "villain" }
                } }
              } }
            ] }
            """;
        string effect = repeated
            ? $$"""
              { "eachPlayer": { "effect": { "if": {
                "test": { "inForm": {
                  "player": "firstPlayer", "form": "hero"
                } },
                "then": {{sequence}},
                "else": { "attack": {
                  "target": { "query": "villain" },
                  "effect": { "enemyAttacks": {
                    "enemies": { "query": "villain" }
                  } }
                } }
              } } } }
              """
            : $$"""
              { "attack": {
                "target": { "query": "villain" },
                "effect": {{sequence}}
              } }
              """;
        return new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [
                  { "card": "01006", "abilities": [ {
                    "trigger": {
                      "event": "WhenActionTriggered", "timing": "Action",
                      "subject": "game"
                    },
                    "cost": { "exhaust": "this" },
                    "effect": {{effect}}
                  } ] },
                  { "card": "01092", "abilities": [ {
                    "trigger": { "timing": "Constant", "subject": "this" },
                    "effect": { "if": {
                      "test": { "titleInPlay": "Hydra Mercenary" },
                      "then": { "grant": {
                        "card": { "query": "villain" },
                        "keyword": "health", "amount": 10
                      } }
                    } }
                  } ] }
                ] }
                """));
    }

    private static Marvel.Cards.Run.AbilityRunner RepeatedDynamicTargetRunner(
        string firstTarget, string secondTarget,
        string moveSource = """{ "query": "villain" }""",
        bool includeAuthored = false) => Runner(
        AuthoredCards.AuntMay,
        "Action",
        $$"""
        { "eachPlayer": { "effect": { "if": {
          "test": { "inForm": { "player": "firstPlayer", "form": "hero" } },
          "then": { "seq": [
            { "dealDamage": { "cards": {{firstTarget}}, "amount": 100 } },
            { "dealDamage": { "cards": {{secondTarget}}, "amount": 1 } },
            { "moveDamage": {
              "from": {{moveSource}},
              "to": { "titled": "Spider-Man" },
              "amount": 1
            } }
          ] },
          "else": { "attack": {
            "target": { "query": "villain" },
            "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } }
          } }
        } } } }
        """,
        includeAuthored: includeAuthored);

    /// <summary>
    /// A game past the mulligan, on the first player's turn.
    /// </summary>
    /// <remarks>
    /// The board is prepared <b>before</b> the game begins, because a turn
    /// prompt is built once and lists what was there when it was built. A card
    /// put into play afterwards is on the board and not in the question.
    /// </remarks>
    private static (Game Game, World World) Playing(
        Action<World> prepare,
        bool hero = false,
        string[]? heroes = null,
        ICardAbilities? abilities = null,
        string scenario = "rhino")
    {
        string[] playing = heroes ?? ["spider_man"];
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, scenario, playing), Cards),
            [.. playing.Select(name => Setup.Hero(name).Name)],
            12345);

        if (hero)
        {
            world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        }

        prepare(world);

        // The mulligan is asked as a turn option too, so the loop watches the
        // verb rather than the question: declining it keeps the opening hand.
        var game = Game.Begin(world, Cards, abilities ?? AuthoredCards.Runner());
        while (game.Pending is { } asked
            && asked.Affordances.Any(
                option => option.Verb == Game.ResolveMulligans))
        {
            game.Resolve(Decision.Decline);
        }

        return (game, world);
    }
}
