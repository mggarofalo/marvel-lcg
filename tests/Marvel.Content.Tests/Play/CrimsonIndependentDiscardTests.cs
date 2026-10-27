using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using static Marvel.Content.Tests.Play.CrimsonIndependentDiscardFixture;

namespace Marvel.Content.Tests.Play;

/// <summary>Test-local printed Crimson composition, outside production pack admission.</summary>
public sealed class CrimsonIndependentDiscardTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [Rule("rr:target.3.4")]
    [Rule("rr:target.3.7")]
    [Rule("rr:ability.4")]
    public void IndependentDiscardSurvivesAnEmptyEnemySentence(bool master, bool blocked, bool last)
    {
        // A target is valid "if at least one of those effects can affect the target";
        // "cannot take damage" invalidates damage alone. Resolve "one sentence at a time".
        // ruling:8c9c9d30f4c01189: each Invocation "always has a valid target" (itself).
        var (world, game, deck, _) = BoardActualWithRunner(Runner(blocked), "09032", master, last);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var status = Statuses.Give(world, villain, Statuses.Stunned);
        var original = deck.PublicTop!;
        Assert.Equal(!blocked, world.DamageAbilities.CanTakeDamage(world, villain, original));
        var events = Initiate(world, game, deck, master);
        if (!blocked)
        {
            Assert.Equal(1, game.Pending!.Player);
            Assert.Equal(villain.ObjectId, Assert.Single(game.Pending.Affordances).AnchorId);
            events.AddRange(game.Resolve(Decision.Take(villain.ObjectId)).Events);
        }
        Assert.Equal(blocked ? 0 : 7, villain.Damage);
        Assert.Same(status, Assert.Single(world.Cards, card => card.Area.Type == DeckType.StatusArea));
        AssertLifecycle(world, game, deck, original, events, master, last);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:target.3.4")]
    public void StatusAloneStillRequiresAnExplicitEnemyChoice(bool master)
    {
        // "at least one of those effects" permits stun when damage cannot affect that enemy.
        var (world, game, deck, _) = BoardActualWithRunner(Runner(true), "09032", master);
        var original = deck.PublicTop!;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = Initiate(world, game, deck, master);
        Assert.Equal(villain.ObjectId, Assert.Single(game.Pending!.Affordances).AnchorId);
        var prompt = game.Pending;
        string before = Snapshot(world);
        Assert.ThrowsAny<Exception>(() => game.Resolve(Decision.Take(int.MaxValue)));
        Assert.Equal(before, Snapshot(world));
        Assert.Same(prompt, game.Pending);
        events.AddRange(game.Resolve(Decision.Take(villain.ObjectId)).Events);
        Assert.Equal(1, Statuses.Count(world, villain, Statuses.Stunned));
        Assert.Equal(0, villain.Damage);
        AssertLifecycle(world, game, deck, original, events, master, false);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyChoiceQueriesPreserveSubsequentEventsAndRng(bool master)
    {
        var (world, game, deck, runner) = BoardActualWithRunner(Runner(true), "09032", master);
        var (control, controlGame, controlDeck, _) = BoardActualWithRunner(Runner(true), "09032", master);
        Statuses.Give(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned);
        Statuses.Give(control, control.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned);
        string before = Snapshot(world);
        for (int i = 0; i < 3; i++)
            foreach (var action in runner.Actions(world, 1)) _ = runner.Describe(world, action);
        Assert.Equal(before, Snapshot(world));
        var events = Initiate(world, game, deck, master);
        var expected = Initiate(control, controlGame, controlDeck, master);
        Assert.Equal(Snapshot(control), Snapshot(world));
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(events));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:target.2")]
    public void EmptyEnemySentenceCannotAdmitAnEntirelyIneffectiveSpecial(bool master)
    {
        // "at least one valid target" is still required by the complete ability.
        // This synthetic variant has neither self-discard nor another effective sentence.
        string special = """{"seq":[""" + EnemySentence
            + """,{"giveStatus":{"card":{"query":"villain"},"status":"stunned"}}]}""";
        var (world, game, deck, _) = BoardActualWithRunner(Runner(true, special), "09032", master);
        Statuses.Give(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned);
        var original = deck.PublicTop!;
        var events = Initiate(world, game, deck, master);
        Assert.Same(original, deck.PublicTop);
        Assert.DoesNotContain(events.OfType<CardsMoved>(), item => item.Verb == "Discard"
            && item.Cards.Any(card => card.Card == original.ObjectId));
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:then.2")]
    public void EmptyEnemySentenceCannotCompleteADependentDraw()
    {
        // If the predecessor "does not fully resolve", its following text "does not attempt to resolve".
        string special = """{"seq":[{"then":{"effect":""" + EnemySentence
            + """, "then":{"draw":{"player":"you","count":1}}}},{"discardInvocation":"this"}]}""";
        var (world, game, deck, _) = BoardActualWithRunner(Runner(true, special), "09032", false);
        Statuses.Give(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned);
        var original = deck.PublicTop!;
        int hand = world.Seats[1].Hand.Cards.Count;
        var events = Initiate(world, game, deck, false);
        Assert.Equal(hand - 1, world.Seats[1].Hand.Cards.Count);
        Assert.DoesNotContain(events.OfType<CardsMoved>(), item => item.Verb == "Draw");
        // The dependent predecessor keeps its initiation gate, so this Special is skipped.
        Assert.Same(original, deck.PublicTop);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:guard.1")]
    public void NonattackEnemyChoiceRetainsGuardAndHeroStun()
    {
        // Guard prevents "attack any villain"; ordinary Crimson damage is not an attack.
        var (world, game, deck, _) = BoardActualWithRunner(Runner(false), "09032", false);
        var hero = world.Seats[1].IdentityCard;
        var stunned = Statuses.Give(world, hero, Statuses.Stunned);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var guard = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "guard", Amount: 1,
            Card: guard.ObjectId, Affects: guard.ObjectId));
        Assert.DoesNotContain(villain, BasicPowers.Attackable(world, world.Facts, 1));
        var original = deck.PublicTop!;
        var events = Initiate(world, game, deck, false);
        Assert.Equal(2, game.Pending!.Affordances.Count);
        events.AddRange(game.Resolve(Decision.Take(villain.ObjectId)).Events);
        Assert.Equal(7, villain.Damage);
        Assert.Equal(0, guard.Damage);
        Assert.Equal(DeckType.StatusArea, stunned.Area.Type);
        AssertLifecycle(world, game, deck, original, events, false, false);
    }
}
