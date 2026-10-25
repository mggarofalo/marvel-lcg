using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

public sealed class HailHydraTests
{
    [Theory]
    [InlineData("03030", false)]
    [InlineData("03030", true)]
    [InlineData("04057", false)]
    [InlineData("04147", false)]
    [InlineData("04147", true)]
    [Rule("rr:stun-stunned.6")]
    public void OnlyPlayersActuallyAttackedSkipTheirSearch(string face, bool stunned)
    {
        // Stunned replaces an attack with discarding the status card.
        var runner = new AbilityRunner(AbilityCatalog.Parse(HailHydraFragment.Json));
        var (_, world) = Playing(_ => { }, hero: true,
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var attacker = world.CreateCard("04145", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        if (stunned) Statuses.Give(world, attacker, "stunned");
        var reserve = world.CreateCard("04145", world.AreaOf(DeckType.EncounterDeck));
        var discarded = world.CreateCard("04145", world.AreaOf(DeckType.EncounterDiscardPile));
        var treachery = world.CreateCard(face, world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, treachery, 0);
        var searches = new List<int>();
        var events = Finish(world, runner, prompt =>
        {
            searches.Add(prompt.Player);

            var target = prompt.Player == 0 ? reserve : discarded;
            var choice = Assert.Single(prompt.Affordances, option => option.AnchorId == target.ObjectId);
            return choice.Targets is null ? Decision.Take(choice.Id)
                : new Decision(choice.Id, [target.ObjectId]);
        });
        Assert.Equal(stunned ? [0, 1] : new List<int> { 1 }, searches);
        Assert.Equal(stunned ? 0 : 1, events.OfType<AttackCompleted>().Count());
        Assert.Equal(stunned ? DeckType.EngagedEnemiesArea : DeckType.EncounterDeck, reserve.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, discarded.Area.Type);
        Assert.Equal(1, discarded.Area.PlayArea.Player);
    }

    [Theory]
    [InlineData("04147", true)]
    [InlineData("04147", false)]
    [InlineData("03030", true)]
    [InlineData("03030", false)]
    public void EveryHeroAttackedAvoidsSearchWhileAlterEgosSearchIndependently(string face, bool hero)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(HailHydraFragment.Json));
        var (_, world) = Playing(_ => { }, hero: hero,
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        if (hero) world.Seats[1].IdentityCard.TurnTo("01010a");
        for (int player = 0; player < 2; player++)
            world.CreateCard("04145", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var reserve = world.CreateCard("04145", deck);
        var second = world.CreateCard("04145", deck);
        int[] before = deck.Cards.Select(card => card.ObjectId).ToArray();
        runner.ExecuteReveal(world, world.CreateCard(face, world.AreaOf(DeckType.RevealingArea)), 0);
        var searches = new List<int>();
        var events = Finish(world, runner, prompt =>
        {
            searches.Add(prompt.Player);
            return Decision.Take(prompt.Player == 0 ? reserve.ObjectId : second.ObjectId);
        });
        Assert.Equal(hero ? 2 : 0, events.OfType<AttackCompleted>().Count());
        Assert.Equal(hero ? 0 : 2, searches.Count);
        if (hero) Assert.Equal(before, deck.Cards.Select(card => card.ObjectId));
        else
        {
            Assert.Equal(0, reserve.Area.PlayArea.Player);
            Assert.Equal(1, second.Area.PlayArea.Player);
        }
    }

    [Theory]
    [InlineData("04147")]
    [InlineData("03030")]
    public void EmptySearchPoolCompletesWithoutAnUnanswerableChoice(string face)
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse(HailHydraFragment.Json));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        foreach (var card in world.AreaOf(DeckType.EncounterDeck).Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        world.CreateCard("04087", world.AreaOf(DeckType.EncounterDeck));
        runner.ExecuteReveal(world, world.CreateCard(face, world.AreaOf(DeckType.RevealingArea)), 0);
        var events = Finish(world, runner, prompt => throw new InvalidOperationException(prompt.Description));
        Assert.Empty(events.OfType<AttackCompleted>());
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
    }

    [Theory]
    [InlineData("04147")]
    [InlineData("03030")]
    [Rule("rr:defend-defense.5")]
    public void AnotherPlayerDefendingBecomesTheAttackedPlayerForTheReceipt(string face)
    {
        // The defender's player becomes the target player of the attack.
        var runner = new AbilityRunner(AbilityCatalog.Parse(HailHydraFragment.Json));
        var (_, world) = Playing(_ => { }, hero: true,
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var defender = world.Seats[1].IdentityCard;
        defender.TurnTo("01010a");
        world.CreateCard("04145", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var reserve = world.CreateCard("04145", world.AreaOf(DeckType.EncounterDeck));
        runner.ExecuteReveal(world, world.CreateCard(face, world.AreaOf(DeckType.RevealingArea)), 0);
        var searches = new List<int>();
        Finish(world, runner, prompt =>
        {
            searches.Add(prompt.Player);
            return Decision.Take(reserve.ObjectId);
        }, prompt => prompt.Player == 1 && prompt.Affordances.Any(option => option.AnchorId == defender.ObjectId)
            ? Decision.Take(prompt.Affordances.Single(option => option.AnchorId == defender.ObjectId).Id)
            : Decision.Decline);
        Assert.Equal(new List<int> { 0 }, searches);
        Assert.Equal(0, reserve.Area.PlayArea.Player);
        Assert.False(defender.Ready);
    }

    [Fact]
    public void AnEmptyLaterActivationBatchDoesNotReuseEarlierAttackedPlayers()
    {
        var book = System.Text.Json.Nodes.JsonNode.Parse(HailHydraFragment.Json)!;
        book["cards"]![1]!["abilities"]![0]!["effect"]!["seq"]!.AsArray().Insert(1,
            System.Text.Json.Nodes.JsonNode.Parse("""
                {"enemyAttacks":{"enemies":{"enemiesWithTrait":"ABSENT"},"against":"you"}}
                """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        world.CreateCard("04145", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var reserve = world.CreateCard("04145", world.AreaOf(DeckType.EncounterDeck));
        runner.ExecuteReveal(world, world.CreateCard("04147", world.AreaOf(DeckType.RevealingArea)), 0);
        int searches = 0;
        Finish(world, runner, prompt => { searches++; return Decision.Take(reserve.ObjectId); });
        Assert.Equal(1, searches);
        Assert.Equal(DeckType.EngagedEnemiesArea, reserve.Area.Type);
    }

    [Fact]
    public void SearchPlacementOpensEntryInterruptsBeforeEngagement()
    {
        var book = System.Text.Json.Nodes.JsonNode.Parse(HailHydraFragment.Json)!;
        book["cards"]!.AsArray().Add(System.Text.Json.Nodes.JsonNode.Parse("""
            {"card":"01006","abilities":[{"trigger":{"timing":"ForcedInterrupt","event":"WhenCardEntersPlay","subject":"game"},
              "when":{"isKind":{"card":"trigger.subject","kind":"minion"}},"effect":{"cancelOccurrence":"trigger"}}]}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        InPlay(world, "01006");
        var reserve = world.CreateCard("04145", world.AreaOf(DeckType.EncounterDeck));
        runner.ExecuteReveal(world, world.CreateCard("04147", world.AreaOf(DeckType.RevealingArea)), 0);
        int searches = 0;
        Finish(world, runner, prompt => { searches++; return Decision.Take(reserve.ObjectId); });
        Assert.Equal(1, searches);
        Assert.Equal(DeckType.EncounterDeck, reserve.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
    }

    private static List<GameEvent> Finish(World world, AbilityRunner runner, Func<Prompt, Decision> answer,
        Func<Prompt, Decision>? optional = null)
    {
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, runner, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 40, prompt.Description);
            var decision = prompt.Cancellable ? optional?.Invoke(prompt) ?? Decision.Decline : prompt.Affordances is [{ Verb: "Order", Targets: { } targets } order] ? new Decision(order.Id, targets.Legal.ToArray()) : answer(prompt);
            Sequence.Answer(world, world.Facts, runner, prompt, decision, events);
            prompt = Sequence.Work(world, world.Facts, runner, events);
        }
        return events;
    }
}
