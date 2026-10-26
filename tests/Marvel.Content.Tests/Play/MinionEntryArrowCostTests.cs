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

/// <summary>Synthetic event bodies isolate the uncertain arrow-cost prerequisite.</summary>
public sealed class MinionEntryArrowCostTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    [Rule("rr:cost-arrow-icon")]
    public void ExactPhysicalMatchEntersBeforeTheEffectEvenAfterReset(bool last)
    {
        // "Do not continue the discard effect with the newly shuffled encounter deck."
        var world = Hero("06001a");
        var runner = Runner();
        for (int i = 0; i < 4; i++) world.CreateCard("01088", world.Seats[0].Deck);
        var scheme = world.CreateCard("01107", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 6);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        if (!last) world.CreateCard("01100", deck);
        var minion = world.CreateCard("01101", deck);
        world.CreateCard("01100", deck);
        var source = world.CreateCard("06003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0));
        var events = new List<GameEvent>(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        Assert.Equal(6, scheme.Tokens.GetValueOrDefault("k_threat"));
        Finish(world, runner, events);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(PlayArea.Of(0), minion.Area.PlayArea);
        Assert.Equal(3, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(last ? 1 : 0, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("ruling:6e301a347a5392fb")]
    [Rule("rr:cost-arrow-icon")]
    [Rule("rr:encounter-deck.2")]
    public void NoMatchFinishesTheEventWithoutItsEffectOrRemovingConfused()
    {
        // Text before the arrow "must be paid and/or resolved in full".
        // Current Defender ruling requires an actual minion and engagement.
        var world = Hero("06001a");
        var runner = Runner();
        for (int i = 0; i < 4; i++) world.CreateCard("01088", world.Seats[0].Deck);
        var scheme = world.CreateCard("01107", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 6);
        world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        var source = world.CreateCard("06003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0));
        var events = new List<GameEvent>(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        Finish(world, runner, events);
        Assert.Equal(6, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(1, scheme.Tokens[EncounterDeck.AccelerationToken]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:quickstrike.1")]
    [Rule("rr:ability.step.4")]
    public void QuickstrikeAndThorResponseFinishBeforeThePostArrowThreatRemoval()
    {
        // Quickstrike is a "Forced Response (Hero)" before an optional "Response".
        var world = Hero("06001a");
        var runner = Runner();
        for (int i = 0; i < 4; i++) world.CreateCard("01088", world.Seats[0].Deck);
        var scheme = world.CreateCard("01107", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 6);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 4; i++) world.CreateCard("01100", deck);
        var minion = world.CreateCard("01167", deck);
        world.CreateCard("06003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0));
        var events = new List<GameEvent>(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        int defenses = 0, responses = 0;
        Finish(world, runner, events, prompt =>
        {
            Assert.Equal(6, scheme.Tokens.GetValueOrDefault("k_threat"));
            string before = world.Digest().Canonical();
            var rng = world.Random.Generator.GetState();
            var outstanding = world.Agenda.Outstanding.ToArray();
            Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, runner,
                prompt, Decision.Take(int.MaxValue), events));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(outstanding, world.Agenda.Outstanding);
            if (prompt.Asking == Question.Defender) defenses++;
            var response = prompt.Affordances.SingleOrDefault(a => a.Timing == AbilityType.Response);
            if (response is null) return Decision.Decline;
            responses++;
            Assert.Equal(1, defenses);
            Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
            return Decision.Take(response.Id);
        });
        Assert.Equal(1, responses);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(3, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(6, true)]
    [Rule("rr:cost.6")]
    public void NoLegalThreatRemovalPreventsPaymentWithoutInspectingTheDeck(int threat, bool offered)
    {
        // A cost cannot be paid when "there is not at least one valid target".
        var world = Hero("06001a");
        var runner = Runner();
        var scheme = world.CreateCard("01107", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", threat);
        world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("06003", world.Seats[0].Hand);
        string before = world.Digest().Canonical();
        Assert.Equal(offered, runner.Actions(world, 0).Count > 0);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:unique-icon")]
    [Rule("ruling:6e301a347a5392fb")]
    public void AFoundMinionThatCannotEnterLeavesTheArrowCostUnpaid()
    {
        // Unique prevents a second physical Vulture entering play.
        var world = Hero("06001a");
        var runner = Runner();
        for (int i = 0; i < 4; i++) world.CreateCard("01088", world.Seats[0].Deck);
        var scheme = world.CreateCard("01107", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 6);
        world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
        var found = world.CreateCard("01167", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("06003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0));
        var events = new List<GameEvent>(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        Finish(world, runner, events);
        Assert.Equal(6, scheme.Tokens["k_threat"]);
        Assert.False(DeckTypes.IsInPlay(found.Area.Type));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("ruling:a1f4982cfebcd768")]
    public void PatrolAcquiredDuringPaymentPreventsEffectInitiation()
    {
        // The April 2026 ruling: Patrol engaging at step 5 can prevent step 6.
        var world = Hero("06001a");
        var runner = Runner();
        for (int i = 0; i < 4; i++) world.CreateCard("01088", world.Seats[0].Deck);
        var scheme = world.CreateCard("01107", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 6);
        world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
        var patrol = world.CreateCard("16119", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("06003", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0));
        var events = new List<GameEvent>(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        Finish(world, runner, events);
        Assert.Equal(DeckType.EngagedEnemiesArea, patrol.Area.Type);
        Assert.Equal(6, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse("""
        {"cards":[
          {"card":"06003","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
            "cost":{"discardUntilMinionEngaged":"you"},
            "labels":["thwart"],
            "effect":{"thwart":{"target":{"query":"mainScheme"},"effect":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":3}}}}
          }]},
          {"card":"06001a","abilities":[{"limitPerPhase":1,
            "trigger":{"event":"WhenMinionEngaged","timing":"Response","subject":"you","form":"hero"},
            "effect":{"draw":{"player":"you","count":2}}}]}
        ]}
        """));

    private static void Finish(World world, AbilityRunner runner, List<GameEvent> events,
        Func<Prompt, Decision>? answer = null)
    {
        for (int i = 0; i < 30; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) return;
            Sequence.Answer(world, Cards, runner, prompt, answer?.Invoke(prompt) ?? Decision.Decline, events);
        }
        Assert.Fail("cost continuation did not settle");
    }
}
