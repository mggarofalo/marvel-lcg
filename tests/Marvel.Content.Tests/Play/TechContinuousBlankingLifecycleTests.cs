using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class TechContinuousBlankingLifecycleTests : CoreCardTestBase
{
    private static readonly int[] Players = [0, 1];
    [Fact]
    [Rule("rr:uses-x-type.1")]
    [Rule("rr:play-put-into-play")]
    public void PaidWebShooterEntersBlankedWithoutUsesAndLeavesWhenRestored()
    {
        // Playing pays its cost and places the card in play; a Uses card with
        // no all-purpose counters is discarded when its text resumes.
        var world = Hero("01001a");
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12026","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"blankTechPlayerCards":"game"}}]},
              {"card":"01008","startingCounters":{"type":"web","count":3,"uses":true},"abilities":[]}
            ]}
            """));
        world.Abilities = runner;
        var scheme = world.CreateCard("12026", world.AreaOf(DeckType.SideSchemesArea));
        var shooter = world.CreateCard("01008", world.Seats[0].Hand);
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Deck);
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], shooter, [payment.ObjectId], events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, shooter.Area.Type);
        Assert.True(PrintedText.IsBlanked(world, shooter));
        Assert.Equal(0, shooter.Tokens.GetValueOrDefault("c_web"));
        Discard.Card(world, scheme, "test", events);
        Assert.Equal(DeckType.DiscardPile, shooter.Area.Type);
        Assert.Equal(0, shooter.Tokens.GetValueOrDefault("c_web"));
    }

    [Fact]
    [Rule("rr:ally-limit")]
    [Rule("rr:traits.2")]
    public void TraitGainRechecksNonFirstPlayersAllyLimitAndChoice()
    {
        // A trait remains while the printed ally-limit grant becomes blank.
        var world = Hero("01001a", players: 2);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12026","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"blankTechPlayerCards":"game"}}]},
              {"card":"01002","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"grant":{"card":"you","keyword":"ally_limit","amount":1}}}]}
            ]}
            """));
        world.Abilities = runner;
        var scheme = world.CreateCard("12026", world.AreaOf(DeckType.SideSchemesArea));
        var allies = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1);
        var grantor = world.CreateCard("01002", allies);
        var chosen = world.CreateCard("01011", allies);
        world.CreateCard("01020", allies);
        world.CreateCard("01030", allies);
        world.CreateCard("01089", world.Seats[1].Deck);
        Assert.Equal(4, StateFields.Modified(world, world.Seats[1].IdentityCard, "ally_limit", world.Facts, 2));
        using var trait = world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Traits.Granted + "TECH", Card: scheme.ObjectId, Affects: grantor.ObjectId));
        Assert.True(PrintedText.IsBlanked(world, grantor));
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        Assert.NotNull(prompt);
        Assert.Equal(Steps.ChooseAllyForLimit, prompt.Trigger);
        Assert.Equal(1, prompt.Player);
        Assert.Contains(prompt.Affordances, option => option.Id == chosen.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(chosen.ObjectId), events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.DiscardPile, chosen.Area.Type);
        Assert.Equal(3, allies.Cards.Count);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    public void RevealedSourceRequiresEachAffectedPlayerToChoose()
    {
        // Each player over their limit chooses their own ally to discard.
        var world = Hero("01001a", players: 2);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12026","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"blankTechPlayerCards":"game"}}]},
              {"card":"01008","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"grant":{"card":"you","keyword":"ally_limit","amount":1}}}]}
            ]}
            """));
        world.Abilities = runner;
        var chosen = new Card[2];
        for (int player = 0; player < 2; player++)
        {
            world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(player), cardOwner: player));
            var allies = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player);
            chosen[player] = world.CreateCard("01002", allies);
            foreach (string id in new[] { "01011", "01020", "01030" }) world.CreateCard(id, allies);
            world.CreateCard("01089", world.Seats[player].Deck);
        }
        var scheme = world.CreateCard("12026", world.AreaOf(DeckType.RevealingArea));
        var events = new List<GameEvent>();
        Reveal.Resolve(world, world.Facts, scheme, 0, events);
        var answered = new HashSet<int>();
        for (int i = 0; i < 2; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            Assert.NotNull(prompt);
            Assert.Equal(Steps.ChooseAllyForLimit, prompt.Trigger);
            Assert.True(answered.Add(prompt.Player));
            var ally = chosen[prompt.Player];
            Assert.Contains(prompt.Affordances, option => option.Id == ally.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(ally.ObjectId), events);
            Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        }
        Assert.Equal(Players, answered.Order().ToArray());
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:hit-points.3.1")]
    public void MissingOccurrenceRefusesLethalUnblankBeforeTraitOrSourceLeaves(bool removeTrait)
    {
        // The synthetic negative modifier resumes when text is restored. Defeat
        // needs an enclosing occurrence; refusal preserves the preflight state.
        var world = Hero("01001a");
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12026","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"blankTechPlayerCards":"game"}}]},
              {"card":"01002","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"grant":{"card":"this","keyword":"health","amount":-2}}}]}
            ]}
            """));
        world.Abilities = runner;
        var scheme = world.CreateCard("12026", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var trait = world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Traits.Granted + "TECH", Card: scheme.ObjectId, Affects: ally.ObjectId));
        ally.TakeDamage(1);
        string before = world.Digest().Canonical();
        var error = Assert.Throws<RulesNotImplementedException>(() =>
        {
            if (removeTrait) trait.Dispose();
            else Discard.Card(world, scheme, "test", []);
        });
        Assert.Contains("enclosing occurrence", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.True(PrintedText.IsBlanked(world, ally));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
    }

}
