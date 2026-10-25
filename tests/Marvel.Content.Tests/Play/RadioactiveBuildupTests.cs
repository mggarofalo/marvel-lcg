using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RadioactiveBuildupTests
{
    [Fact]
    [Rule("rr:response")]
    public void ThreatResponsesFinishBeforeAttackEndDiscardsTheAttachment()
    {
        // "A response ability is executed after its triggering condition occurs."
        // A synthetic optional observer on the identity exposes the nested threat window.
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var row = book["cards"]!.AsArray().Single(item => item!["card"]!.GetValue<string>() == "01001a")!;
        row["abilities"]!.AsArray().Add(JsonNode.Parse("""
            {"trigger":{"event":"WhenThreatPlaced","timing":"Response","subject":"game"},
             "effect":{"draw":{"player":"you","count":1}}}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07022", 1);
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, 0, []);
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        world.EncounterTopology!.SelectActive(groups[0].Villain, "test", []);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1, Subject: groups[1].Villain.ObjectId, Seat: 0));
        bool observed = false;
        List<GameEvent> events = [];
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            Assert.True(step < 25, prompt.Label);
            if (groups[1].Scheme.Tokens.GetValueOrDefault("k_threat") > 0 && !observed)
            {
                Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
                Assert.True(prompt.Cancellable, prompt.Label);
                observed = true;
            }
            bool defend = prompt.Asking == Question.Defender;
            Sequence.Answer(world, world.Facts, runner, prompt,
                defend ? Decision.Take(ally.ObjectId)
                    : prompt.Cancellable ? Decision.Decline
                    : Decision.Take(prompt.Affordances.First(offer => offer.IsLegal).Id), events);
        }
        Assert.True(observed);
        // Thunderball 2 + attachment 3 - Mockingbird DEF 1 - remaining HP 1 = 3.
        Assert.Equal(3, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Contains(source, groups[1].Discard.Cards);
        Assert.Equal(0, groups[0].Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void RoutingRequiresTheAttachedLiveSourceAndItsOwnDamage(bool depart, bool foreign)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07022", 1);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, 0, []);
        if (depart) World.MoveToTop(source, groups[1].Discard);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        var thunderball = groups[1].Villain;
        DamageAttacks.Resolve(world, world.Facts,
            new(thunderball, foreign ? groups[0].Villain : thunderball, ally, 6, "test", "Attack", Retaliate: false), []);
        WreckingSchemeFixture.Finish(world, runner);
        Assert.All(groups, group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
    }
}
