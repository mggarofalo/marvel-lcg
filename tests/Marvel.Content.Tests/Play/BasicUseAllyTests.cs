using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BasicUseAllyTests
{
    // Synthetic runner for printed 06032; no product admission or ability-book change.
    private static AbilityRunner Runner(string face = "06032", string effect = "{\"boostBasicUse\":\"exhaustedAllyPower\"}") =>
        new(AbilityCatalog.Parse($$$"""
        {"cards":[{"card":"{{{face}}}","abilities":[{
          "trigger":{"event":"WhenHeroBasicUseInitiated","timing":"Interrupt","actor":"you","form":"hero","player":"you"},
          "cost":{"exhaustChosen":{"from":{"query":"alliesYouControl"},"count":1,"matchingBasicPower":"true"}},
          "effect":{{{effect}}}
        }]}]}
        """));

    private static Card Ally(World world, int player = 0) => world.CreateCard("01002",
        world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));

    private static void Modify(World world, Card card, string field, long amount) =>
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: field, Amount: amount, Card: card.ObjectId, Affects: card.ObjectId));

    private static void Begin(World world, bool thwart)
    {
        if (thwart)
        {
            var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
            scheme.PlaceTokens("k_threat", 10);
            BasicThwartPowers.BasicThwart(world, world.Facts, 0, scheme, []);
        }
        else BasicPowerInitiation.BasicAttack(world, world.Facts, 0, world.TheCardIn(DeckType.VillainArea)!, []);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [Rule("rr:modifiers.4")]
    [Rule("rr:cost.7")]
    public void SelectedAllyAddsItsMatchingModifiedPowerOnlyToThisUse(bool thwart, int index)
    {
        // "After all active modifiers ... below zero ... treated as zero";
        // "must pay costs with cards and/or game elements they control".
        var runner = Runner();
        Card[] allies = [];
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("06032", board.Seats[0].Hand);
            allies = [Ally(board), Ally(board)];
            Modify(board, allies[0], "attack", 2);
            Modify(board, allies[0], "thwart", 1);
            Modify(board, allies[1], "attack", 4);
            Modify(board, allies[1], "thwart", 3);
        }, hero: true, abilities: runner);
        string field = thwart ? "thwart" : "attack";
        long hero = StateFields.Modified(world, world.Seats[0].IdentityCard, field, world.Facts, world.Players);
        long bonus = StateFields.Modified(world, allies[index], field, world.Facts, world.Players);
        Begin(world, thwart);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(allies.Select(card => card.ObjectId), offer.Targets!.Legal);
        Assert.Contains($"{bonus} {(thwart ? "THW" : "ATK")}", offer.Targets.Details![allies[index].ObjectId]);
        Sequence.Answer(world, world.Facts, runner, prompt, new(offer.Id, [allies[index].ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(allies[index].Ready);
        Assert.True(allies[1 - index].Ready);
        Assert.All(allies, ally => Assert.Equal(0, ally.Damage));
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        if (thwart) Assert.Equal(10 - hero - bonus, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        else Assert.Equal(hero + bonus, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(hero, StateFields.Modified(world, world.Seats[0].IdentityCard, field, world.Facts, world.Players));
        world.Seats[0].IdentityCard.Refresh();
        Begin(world, thwart);
        Sequence.Finish(world, world.Facts, runner, []);
        if (thwart) Assert.Equal(20 - hero * 2 - bonus, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        else Assert.Equal(hero * 2 + bonus, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Theory]
    [InlineData("exhausted")]
    [InlineData("foreign")]
    [InlineData("zero")]
    [InlineData("negative")]
    public void IneligibleAllyDoesNotOfferAnIneffectiveEvent(string reason)
    {
        var runner = Runner();
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("06032", board.Seats[0].Hand);
            var ally = Ally(board, reason == "foreign" ? 1 : 0);
            if (reason == "exhausted") ally.Exhaust();
            if (reason is "zero" or "negative") Modify(board, ally, "attack", reason == "zero" ? -2 : -9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Begin(world, false);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
    }

    [Fact]
    public void InvalidAndStaleSelectionsPreserveCostAndCanRetryAnotherAlly()
    {
        var runner = Runner();
        Card[] allies = [];
        Card? foreign = null;
        var (_, world) = Playing(board =>
        {
            board.CreateCard("06032", board.Seats[0].Hand);
            allies = [Ally(board), Ally(board)];
            foreign = Ally(board, 1);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Begin(world, false);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(prompt.Affordances);
        allies[0].Exhaust();
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        foreach (int[] ids in new int[][] { [], [foreign!.ObjectId], [allies[0].ObjectId], [allies[1].ObjectId, allies[1].ObjectId] })
        {
            Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, world.Facts, runner, prompt, new(offer.Id, ids), []));
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
            Assert.Equal(0, world.Agenda.Occurrence!.BasicUse!.Bonus);
        }
        Sequence.Answer(world, world.Facts, runner, prompt, new(offer.Id, [allies[1].ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(allies[1].Ready);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SyntheticDepartedPerformerCannotDonateItsUseToAReplacement(bool thwart)
    {
        var runner = Runner();
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("06032", board.Seats[0].Hand);
            Ally(board);
        }, hero: true, abilities: runner);
        Begin(world, thwart);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var hero = world.Seats[0].IdentityCard;
        var original = hero.Area;
        World.MoveToTop(hero, world.Seats[0].Hand);
        World.MoveToTop(hero, original);
        Assert.Empty(runner.Waiting(world, world.Agenda.Occurrence!, WindowKind.Interrupt));
        Sequence.Answer(world, world.Facts, runner, prompt, new Decision(-1, []), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.HandsArea, source!.Area.Type);
        if (thwart) Assert.Equal(10, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        else Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }
}



