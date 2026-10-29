using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic capability and host metadata; no production row is admitted.
public sealed class SharedTraitPermanentLegalityTests
{
    [Fact]
    public void LostSharedTraitRejectsThePublishedAnswerWithoutMutation()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{"playSharedHeroTraitCard":"game"}""", cost: """{"exhaust":"this"}""");
        Card? source = null, card = null;
        var (game, world) = Playing(board => {
            source = InPlay(board, AuthoredCards.AuntMay);
            card = board.CreateCard("03019", board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == card!.ObjectId);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Characteristics.Lost + Traits.Granted + "AVENGER", Affects: card!.ObjectId));
        world.RecordInformation(InformationKind.Search);
        var digest = world.Digest().Canonical();
        var agenda = world.Agenda.Outstanding.ToArray();
        var random = world.Random.Generator.GetState();
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(offer.Id)));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(InformationKind.Search, Assert.Single(new Resolution(world, prompt, []).Information).Kind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExhaustedSourceCannotAlsoGenerateItsOwnPayment(bool anotherGenerator)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[
              {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},"cost":{"exhaust":"this"},"effect":{"playSharedHeroTraitCard":"game"}},
              {"trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},"cost":{"exhaust":"this"},"effect":{"generate":"Y"}}]},
              {"card":"01030","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},"cost":{"exhaust":"this"},"effect":{"generate":"Y"}}]}]}
            """));
        Card? source = null;
        var (game, world) = Playing(board => {
            source = InPlay(board, "01006");
            board.CreateCard("03024", board.Seats[0].Hand);
            if (anotherGenerator) board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        foreach (var card in world.Seats[0].Hand.Cards.Where(c => c.FaceId != "03024").ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        // Rebuild the turn offer after establishing the isolated hand.
        var available = runner.Actions(world, 0);
        Assert.Equal(anotherGenerator, available.Any(a => a.Card == source!.ObjectId));
        Assert.True(source!.Ready);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HostPriceAndLiveHostAreValidated(bool stale)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},"cost":{"exhaust":"this"},"effect":{"playSharedHeroTraitCard":"game"}}]},
              {"card":"09039","abilities":[{"trigger":{"timing":"Constant","subject":"this"},"effect":{"reduceUpgradeCostOnThis":1}}]},
              {"card":"01093","attachTo":{"query":"allies"},"abilities":[]},
              {"card":"01067","abilities":[]}]}
            """));
        Card? source = null, iron = null, ordinary = null, upgrade = null, resource = null;
        var (game, world) = Playing(board => {
            source = InPlay(board, "01006");
            iron = board.CreateCard("09039", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            ordinary = board.CreateCard("01067", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            upgrade = board.CreateCard("01093", board.Seats[0].Hand);
            resource = board.CreateCard("01003", board.Seats[0].Hand);
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Traits.Granted + "AVENGER", Affects: upgrade.ObjectId));
        }, hero: true, abilities: runner);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var prompt = game.Pending!;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == upgrade!.ObjectId);
        Assert.Contains("reduction of 1", offer.Description);
        Assert.Equal("0", Assert.Single(offer.CostOptions, c => c.Target == iron!.ObjectId).Cost);
        Assert.Equal("1", Assert.Single(offer.CostOptions, c => c.Target == ordinary!.ObjectId).Cost);
        if (stale)
        {
            World.MoveToTop(iron!, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
            var digest = world.Digest().Canonical();
            var agenda = world.Agenda.Outstanding.ToArray();
            var random = world.Random.Generator.GetState();
            Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(offer.Id, [iron!.ObjectId], [])));
            Assert.Same(prompt, game.Pending);
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(agenda, world.Agenda.Outstanding);
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        }
        else
        {
            var cost = Assert.Single(offer.CostOptions, c => c.Target == ordinary!.ObjectId);
            var payment = new[] { resource!.ObjectId };
            game.Resolve(new Decision(offer.Id, [ordinary!.ObjectId], payment, Allocations: ResourcePayment.Allocate(cost, payment)));
            Assert.Equal(ordinary.ObjectId, upgrade!.Area.Host);
            Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        }
    }

    [Theory]
    [InlineData("Action", "{}", "{\"playSharedHeroTraitCard\":\"game\"}")]
    [InlineData("Action", "{\"seq\":[{\"exhaust\":\"this\"},{\"spend\":\"Y\"}]}", "{\"playSharedHeroTraitCard\":\"game\"}")]
    [InlineData("Resource", "{\"exhaust\":\"this\"}", "{\"playSharedHeroTraitCard\":\"game\"}")]
    [InlineData("Action", "{\"exhaust\":\"this\"}", "{\"seq\":[{\"draw\":{\"player\":\"you\",\"count\":1}},{\"playSharedHeroTraitCard\":\"game\"}]}")]
    public void UnsupportedSourceShapesFailLowering(string timing, string cost, string effect)
    {
        string json = "{\"cards\":[{\"card\":\"01006\",\"abilities\":[{\"trigger\":{\"timing\":\"" + timing
            + "\",\"subject\":\"game\"},\"cost\":" + cost + ",\"effect\":" + effect + "}]}]}";
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(json)));
    }
}
