using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class IgnoreToughDamageTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompiledFlagSurvivesInputChangesAndDoesNotLeak(bool ignores)
    {
        // Synthetic fixed-X effect: printed 06006 and its vendored erratum specify
        // damage, not an attack. This does not admit Lightning Strike.
        string marker = ignores ? ",\"ignoresTough\":1" : "";
        var (runner, fields) = MutableEffectRunner("dealDamage",
            $$$$$$"""{"cards":{"query":"minionsEngagedWithYou"},"amount":2{{{{{{marker}}}}}}}""", false);
        Card? source = null;
        Card? target = null;
        Card? other = null;
        Card? tough = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            target = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            other = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            tough = Statuses.Give(board, target, Statuses.Tough);
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: "retaliate", Amount: 1, Affects: target.ObjectId));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        fields["ignoresTough"] = new AbilityValue.Number(ignores ? 0 : 1);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(ignores ? 2 : 0, target!.Damage);
        Assert.Equal(0, other!.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(ignores, Statuses.Has(world, target, Statuses.Tough));
        if (ignores) Assert.Same(tough, Assert.Single(Statuses.On(world, target, Statuses.Tough)));
        DamagePlacement.Resolve(world, world.Facts,
            new(source!, target, 1, "test", "Deal_Damage"), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(Statuses.Has(world, target, Statuses.Tough));
        Assert.Equal(ignores ? 2 : 1, target.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:damage.step.6")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryRecipientIsDamagedBeforeTheFirstDefeatWindow(bool ignores)
    {
        // Step 6 opens "when [character] would be defeated" after simultaneous
        // placement on every recipient. The following draw still waits.
        string marker = ignores ? ",\"ignoresTough\":1" : "";
        var runner = DamageContinuationRunner($$$$$$"""
            {"dealDamage":{"cards":{"query":"minionsEngagedWithYou"},"amount":1{{{{{{marker}}}}}}}}
            """);
        Card? source = null;
        Card? first = null;
        Card? later = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            InPlay(board, "01092");
            first = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            first.TakeDamage(Damage.Health(board, board.Facts, first) - 1);
            later = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            if (ignores)
            {
                Statuses.Give(board, first, Statuses.Tough);
                Statuses.Give(board, later, Statuses.Tough);
            }
        }, hero: true, abilities: runner);
        int hand = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(1, later!.Damage);
        Assert.Equal(hand, world.Seats[0].Hand.Cards.Count);
        game.Resolve(Decision.Decline);
        Assert.Equal(DeckType.EncounterDiscardPile, first!.Area.Type);
        Assert.Equal(1, later.Damage);
        Assert.Equal(ignores, Statuses.Has(world, later, Statuses.Tough));
        Assert.Equal(hand + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KillingDronesDoesNotExposeUltronDuringTheSameDamage(bool ignores)
    {
        // "The drone minions are in play when the damage is dealt": even with
        // the drone declared first, simultaneous damage cannot damage Ultron III.
        Card? drone = null;
        Card? ultron = null;
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(board =>
        {
            World.MoveToTop(board.TheCardIn(DeckType.VillainArea)!, board.AreaOf(DeckType.VillainDeck));
            ultron = board.CreateCard("01136", board.AreaOf(DeckType.VillainArea));
            drone = board.CreateCard("01004", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0), cardOwner: 0));
            drone.AssignProfile(AuthoredCards.DroneProfile);
            if (ignores) Statuses.Give(board, drone, Statuses.Tough);
        }, hero: true, abilities: runner);
        var result = SimultaneousDirectDamage.Resolve(world, world.Facts,
            [new(world.Seats[0].IdentityCard, drone!, 100, "test", "Deal_Damage") { IgnoresTough = ignores },
             new(world.Seats[0].IdentityCard, ultron!, 3, "test", "Deal_Damage") { IgnoresTough = ignores }], []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(result.IsComplete);
        Assert.Equal(DeckType.DiscardPile, drone!.Area.Type);
        Assert.Equal(0, ultron!.Damage);
        Assert.Equal(0, result.Recipients[1].Dealt);
        DamagePlacement.Resolve(world, world.Facts,
            new(world.Seats[0].IdentityCard, ultron, 1, "test", "Deal_Damage"), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, ultron.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AreaProjectionRejectsChangingSingularAreaAfterIgnoringTough(bool ignores)
    {
        string marker = ignores ? ",\"ignoresTough\":1" : "";
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"seq":[
              {"dealDamage":{"cards":{"titled":"Hydra Mercenary"},"amount":100{{{{{{marker}}}}}}}},
              {"removeFromGame":{"cardsIn":{"area":"encounterDiscardPile","title":"Hydra Mercenary"}}}
            ]}
            """, cost: """{"exhaust":"this"}""");
        Card? source = null;
        Card? target = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            target = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, target, Statuses.Tough);
        }, hero: true, abilities: AuthoredCards.Runner());
        string before = world.Digest().Canonical();
        long consumed = world.Random.Generator.WordsConsumed;
        var random = world.Random.Generator.GetState();
        var current = world.Agenda.Current;
        bool busy = world.Agenda.IsBusy;
        if (ignores)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
            Assert.Contains("singular area query", error.Message);
        }
        else Assert.Contains(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(current, world.Agenda.Current);
        Assert.Equal(busy, world.Agenda.IsBusy);
        Assert.True(source!.Ready);
        Assert.Equal(0, target!.Damage);
        Assert.True(Statuses.Has(world, target, Statuses.Tough));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedProjectionPreservesToughOnlyForIgnoredInstances(bool ignores)
    {
        string marker = ignores ? ",\"ignoresTough\":1" : "";
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"eachPlayer":{"effect":{"if":{
              "test":{"inForm":{"player":"firstPlayer","form":"hero"}},
              "then":{"dealDamage":{"cards":{"titled":"Spider-Man"},"amount":1{{{{{{marker}}}}}}}},
              "else":{"attack":{"target":{"query":"villain"},"effect":{"enemyAttacks":{"enemies":{"query":"villain"}}}}}
            }}}}
            """, cost: """{"exhaust":"this"}""");
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(9);
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        if (ignores)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
            Assert.Contains("suspends inside a labelled power", error.Message);
        }
        else Assert.Contains(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PowerProjectionRecognizesIgnoredToughDefeatWindow(bool ignores)
    {
        string marker = ignores ? ",\"ignoresTough\":1" : "";
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"attack":{"target":{"titled":"Shocker"},"effect":{"seq":[
              {"dealDamage":{"cards":{"titled":"Shocker"},"amount":100{{{{{{marker}}}}}}}},
              {"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        Card? source = null;
        Card? minion = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01103", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01007", board.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId, 0));
            Statuses.Give(board, minion, Statuses.Tough);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 1);
        }, hero: true, abilities: AuthoredCards.Runner());
        if (ignores)
        {
            var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
            Assert.Contains("unprojected defeat window", error.Message);
        }
        else Assert.Contains(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(0, minion!.Damage);
        Assert.True(Statuses.Has(world, minion, Statuses.Tough));
    }

}

