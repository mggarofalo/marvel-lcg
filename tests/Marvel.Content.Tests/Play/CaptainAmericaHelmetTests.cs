using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaHelmetTests : WouldBeDefeatedTestBase
{
    [Theory]
    [InlineData(11)]
    [InlineData(20)]
    [Rule("rr:damage.step.6")]
    [Rule("rr:hit-points.2")]
    public void HelmetAssignsOneRemainingHitPointBeforeDiscarding(long incoming)
    {
        // rr:damage.step.6: "Abilities that trigger “when [character] would be defeated...”"
        // rr:hit-points.2: "An identity’s or villain’s hit point dial represents their remaining hit points."
        // 03008: "set his hit point dial to 1 instead. Then, discard this card."
        var (world, captain, helmet, source) = HelmetBoard();
        var events = new List<GameEvent>();
        var damage = DamagePlacement.Resolve(world, Cards, new(source, captain, incoming, "test", "Deal_Damage"), events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.False(damage.IsComplete);
        Assert.Equal(incoming, captain.Damage);
        var offered = Assert.Single(prompt.Affordances, item => item.AnchorId == helmet.ObjectId);
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(offered.Id), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(damage.IsComplete);
        Assert.Equal(10, captain.Damage);
        Assert.Equal(DeckType.HeroArea, captain.Area.Type);
        Assert.Equal(DeckType.DiscardPile, helmet.Area.Type);
        int assigned = events.FindIndex(item => item is FieldSet { Field: "health", To: 1 } set && set.Card == captain.ObjectId);
        int discarded = events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(landing => landing.Card == helmet.ObjectId));
        Assert.True(assigned >= 0 && discarded > assigned);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(3)]
    [Rule("rr:maximum-hit-points")]
    public void AssignmentUsesModifiedMaximum(long modifier)
    {
        // rr:maximum-hit-points: "plus or minus all “gets” hit point modifiers that are active on that character."
        var (world, captain, helmet, source) = HelmetBoard();
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "health", modifier, Affects: captain.ObjectId));
        var events = new List<GameEvent>();
        DamagePlacement.Resolve(world, Cards, new(source, captain, 20, "test", "Deal_Damage"), events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        var offered = Assert.Single(prompt.Affordances, item => item.AnchorId == helmet.ObjectId);
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(offered.Id), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(10 + modifier, captain.Damage);
        Assert.Equal(1, DamagePlacement.Health(world, Cards, captain) - captain.Damage);
        Assert.False(world.Seats[0].Eliminated);
    }

    [Fact]
    [Rule("rr:ability.11")]
    public void OptionalHelmetCanBeDeclined()
    {
        // rr:ability.11: "Unless prefaced by the word “Forced”, all interrupt and response abilities are optional."
        var (world, captain, helmet, source) = HelmetBoard();
        var events = new List<GameEvent>();
        DamagePlacement.Resolve(world, Cards, new(source, captain, 20, "test", "Deal_Damage"), events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Contains(prompt.Affordances, item => item.AnchorId == helmet.ObjectId);
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Decline, events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(world.Seats[0].Eliminated);
        Assert.DoesNotContain(events, item => item is FieldSet { Field: "health", To: 1 });
    }

    [Fact]
    [Rule("rr:maximum-hit-points")]
    public void HelmetAlsoReplacesDefeatCausedByLosingAMaximumModifier()
    {
        // rr:maximum-hit-points: "plus or minus all “gets” hit point modifiers that are active on that character."
        var (world, captain, helmet, _) = HelmetBoard();
        var modifier = world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "health", 3, Affects: captain.ObjectId));
        captain.TakeDamage(12);
        Assert.Equal(2, DamagePlacement.Health(world, Cards, captain) - captain.Damage);
        modifier.Dispose();
        var events = new List<GameEvent>();
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        var offered = Assert.Single(prompt.Affordances, item => item.AnchorId == helmet.ObjectId);
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(offered.Id), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.False(world.Seats[0].Eliminated);
        Assert.Equal(10, captain.Damage);
        Assert.Equal(DeckType.DiscardPile, helmet.Area.Type);
    }

    [Theory]
    [InlineData("03001b")]
    [InlineData("01001a")]
    [Rule("rr:form-change-form.5")]
    public void HelmetDoesNotSaveADifferentNamedCharacter(string face)
    {
        // rr:form-change-form.5: "card abilities that interact with their hero do not interact with their identity."
        var (world, captain, _, source) = HelmetBoard(face);
        var events = new List<GameEvent>();
        DamagePlacement.Resolve(world, Cards, new(source, captain, 20, "test", "Deal_Damage"), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(world.Seats[0].Eliminated);
        Assert.DoesNotContain(events, item => item is FieldSet { Field: "health", To: 1 });
    }

    [Fact]
    public void RepeatedOfferQueriesPreserveStateAndContinuation()
    {
        var (world, captain, helmet, source) = HelmetBoard();
        var events = new List<GameEvent>();
        DamagePlacement.Resolve(world, Cards, new(source, captain, 20, "test", "Deal_Damage"), events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        var occurrence = world.Agenda.Current!.Value.Require<DefeatAbilityOperation>().Window.Origin!;
        string digest = world.Digest().Canonical();
        long random = world.Random.Generator.WordsConsumed;
        var outstanding = world.Agenda.Outstanding.ToArray();
        var before = events.ToArray();
        for (int index = 0; index < 3; index++)
            Assert.NotEmpty(world.WindowAbilities.Waiting(world, occurrence, WindowKind.Interrupt));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
        Assert.Equal(before, events);
        var offered = Assert.Single(prompt.Affordances, item => item.AnchorId == helmet.ObjectId);
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(offered.Id), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(10, captain.Damage);
        Assert.Equal(DeckType.DiscardPile, helmet.Area.Type);
        var (control, controlCaptain, controlHelmet, controlSource) = HelmetBoard();
        var controlEvents = new List<GameEvent>();
        DamagePlacement.Resolve(control, Cards,
            new(controlSource, controlCaptain, 20, "test", "Deal_Damage"), controlEvents);
        var controlPrompt = Assert.IsType<Prompt>(Sequence.Work(control, Cards, control.Abilities, controlEvents));
        var controlOffer = Assert.Single(controlPrompt.Affordances, item => item.AnchorId == controlHelmet.ObjectId);
        Sequence.Answer(control, Cards, control.Abilities, controlPrompt, Decision.Take(controlOffer.Id), controlEvents);
        Assert.Null(Sequence.Work(control, Cards, control.Abilities, controlEvents));
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(controlEvents), System.Text.Json.JsonSerializer.Serialize(events));
    }

    private static (World World, Card Captain, Card Helmet, Card Source) HelmetBoard(string face = "03001a")
    {
        var world = Empty();
        var seat = world.Seats[0];
        seat.IdentityCard = world.CreateCard(face == "01001a" ? "01001a,01001b" : "03001a,03001b", seat.Hero);
        seat.IdentityCard.TurnTo(face);
        world.CreateCard("01088", seat.Deck);
        var helmet = world.CreateCard("03008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var source = world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        return (world, seat.IdentityCard, helmet, source);
    }
}
