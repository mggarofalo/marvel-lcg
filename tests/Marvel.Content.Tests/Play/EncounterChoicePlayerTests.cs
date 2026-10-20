using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EncounterChoicePlayerTests : ChoosingCardsTestBase
{
    [Rule("rr:first-player.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChoosingSeatDoesNotRebindTheResolvingPlayersYou(bool firstPlayerChooses)
    {
        // rr:first-player.1: "the first player selects among the eligible options."
        // Synthetic public-target choice separates that player from the effect's "you".
        Assert.Equal(Resolve(firstPlayerChooses, probe: false), Resolve(firstPlayerChooses, probe: true));
    }

    private static (string Digest, long Words) Resolve(bool firstPlayerChooses, bool probe)
    {
        World world = Deal("spider_man", "captain_marvel");
        var runner = Runner(firstPlayerChooses ? "\"chooser\":\"firstPlayer\"," : "");
        var source = world.CreateCard("01110", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 1);
        var events = new List<Marvel.Rules.Events.GameEvent>();
        var activePrompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        var waiting = AbilityContinuationFixtures.Pending(world, source);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Outstanding.ToArray());
        Prompt prompt = runner.Choosing(world, waiting)!;
        Assert.Equal(firstPlayerChooses ? world.FirstPlayer : 1, prompt.Player);
        Assert.Contains(source.ObjectId, prompt.ContextCardIds);
        Assert.Equal(2, prompt.Affordances.Count);
        if (probe)
        {
            var repeated = runner.Choosing(world, waiting)!;
            Assert.Equal(prompt.Player, repeated.Player);
            Assert.Equal(prompt.Affordances.Select(option => option.Id), repeated.Affordances.Select(option => option.Id));
        }
        Assert.Equal(before.Item1, world.Digest().Canonical());
        Assert.Equal(before.Item2, world.Random.Generator.WordsConsumed);
        Assert.Equal(before.Item3, world.Agenda.Outstanding);

        Sequence.Answer(world, world.Facts, runner, activePrompt,
            Decision.Take(world.Seats[0].IdentityCard.ObjectId), events);
        Sequence.Finish(world, world.Facts, runner, []);

        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        return (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("\"chosenPlayer\"")]
    public void UnsupportedChooserFailsLowering(string chooser)
    {
        Assert.Throws<AbilityException>(() => Runner("\"chooser\":" + chooser + ","));
    }

    private static AbilityRunner Runner(string chooser) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"01110","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"chooseCard":{CHOOSER"from":{"query":"identities"},
            "effect":{"seq":[
              {"giveStatus":{"card":"chosen","status":"tough"}},
              {"dealDamage":{"cards":"you","amount":1}}
            ]}}}
        }]}]}
        """.Replace("CHOOSER", chooser, StringComparison.Ordinal)));
}
