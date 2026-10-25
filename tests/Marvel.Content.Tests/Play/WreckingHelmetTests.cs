using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingHelmetTests
{
    [Theory]
    [InlineData("ordinary", 4, 4)]
    [InlineData("tough", 4, 0)]
    [InlineData("defended", 1, 1)]
    [InlineData("ally", 4, 3)]
    [InlineData("stunned", 0, 0)]
    [InlineData("foreign", 0, 2)]
    [Rule("rr:damage.3.2")]
    [Rule("rr:attack-enemy-activation.step.6.a")]
    public void HelmetDiscardsDamageDealtFromTheAttackedPlayersDeck(
        string mode, int discarded, int damage)
    {
        // Damage prevented changes damage taken, while "the amount of damage dealt is not modified".
        // Helmet: "discard 1 card ... for each point of damage dealt by this attack."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07049", 3, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        for (int index = 0; index < 9; index++) world.CreateCard("01088", world.Seats[1].Deck);
        Reveal.Resolve(world, world.Facts, source, 0, []);
        Assert.Equal(groups[3].Villain.ObjectId, source.Area.Host);
        if (mode == "tough") Statuses.Give(world, world.Seats[1].IdentityCard, Statuses.Tough);
        Card? ally = null;
        if (mode == "ally")
        {
            ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
            ally.TakeDamage(1);
        }
        var attacker = groups[mode == "foreign" ? 1 : 3].Villain;
        if (mode == "stunned") Statuses.Give(world, attacker, Statuses.Stunned);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1,
            Subject: attacker.ObjectId, Seat: 1));
        Finish(world, runner, mode == "defended", ally);
        if (ally is not null) Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(discarded + (ally is null ? 0 : 1), world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)).Cards.Count);
        Assert.Equal(9 - discarded, world.Seats[1].Deck.Cards.Count);
        Assert.Empty(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)).Cards);
        Assert.Equal(damage, world.Seats[1].IdentityCard.Damage);
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Fact]
    [Rule("rr:forced.4")]
    public void HelmetDiscardsBeforeOptionalAttackEndResponses()
    {
        // "Forced responses take priority and initiate before non-forced responses."
        // Synthetic optional response on Helmet supplies an observable response boundary.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07049", 3);
        var document = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var row = document["cards"]!.AsArray().Single(card => card!["card"]!.GetValue<string>() == "07049")!;
        row["abilities"]!.AsArray().Add(JsonNode.Parse("""
        {"trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"attachedTo","player":"trigger.player"},
         "effect":{"giveStatus":{"card":"you","status":"tough"}}}
        """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(document.ToJsonString()));
        world.Abilities = runner;
        for (int index = 0; index < 8; index++) world.CreateCard("01088", world.Seats[0].Deck);
        Reveal.Resolve(world, world.Facts, source, 0, []);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1,
            Subject: groups[3].Villain.ObjectId, Seat: 0));
        bool offered = false;
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.True(step < 15, prompt.Label);
            if (prompt.Affordances.Any(offer => offer.AnchorId == source.ObjectId))
            {
                offered = true;
                Assert.Equal(4, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)).Cards.Count);
                Assert.Null(world.Attack);
            }
            Assert.True(prompt.Cancellable, prompt.Label);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
        Assert.True(offered);
    }

    private static void Finish(World world, AbilityRunner runner, bool defend, Card? ally)
    {
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) return;
            Assert.True(step < 15, prompt.Label);
            var answer = prompt.Asking == Question.Defender && (defend || ally is not null)
                && prompt.Player == 1 ? Decision.Take(ally?.ObjectId ?? world.Seats[1].IdentityCard.ObjectId) : Decision.Decline;
            if (answer == Decision.Decline) Assert.True(prompt.Cancellable, prompt.Label);
            Sequence.Answer(world, world.Facts, runner, prompt, answer, []);
        }
    }
}

