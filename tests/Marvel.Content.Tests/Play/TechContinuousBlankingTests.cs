using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class TechContinuousBlankingTests : CoreCardTestBase
{
    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"12026","abilities":[{
          "trigger":{"timing":"Constant","subject":"this"},
          "effect":{"blankTechPlayerCards":"game"}
        }]}]}
        """));

    [Fact]
    [Rule("rr:ability.8.2")]
    [Rule("rr:traits.2")]
    public void SyntheticConstantTracksLivePlayerCardAndTraitMembership()
    {
        // A constant "becomes active as soon as its card enters play";
        // traits are not part of a printed text box for blanking.
        var world = Hero("01001a", players: 2);
        world.Abilities = Runner();
        var source = world.CreateCard("12026", world.AreaOf(DeckType.SideSchemesArea));
        var tech = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var otherTech = world.CreateCard("01035", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var ordinary = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var hand = world.CreateCard("01035", world.Seats[0].Hand);
        var encounter = world.CreateCard("01141", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.True(PrintedText.IsBlanked(world, tech));
        Assert.True(PrintedText.IsBlanked(world, otherTech));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.True(Characteristics.IsLost(world, tech, "uses"));
        Assert.True(Traits.Has(world, tech, "TECH", world.Facts));
        Assert.False(PrintedText.IsBlanked(world, ordinary));
        Assert.False(PrintedText.IsBlanked(world, hand));
        Assert.False(PrintedText.IsBlanked(world, encounter));
        using (world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Traits.Granted + "TECH", Card: source.ObjectId, Affects: ordinary.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound))))
            Assert.True(PrintedText.IsBlanked(world, ordinary));
        Assert.False(PrintedText.IsBlanked(world, ordinary));
        World.MoveToTop(source, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.False(PrintedText.IsBlanked(world, tech));
        Assert.False(Characteristics.IsLost(world, tech, "uses"));
        World.MoveToTop(source, world.AreaOf(DeckType.SideSchemesArea));
        Assert.True(PrintedText.IsBlanked(world, tech));
    }

    [Fact]
    [Rule("rr:hit-points.3.1")]
    public void ProjectingAHealthDefeatWithASyntheticTitledGrantRefusesBeforeReveal()
    {
        // A titled grant whose only target would leave cannot yet be projected
        // through the transient fixed-point pass. The board remains unchanged.
        var world = Hero("01001a");
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12026","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"blankTechPlayerCards":"game"}}]},
              {"card":"01008","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"grant":{"card":{"titled":"Black Cat"},"keyword":"health","amount":2}}}]}
            ]}
            """));
        world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        var scheme = world.CreateCard("12026", world.AreaOf(DeckType.RevealingArea));
        BeginOccurrence(world, scheme);
        string before = world.Digest().Canonical();
        var error = Assert.Throws<RulesNotImplementedException>(() => Reveal.Resolve(world, world.Facts, scheme, 0, []));
        Assert.Contains("would grant to a card that is not there", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(DeckType.RevealingArea, scheme.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:hit-points.3.1")]
    public void RevealPreflightsHealthLostWhenTheTechSourceIsBlanked(bool blocked)
    {
        // A character whose damage reaches its reduced hit points is defeated.
        // The health grant is synthetic; the source enters by normal reveal.
        var world = Hero("01001a");
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12026","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"blankTechPlayerCards":"game"}}]},
              {"card":"01008","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"grantEach":{"cards":{"query":"allies"},"keyword":"health","amount":2}}}]}
            ]}
            """));
        var tech = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        if (blocked) world.CreateCard("27189a", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId));
        ally.TakeDamage(2);
        var scheme = world.CreateCard("12026", world.AreaOf(DeckType.RevealingArea));
        BeginOccurrence(world, scheme);
        world.CreateCard("01089", world.Seats[0].Deck);
        Assert.Equal(4, DamagePlacement.Health(world, world.Facts, ally));
        string before = world.Digest().Canonical();
        if (blocked)
        {
            Assert.Throws<RulesNotImplementedException>(() => Reveal.Resolve(world, world.Facts, scheme, 0, []));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.False(PrintedText.IsBlanked(world, tech));
            Assert.Equal(DeckType.RevealingArea, scheme.Area.Type);
        }
        else
        {
            Reveal.Resolve(world, world.Facts, scheme, 0, []);
            Assert.True(PrintedText.IsBlanked(world, tech));
            Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
            Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:traits.2")]
    public void TraitGainPreflightsLossOfAConstantHealthGrant(bool blocked)
    {
        // Traits remain independently granted while blanking removes printed abilities.
        var world = Hero("01001a");
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12026","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"blankTechPlayerCards":"game"}}]},
              {"card":"01002","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"grant":{"card":"this","keyword":"health","amount":2}}}]}
            ]}
            """));
        var scheme = world.CreateCard("12026", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        if (blocked) world.CreateCard("27189a", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId));
        ally.TakeDamage(2);
        Assert.Equal(4, DamagePlacement.Health(world, world.Facts, ally));
        BeginOccurrence(world, scheme);
        world.CreateCard("01089", world.Seats[0].Deck);
        var grant = new ContinuousEffect(EffectSource.LastingEffect, Traits.Granted + "TECH",
            Card: scheme.ObjectId, Affects: ally.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound));
        string before = world.Digest().Canonical();
        if (blocked)
        {
            Assert.Throws<RulesNotImplementedException>(() => world.Effects.Register(grant));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.DoesNotContain(grant, world.Effects.Registered);
            Assert.False(PrintedText.IsBlanked(world, ally));
        }
        else
        {
            world.Effects.Register(grant);
            Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:hit-points.3.1")]
    public void SourceDeparturePreflightsIndirectHealthLoss(bool blocked)
    {
        // A synthetic negative constant resumes when the blanking source leaves.
        // A character whose damage reaches its reduced hit points is defeated.
        var world = Hero("01001a");
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12026","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"blankTechPlayerCards":"game"}}]},
              {"card":"01008","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"grantEach":{"cards":{"query":"allies"},"keyword":"health","amount":-2}}}]}
            ]}
            """));
        var scheme = world.CreateCard("12026", world.AreaOf(DeckType.SideSchemesArea));
        world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        if (blocked) world.CreateCard("27189a", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), ally.ObjectId));
        ally.TakeDamage(1);
        world.CreateCard("01089", world.Seats[0].Deck);
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        BeginOccurrence(world, scheme);
        string before = world.Digest().Canonical();
        if (blocked)
        {
            Assert.Throws<RulesNotImplementedException>(() => Discard.Card(world, scheme, "test", []));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        }
        else
        {
            Discard.Card(world, scheme, "test", []);
            Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
            Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        }
    }

    [Fact]
    [Rule("rr:uses-x-type.1")]
    public void EnteringBlankedDoesNotPlaceCountersAndRestoringUsesDiscardsIt()
    {
        // "If there are no all-purpose counters on this card, discard this card."
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
        var tech = world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01089", world.Seats[0].Deck);
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        Reveal.EnterPlay(world, world.Facts, tech, [], abilities: runner);
        Assert.Equal(0, tech.Tokens.GetValueOrDefault("c_web"));
        Discard.Card(world, scheme, "test", []);
        Assert.Equal(DeckType.DiscardPile, tech.Area.Type);
        Assert.Equal(0, tech.Tokens.GetValueOrDefault("c_web"));
    }

    [Fact]
    [Rule("rr:hit-points.2.3")]
    public void ActualMarkVArmorLossFromRevealEliminatesTheDamagedHero()
    {
        // A hero whose damage reaches its reduced hit points is defeated.
        // The armor uses its canonical +6 health constant; the scheme is synthetic.
        var world = Hero("01029a");
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12026","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"blankTechPlayerCards":"game"}}]},
              {"card":"01036","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"grant":{"card":"you","keyword":"health","amount":6}}}]}
            ]}
            """));
        var identity = world.Seats[0].IdentityCard;
        world.CreateCard("01036", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        identity.TakeDamage(9);
        Assert.Equal(15, DamagePlacement.Health(world, world.Facts, identity));
        var scheme = world.CreateCard("12026", world.AreaOf(DeckType.RevealingArea));
        BeginOccurrence(world, scheme);
        Reveal.Resolve(world, world.Facts, scheme, 0, []);
        Assert.True(world.Seats[0].Eliminated);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    public void RevealedBlankingSourceRechecksTheAffectedPlayersAllyLimit()
    {
        // A player who exceeds their ally limit chooses allies to discard.
        var world = Hero("01001a");
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"12026","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"blankTechPlayerCards":"game"}}]},
              {"card":"01008","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
                "effect":{"grant":{"card":"you","keyword":"ally_limit","amount":1}}}]}
            ]}
            """));
        world.CreateCard("01008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        foreach (string id in new[] { "01002", "01011", "01020", "01030" })
            world.CreateCard(id, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Assert.Equal(4, StateFields.Modified(world, world.Seats[0].IdentityCard, "ally_limit", world.Facts, 1));
        var scheme = world.CreateCard("12026", world.AreaOf(DeckType.RevealingArea));
        Reveal.Resolve(world, world.Facts, scheme, 0, []);
        Assert.Equal(3, StateFields.Modified(world, world.Seats[0].IdentityCard, "ally_limit", world.Facts, 1));
        Assert.Contains(world.Agenda.Outstanding, step => step.What == Steps.ChooseAllyForLimit && step.Seat == 0);
    }

    private static void BeginOccurrence(World world, Card source)
    {
        world.Agenda.Add(new PhaseStep(new LifecycleOperation(LifecycleKind.CardEntersPlay),
            Round: 1, Number: 0, Subject: source.ObjectId, Seat: 0));
        world.Agenda.Begin(world, world.Facts);
    }

    [Theory]
    [InlineData("Action", "{\"blankTechPlayerCards\":\"game\"}")]
    [InlineData("Constant", "{\"seq\":[{\"blankTechPlayerCards\":\"game\"}]}")]
    public void SyntheticConstantRejectsUnreviewedAdmission(string timing, string effect)
    {
        string json = $$"""
            {"cards":[{"card":"12026","abilities":[{
              "trigger":{"timing":"{{timing}}","subject":"this"}, "effect":{{effect}}
            }]}]}
            """;
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(json)));
    }
}
