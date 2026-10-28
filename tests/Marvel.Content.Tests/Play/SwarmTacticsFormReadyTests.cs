using System.Text.Json.Nodes;
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

// Printed 12020 and response rows on a nonpublic research board.
public sealed class SwarmTacticsFormReadyTests
{
    private const string Effect = """{"seq":[{"changeForm":{"player":"you","to":"other-hero"}},{"ready":"yourHero"}]}""";
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("12001a", "12001c", false)]
    [InlineData("12001c", "12001a", false)]
    [InlineData("12001a", "12001c", true)]
    [Rule("rr:form-change-form.2")]
    [Rule("rr:form-change-form.3")]
    public void SwarmChangesTheControllersPhysicalFaceAndReadiesIt(string from, string to, bool initiallyReady)
    {
        // "Only the form changes"; a card ability does not count against the voluntary change.
        var runner = Runner();
        var (world, source, payment, _) = Board(runner, from);
        var hero = world.Seats[1].IdentityCard;
        if (!initiallyReady) hero.Exhaust();
        string before = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(to, hero.FaceId);
        Assert.True(hero.Ready);
        Assert.Equal(0, world.Seats[1].FormChangedInRound);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
    }

    [Fact]
    [Rule("rr:initiating-abilities.2")]
    public void FormResponsesAreOfferedBeforeTheFollowingReadyAndInvalidPaymentCanRetry()
    {
        // A response resolves "immediately after" its triggering condition.
        var runner = Runner(responses: true);
        var (world, source, payment, _) = Board(runner, "12001a");
        var hero = world.Seats[1].IdentityCard;
        hero.Exhaust();
        var moxie = world.CreateCard("12016", world.Seats[1].Hand);
        var game = Game.Begin(world, Cards, runner);
        for (int i = 0; i < 2; i++)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
                a => a.Verb == Game.ResolveMulligans).Id));
        var offer = Assert.Single(game.PromptFor(1)!.Affordances, a => a.AnchorId == source.ObjectId);
        var prompt = game.Pending;
        string before = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(offer.Id, [], [])));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        game.Resolve(PaidDecision.Take(offer, [payment.ObjectId]));
        Assert.Equal("12001c", hero.FaceId);
        Assert.False(hero.Ready);
        var response = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == moxie.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        Assert.False(hero.Ready);
        game.Resolve(Decision.Decline);
        Assert.True(hero.Ready);
        Assert.Equal(4, AntManFormResponseFixture.Value(world, 1, "attack"));
        Assert.Equal(0, world.Seats[1].FormChangedInRound);
    }

    [Theory]
    [InlineData("12001a", "12001c")]
    [InlineData("12001c", "12001a")]
    [Rule("rr:initiating-abilities.2")]
    public void IdentityAndHelmetResponsesResolveBeforeReady(string from, string to)
    {
        // Responses initiate "immediately after" their condition becomes true.
        var runner = Runner(responses: true);
        var (world, source, payment, _) = Board(runner, from);
        var hero = world.Seats[1].IdentityCard;
        hero.Exhaust();
        hero.TakeDamage(3);
        var helmet = world.CreateCard("12008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var game = Game.Begin(world, Cards, runner);
        for (int i = 0; i < 2; i++)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
                a => a.Verb == Game.ResolveMulligans).Id));
        var action = Assert.Single(game.PromptFor(1)!.Affordances, a => a.AnchorId == source.ObjectId);
        game.Resolve(PaidDecision.Take(action, [payment.ObjectId]));
        Assert.Equal(to, hero.FaceId);
        Assert.False(hero.Ready);
        var identity = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == hero.ObjectId);
        game.Resolve(Decision.Take(identity.Id, [to == "12001c" ? villain.ObjectId : scheme.ObjectId], []));
        Assert.False(hero.Ready);
        int deck = world.Seats[1].Deck.Cards.Count;
        var response = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == helmet.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        Assert.Equal(to == "12001c" ? 1 : 3, hero.Damage);
        Assert.Equal(to == "12001a" ? deck - 1 : deck, world.Seats[1].Deck.Cards.Count);
        Assert.Equal(to == "12001c" ? 1 : 0, villain.Damage);
        Assert.Equal(to == "12001a" ? 5 : 6, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.True(hero.Ready);
    }
    [Theory]
    [InlineData("12001a", true)]
    [InlineData("12001b", false)]
    public void HeroFormGatesTheEvent(string face, bool offered)
    {
        var runner = Runner();
        var (world, source, _, _) = Board(runner, face);
        var game = Game.Begin(world, Cards, runner);
        for (int i = 0; i < 2; i++)
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
                a => a.Verb == Game.ResolveMulligans).Id));
        Assert.Equal(offered, game.PromptFor(1)?.Affordances.Any(a => a.AnchorId == source.ObjectId) == true);
    }

    [Theory]
    [InlineData("12001a")]
    [InlineData("12001c")]
    [Rule("rr:cannot.1")]
    [Rule("rr:ability.4")]
    public void ProhibitedFormChangeStillAllowsIndependentReady(string face)
    {
        // rr:cannot.1 makes the prohibition absolute; rr:ability.4 resolves sentences one at a time.
        var runner = Runner();
        var (world, source, payment, _) = Board(runner, face);
        var hero = world.Seats[1].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            FormChangePermissions.ProhibitedField, 1, Card: hero.ObjectId, Affects: hero.ObjectId));
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        hero.Exhaust();
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(face, hero.FaceId);
        Assert.True(hero.Ready);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
    private static AbilityRunner Runner(bool responses = false)
    {
        var root = JsonNode.Parse($$"""
            {"cards":[{"card":"12020","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":{{Effect}}
            }]}]}
            """)!;
        if (responses)
            foreach (var row in JsonNode.Parse(AuthoredCards.Text)!["cards"]!.AsArray())
                if (row!["card"]!.GetValue<string>() is "12001a" or "12001c" or "12008" or "12016")
                    root["cards"]!.AsArray().Add(row.DeepClone());
        return new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
    }

    private static (World World, Card Source, Card Payment, Card Wasp) Board(AbilityRunner runner, string face)
    {
        var world = new World(Cards, 2, 241);
        var first = world.CreateSeat("p0");
        first.IdentityCard = world.CreateCard("01001a,01001b", first.Hero);
        var seat = world.CreateSeat("p1");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo(face);
        for (int i = 0; i < 20; i++)
        {
            world.CreateCard("01087", first.Deck);
            world.CreateCard("01087", seat.Deck);
            world.CreateCard("01120", world.AreaOf(DeckType.EncounterDeck));
        }
        var wasp = world.CreateCard("12002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 6);
        var source = world.CreateCard("12020", seat.Hand);
        var payment = world.CreateCard("01088", seat.Hand);
        world.Abilities = runner;
        return (world, source, payment, wasp);
    }
}






