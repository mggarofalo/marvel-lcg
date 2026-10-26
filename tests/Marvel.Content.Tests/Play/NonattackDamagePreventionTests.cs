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

public sealed class NonattackDamagePreventionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:damage.3.2")]
    public void IndividualDamageRetainsDealtAmountAndDeclineKeepsTheNextInstance(bool decline)
    {
        // Prevention changes damage taken: "the amount of damage dealt is not modified".
        var f = new BlackWidowPreventionFixture();
        var stance = f.Upgrade("08032");
        var damage = DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.Villain, f.Hero, 5, "individual", "Damage"), f.Events);
        var prompt = f.Find(stance);
        Assert.False(damage.IsComplete);
        Assert.Equal(0, f.Hero.Damage);
        var baseline = (f.World.Digest().Canonical(), f.World.Random.Generator.WordsConsumed,
            f.World.Agenda.Count, f.World.Agenda.Occurrence!.DirectDamage!.Remaining);
        Assert.Throws<RulesNotImplementedException>(() => f.Answer(prompt, Decision.Take(int.MaxValue)));
        Assert.Equal(baseline, (f.World.Digest().Canonical(), f.World.Random.Generator.WordsConsumed,
            f.World.Agenda.Count, f.World.Agenda.Occurrence!.DirectDamage!.Remaining));
        if (decline) f.Answer(prompt, Decision.Decline); else f.Use(prompt, stance);
        f.Finish(stance);
        Assert.True(damage.IsComplete);
        Assert.Equal(5, damage.Dealt);
        Assert.Equal(decline ? 5 : 2, damage.Taken);
        var next = DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.Villain, f.Hero, 2, "next", "Damage"), f.Events);
        if (decline) f.Use(f.Find(stance), stance);
        f.Finish(stance);
        Assert.True(next.IsComplete);
        Assert.Equal(2, next.Dealt);
        Assert.Equal(decline ? 0 : 2, next.Taken);
        Assert.Equal(decline ? 5 : 4, f.Hero.Damage);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:damage.step.2")]
    [Rule("rr:damage.step.3")]
    public void ToughOrZeroDamageNeverOffersTakingPrevention(bool tough)
    {
        // "Tough status cards" precede "would take any amount of damage".
        var f = new BlackWidowPreventionFixture();
        var stance = f.Upgrade("08032");
        if (tough) Statuses.Give(f.World, f.Hero, Statuses.Tough);
        var result = DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.Villain, f.Hero, tough ? 5 : 0, "status", "Damage"), f.Events);
        f.Finish(stance);
        Assert.True(result.IsComplete);
        Assert.Equal(tough ? 5 : 0, result.Dealt);
        Assert.Equal(0, result.Taken);
        Assert.Equal(0, f.Hero.Damage);
        Assert.False(Statuses.Has(f.World, f.Hero, Statuses.Tough));
        Assert.Equal(DeckType.UpgradesArea, stance.Area.Type);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void ADepartedAndReenteredRecipientCannotReceiveItsOldAssignment()
    {
        // A card returning to play is "considered to be a new copy of the card".
        var f = new BlackWidowPreventionFixture();
        var stance = f.Upgrade("08032");
        var result = DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.Villain, f.Hero, 2, "incarnation", "Damage"), f.Events);
        var prompt = f.Find(stance);
        var original = f.Hero.Incarnation;
        World.MoveToTop(f.Hero, f.World.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        World.MoveToTop(f.Hero, f.World.Seats[1].Hero);
        Assert.NotEqual(original, f.Hero.Incarnation);
        f.Answer(prompt, Decision.Decline);
        f.Finish(stance);
        Assert.True(result.IsComplete);
        Assert.Equal(0, result.Taken);
        Assert.Equal(0, f.Hero.Damage);
        Assert.Equal(DeckType.UpgradesArea, stance.Area.Type);
    }

    [Fact]
    [Rule("rr:prevent.1")]
    public void NestedDamageToTheSameHeroDoesNotBorrowOuterPrevention()
    {
        // Synthetic interrupt: prevent 3, then deal 2 to the same recipient.
        // "Reduce the amount of damage the target takes" applies to this instance.
        var f = Fixture("Interrupt", """
            {"seq":[{"preventDamage":{"card":"trigger.target","amount":3}},
                {"dealDamage":{"cards":"trigger.target","amount":2}}]}
            """);
        var source = f.Upgrade("01008");
        var result = DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.Villain, f.Hero, 5, "outer", "Damage"), f.Events);
        f.Use(f.Find(source), source);
        f.Finish(source);
        Assert.True(result.IsComplete);
        Assert.Equal(5, result.Dealt);
        Assert.Equal(2, result.Taken);
        Assert.Equal(4, f.Hero.Damage);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:forced.4")]
    public void ForcedReplacementFinishesBeforeOptionalPrevention()
    {
        // "Forced interrupts initiate before non-forced interrupts."
        // Synthetic replacement absorbs all damage on its discarded source.
        var f = Fixture("ForcedInterrupt", """{"soakDamage":{"onto":"this"}}""");
        var forced = f.Upgrade("01008");
        var stance = f.Upgrade("08032");
        var result = DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.Villain, f.Hero, 5, "forced", "Damage"), f.Events);
        f.Finish(stance);
        Assert.True(result.IsComplete);
        Assert.Equal(0, result.Dealt);
        Assert.Equal(0, result.Taken);
        Assert.Equal(5, forced.Damage);
        Assert.Equal(0, f.Hero.Damage);
        Assert.Equal(DeckType.UpgradesArea, stance.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualEnergyBarrierResolvesNonattackDamageInEitherForm(bool alterEgo)
    {
        // Printed 05017: prevent 1 damage and deal 1 to a chosen enemy, in either form.
        var f = new BlackWidowPreventionFixture(hero: !alterEgo);
        var barrier = f.Upgrade("05017");
        barrier.PlaceTokens("c_reflection", 1);
        var result = DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.Villain, f.Hero, 2, "barrier", "Damage"), f.Events);
        f.Use(f.Find(barrier), barrier);
        var choice = f.Work();
        Assert.Equal(Question.Element, choice.Asking);
        f.Answer(choice, Decision.Take(Assert.Single(choice.Affordances).Id));
        f.Finish(barrier);
        Assert.True(result.IsComplete);
        Assert.Equal(2, result.Dealt);
        Assert.Equal(1, result.Taken);
        Assert.Equal(1, f.Hero.Damage);
        Assert.Equal(1, f.Villain.Damage);
        Assert.Equal(DeckType.DiscardPile, barrier.Area.Type);
        Assert.False(f.World.Agenda.IsBusy);
    }

    private static BlackWidowPreventionFixture Fixture(string timing, string effect)
    {
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var row = book["cards"]!.AsArray().Single(r => r!["card"]!.GetValue<string>() == "01008")!;
        row["abilities"] = JsonNode.Parse($$"""
            [{"trigger":{"event":"WhenDamageWouldBeDealt","timing":"{{timing}}","target":"you"},
              "cost":{"discard":"this"},"effect":{{effect}}}]
            """);
        return new(runner: new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString())));
    }
}
