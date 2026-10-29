using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic ability on a supported research board; no pack admission is implied.
public sealed class NextPlayerTurnProhibitionTests
{
    [Theory]
    [InlineData("Interrupt", true)]
    [InlineData("Response", false)]
    [Rule("rr:interrupt")]
    [Rule("rr:response")]
    [Rule("rr:lasting-effects.5")]
    public void BeginningWindowDeterminesWhichTurnIsNext(string timing, bool expiresThisTurn)
    {
        // rr:interrupt: it resolves immediately before the triggering condition resolves.
        // rr:response: it resolves after that condition occurs.
        var (game, world, source) = Board(timing);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ResolveMulligans).Id));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == source.ObjectId).Id));
        Assert.False(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.Verb == Game.ChangeForm);
        game.Resolve(Decision.Decline);
        Assert.Equal(expiresThisTurn, FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
    }

    [Fact]
    [Rule("rr:lasting-effects.5")]
    public void UnobservedBeginningArmsAnExistingEffect()
    {
        // rr:lasting-effects.5: the effect expires at its specified timing point.
        var (game, world, source) = Board(null);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, source.ObjectId, world.Seats[0].IdentityCard.ObjectId,
            Duration.UntilNextTurnEnds(0)));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ResolveMulligans).Id));
        Assert.False(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
        game.Resolve(Decision.Decline);
        Assert.True(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
    }

    [Fact]
    [Rule("rr:lasting-effects.5")]
    [Rule("rr:interrupt")]
    [Rule("rr:response")]
    public void TurnEndInterruptIsBlockedAndResponseCanChangeForm()
    {
        // rr:lasting-effects.5: the effect expires at the timing point, before responses.
        const string rows = """
            [{"card":"01006","abilities":[
              {"trigger":{"event":"WhenPlayerTurnEnds","timing":"Interrupt","subject":"you"},
               "effect":{"placeCounters":{"card":"this","counter":"probe","count":1}}},
              {"trigger":{"event":"WhenPlayerTurnEnds","timing":"Interrupt","subject":"you"},
               "effect":{"changeForm":{"player":"you","to":{"face":"12001c"}}}},
              {"trigger":{"event":"WhenPlayerTurnEnds","timing":"Response","subject":"you"},
               "effect":{"changeForm":{"player":"you","to":{"face":"12001c"}}}}
            ]}]
            """;
        var (game, world, source) = BoardForRows(rows);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, source.ObjectId, world.Seats[0].IdentityCard.ObjectId,
            Duration.UntilNextTurnEnds(0)));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ResolveMulligans).Id));
        Assert.False(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
        game.Resolve(Decision.Decline);
        Assert.False(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
        var interrupt = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source.ObjectId);
        Assert.Equal(AbilityType.Interrupt, interrupt.Timing);
        game.Resolve(Decision.Decline);
        Assert.True(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
        var response = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source.ObjectId);
        Assert.Equal(AbilityType.Response, response.Timing);
        game.Resolve(Decision.Take(response.Id));
        Assert.Equal("12001c", world.Seats[0].IdentityCard.FaceId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:lasting-effects.1")]
    [Rule("rr:lasting-effects.5")]
    public void EffectCreatedOnEitherPlayersTurnEndsOnOwnersNextTurn(bool onOtherTurn)
    {
        // rr:lasting-effects.1: the effect persists whether its source remains in play.
        var (game, world, source) = BoardForRows("[]", players: 2);
        for (int player = 0; player < 2; player++)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
                offer => offer.Verb == Game.ResolveMulligans).Id));
        if (onOtherTurn) game.Resolve(Decision.Decline);
        Assert.Equal(onOtherTurn ? 1 : 0, game.Active);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, source.ObjectId, world.Seats[0].IdentityCard.ObjectId,
            Duration.UntilNextTurnEnds(0)));
        World.MoveToTop(source, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        Assert.False(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
        Assert.True(FormChangePermissions.CanChange(world, world.Seats[1].IdentityCard));
        game.Resolve(Decision.Decline);
        for (int count = 0; !(game.Round == 2 && game.Phase == GamePhase.PlayerTurn && game.Active == 0)
            && count < 100; count++)
        {
            Assert.False(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
            Continue(game);
        }
        Assert.Equal(2, game.Round);
        Assert.Equal(0, game.Active);
        Assert.False(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
        game.Resolve(Decision.Decline);
        Assert.True(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
    }

    [Fact]
    [Rule("rr:lasting-effects.5")]
    public void EffectCreatedDuringVillainPhaseExpiresAtTheFollowingPlayerTurnEnd()
    {
        // rr:lasting-effects.5: the effect lasts until its specified timing point.
        var (game, world, source) = BoardForRows("[]");
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ResolveMulligans).Id));
        game.Resolve(Decision.Decline);
        for (int count = 0; game.Phase != GamePhase.VillainPhase && count < 30; count++)
            Continue(game);
        Assert.Equal(GamePhase.VillainPhase, game.Phase);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, source.ObjectId, world.Seats[0].IdentityCard.ObjectId,
            Duration.UntilNextTurnEnds(0)));
        for (int count = 0; game.Round == 1 && count < 100; count++)
        {
            Assert.False(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
            Continue(game);
        }
        Assert.Equal(2, game.Round);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.False(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
        game.Resolve(Decision.Decline);
        Assert.True(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
    }

    [Fact]
    [Rule("rr:cannot")]
    public void StaleVoluntaryChoiceIsRejectedWithoutChangingTheDecisionState()
    {
        // rr:cannot: the prohibition cannot be countermanded by other effects.
        var (game, world, source) = BoardForRows("[]");
        var (controlGame, controlWorld, controlSource) = BoardForRows("[]");
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ResolveMulligans).Id));
        controlGame.Resolve(Decision.Take(Assert.Single(controlGame.Pending!.Affordances,
            offer => offer.Verb == Game.ResolveMulligans).Id));
        controlWorld.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, controlSource.ObjectId,
            controlWorld.Seats[0].IdentityCard.ObjectId, Duration.UntilNextTurnEnds(0)));
        var stale = game.Pending!.Affordances.First(offer => offer.Verb == Game.ChangeForm);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, source.ObjectId, world.Seats[0].IdentityCard.ObjectId,
            Duration.UntilNextTurnEnds(0)));
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var prompt = game.Pending;
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(stale.Id)));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        var actual = game.Resolve(Decision.Decline);
        var control = controlGame.Resolve(Decision.Decline);
        Assert.Equal(controlWorld.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Events, actual.Events);
        Assert.False(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
    }

    [Fact]
    [Rule("rr:player-elimination.step.5")]
    public void EliminatedIdentityDepartureRetiresItsDirectLastingEffect()
    {
        // rr:player-elimination.step.5: the eliminated play area is removed from the game.
        var (_, world, source) = BoardForRows("[]", players: 2);
        var identity = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, source.ObjectId, identity.ObjectId,
            Duration.UntilNextTurnEnds(0)));
        Elimination.Eliminate(world, world.Facts, 0, "test", []);
        Assert.True(world.Seats[0].Eliminated);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Affects == identity.ObjectId);
        Assert.True(FormChangePermissions.CanChange(world, world.Seats[1].IdentityCard));
    }

    private static void Continue(Game game)
    {
        var prompt = Assert.IsType<Prompt>(game.Pending);
        var offer = prompt.Affordances[0];
        game.Resolve(prompt.Cancellable ? Decision.Decline : new Decision(offer.Id,
            offer.Targets is { } targets ? [.. targets.Legal.Take(targets.Min)] : []));
    }

    private static (Game Game, World World, Card Source) Board(string? timing)
    {
        string row = timing is null ? "[]" : $$$$"""
            [{"card":"01006","abilities":[{"trigger":{"event":"WhenPlayerTurnBegins",
              "timing":"{{{{timing}}}}","subject":"you"},"effect":{"grantUntil":{
              "card":"you","keyword":"form_change_prohibited","amount":1,"until":"EndOfNextPlayerTurn"}}}]}]
            """;
        return BoardForRows(row);
    }

    private static (Game Game, World World, Card Source) BoardForRows(string row, int players = 1)
    {
        var runner = AntManFormResponseFixture.Runner("{\"cards\":" + row + "}");
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, players, 241);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
            for (int i = 0; i < 20; i++) world.CreateCard("01087", seat.Deck);
            world.CreateCard("01087", seat.Hand);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        for (int i = 0; i < 12; i++) world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = runner;
        return (Game.Begin(world, cards, runner), world, source);
    }
}
