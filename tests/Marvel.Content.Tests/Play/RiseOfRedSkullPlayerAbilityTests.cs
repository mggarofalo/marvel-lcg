using Marvel.Content.Tests.Cards;
using Marvel.Content.Setup;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RiseOfRedSkullPlayerAbilityTests
{
    [Theory]
    [InlineData("04159a", 2, "thwart", true)]
    [InlineData("04159b", 2, "thwart", true)]
    [InlineData("04160a", 1, "attack", true)]
    [InlineData("04160b", 1, "attack", true)]
    [InlineData("04161a", 3, "defense", true)]
    [InlineData("04161b", 3, "defense", true)]
    [InlineData("04162a", 4, "recover", false)]
    [InlineData("04162b", 4, "recover", false)]
    public void CampaignConditionHealthAppliesAcrossFormsWhilePowerMatchesPrintedForm(
        string face, int health, string power, bool heroPower)
    {
        var (_, world) = Playing(_ => { }, abilities: AuthoredCards.Runner());
        var identity = world.Seats[0].IdentityCard;
        long initialHealth = StateFields.Modified(world, identity, "health", world.Facts, 1);
        InPlay(world, face);
        for (int form = 0; form < 2; form++)
        {
            bool hero = form == 1;
            if (hero) Forms.Change(world.Seats[0], world.Facts);
            Assert.Equal(initialHealth + health, StateFields.Modified(world, identity, "health", world.Facts, 1));
            long printed = world.Facts.PrintedValue(identity.FaceId, heroPower ? power == "attack" ? "ATK" : power == "thwart" ? "THW" : "DEF" : "REC", 1);
            Assert.Equal(printed + (hero == heroPower ? 1 : 0), StateFields.Modified(world, identity, power, world.Facts, 1));
        }
    }

    [Theory]
    [InlineData("Attack")]
    [InlineData("Thwart")]
    [Rule("rr:basic-power.1.1")]
    [Rule("rr:basic-power.1.2")]
    public void CaptainMarvelDrawsAfterUsingEitherPrintedBasicPower(string power)
    {
        // 04032: "After Captain Marvel uses a basic power, draw 1 card."
        var runner = AuthoredCards.Runner();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var ally = world.CreateCard("04032", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var target = world.TheCardIn(power == "Attack" ? DeckType.VillainArea : DeckType.MainSchemesArea)!;
        if (power == "Thwart") target.PlaceTokens("k_threat", 3);
        int hand = world.Seats[0].Hand.Cards.Count;

        AllyBasicPowers.AllyPower(world, world.Facts, ally, target, power, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var response = Assert.Single(prompt.Affordances, offer => offer.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);

        Assert.Equal(hand + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(1, ally.Damage);
        Assert.False(ally.Ready);
    }

    [Fact]
    [Rule("rr:limit.1")]
    public void SpiderWomanTracksEachAspectSeparatelyAndRejectsBasicCards()
    {
        // 04031a: "limit once per round for each aspect."
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("04031a", seat.Hero);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var hero = world.Seats[0].IdentityCard;
        int occurrenceId = 1;
        foreach (string face in new[] { "04035", "04036", "04037", "04038" })
        {
            var played = world.CreateCard(face, world.Seats[0].Hand);
            var occurrence = new Occurrence(occurrenceId++, [Steps.CardPlayed], played.ObjectId, Player: 0);
            var pending = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));
            runner.ResolveWindow(world, occurrence, pending, new Decision(pending.Card, [], []));
            var repeated = new Occurrence(occurrenceId++, [Steps.CardPlayed], played.ObjectId, Player: 0);
            Assert.Empty(runner.Waiting(world, repeated, WindowKind.Interrupt));
        }
        var basic = world.CreateCard("01091", world.Seats[0].Hand);
        Assert.Empty(runner.Waiting(world, new Occurrence(occurrenceId, [Steps.CardPlayed], basic.ObjectId, Player: 0), WindowKind.Interrupt));
        Assert.Equal(5, StateFields.Modified(world, hero, "attack", world.Facts, 1));
        Assert.Equal(5, StateFields.Modified(world, hero, "thwart", world.Facts, 1));
        Assert.Equal(5, StateFields.Modified(world, hero, "defense", world.Facts, 1));
        world.Effects.Expire(TimingPoints.EndOfRound, []);
        Assert.Equal(1, StateFields.Modified(world, hero, "attack", world.Facts, 1));
    }
}
