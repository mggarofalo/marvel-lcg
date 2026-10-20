using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class VillainCardReferenceTests : CoreCardTestBase
{
    [Rule("rr:villain-defeat.3.1")]
    [Rule("rr:leaves-play.1")]
    [Theory]
    [InlineData("absent")]
    [InlineData("different")]
    [InlineData("reentered")]
    [InlineData("other-area")]
    public void AChosenVillainCannotBecomeAnUnrelatedOrReenteredCharacter(string replacement)
    {
        // Only same-title stages are "the same character"; a reentered copy
        // has new provenance rather than continuing the captured selection.
        var world = Hero("01001a");
        var source = world.CreateCard("04043", world.Seats[0].Hand);
        var original = world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var reference = new AbilityCardReference(original, original.Area.Id, original.Incarnation);
        World.MoveToTop(original, world.AreaOf(DeckType.EncounterDiscardPile));
        if (replacement == "reentered")
        {
            World.MoveToTop(original, world.AreaOf(DeckType.VillainArea));
            World.MoveToTop(original, world.AreaOf(DeckType.EncounterDiscardPile));
        }
        if (replacement != "absent")
            world.CreateCard(replacement == "different" ? "01094" : "01114",
                world.AreaOf(DeckType.VillainArea, replacement == "other-area" ? PlayArea.Of(0) : PlayArea.Villains));
        Assert.Null(reference.Resolve(world, source, "chosen"));
        Assert.Same(original, reference.Card);
    }

    [Fact]
    public void AmbiguousSameTitleVillainsAreNotResolvedByObjectOrder()
    {
        // An ambiguous topology is an unsupported engine contract, not a rule
        // allowing an arbitrary first matching villain to inherit a selection.
        var world = Hero("01001a");
        var source = world.CreateCard("04043", world.Seats[0].Hand);
        var original = world.CreateCard("01113", world.AreaOf(DeckType.VillainArea));
        var reference = new AbilityCardReference(original, original.Area.Id, original.Incarnation);
        World.MoveToTop(original, world.AreaOf(DeckType.EncounterDiscardPile));
        world.CreateCard("01114", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01115", world.AreaOf(DeckType.VillainArea));
        Assert.Contains("multiple same-title", Assert.Throws<RulesNotImplementedException>(() =>
            reference.Resolve(world, source, "chosen")).Message);
    }
}

