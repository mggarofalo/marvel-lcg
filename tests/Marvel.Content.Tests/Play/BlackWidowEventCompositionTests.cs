using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.BlackWidowEventCompositionFixture;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowEventCompositionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CovertOpsRemovesFourAndConfusesInEitherForm(bool hero)
    {
        var world = Board(hero);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 6 - main.Tokens.GetValueOrDefault("k_threat"));
        var source = PlayEvent(world, "08003");
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities!, []));
        Assert.Equal(1, prompt.Player);
        Sequence.Answer(world, world.Facts, world.Abilities!, prompt, Decision.Take(main.ObjectId), []);
        Sequence.Finish(world, world.Facts, world.Abilities!, []);
        Assert.Equal(2, main.Tokens["k_threat"]);
        Assert.True(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:target.3")]
    [Rule("rr:labeled-ability.4")]
    public void CovertOpsConfusesWithoutAnyRemovableThreatInEitherForm(bool hero)
    {
        // "A target is valid ... if any part of that ability can affect that target."
        // Ruling 0fc2a7cc9197e127: confusion alone still counts as a thwart.
        var world = Board(hero);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", -main.Tokens.GetValueOrDefault("k_threat"));
        var source = PlayEvent(world, "08003");
        Sequence.Finish(world, world.Facts, world.Abilities!, []);
        Assert.True(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Confused));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Seats[1].Hand.Cards);
        Assert.Equal(0, main.Tokens["k_threat"]);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [Rule("rr:labeled-ability.6")]
    public void OwnConfusionCancelsEntirePaidCovertOpsAndPreservesOtherSeatStatus(int threat)
    {
        // "The entire ability (except for its costs) is canceled."
        var world = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", threat - main.Tokens.GetValueOrDefault("k_threat"));
        foreach (var seat in world.Seats) Statuses.Give(world, seat.IdentityCard, Statuses.Confused);
        var source = PlayEvent(world, "08003");
        Sequence.Finish(world, world.Facts, world.Abilities!, []);
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Confused));
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.False(Statuses.Has(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Confused));
        Assert.Equal(threat, main.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Seats[1].Hand.Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AlreadyConfusedVillainAllowsThreatRemovalButCannotAloneMakeEventLegal(bool threatAvailable)
    {
        var world = Board();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, villain, Statuses.Confused);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", (threatAvailable ? 2 : 0) - main.Tokens.GetValueOrDefault("k_threat"));
        if (!threatAvailable)
        {
            Assert.Throws<RulesNotImplementedException>(() => PlayEvent(world, "08003"));
            Assert.Equal(4, world.Seats[1].Hand.Cards.Count);
        }
        else
        {
            var source = PlayEvent(world, "08003");
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities!, []));
            Sequence.Answer(world, world.Facts, world.Abilities!, prompt, Decision.Take(main.ObjectId), []);
            Sequence.Finish(world, world.Facts, world.Abilities!, []);
            Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        }
        Assert.Equal(0, main.Tokens["k_threat"]);
        Assert.Equal(1, Statuses.Count(world, villain, Statuses.Confused));
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.3")]
    public void DanceCanRetargetThenAttackTheFirstEnemyAgain()
    {
        // "Each of those instances is considered a separate attack."
        var world = Board();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard("01130", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var source = PlayEvent(world, "08004");
        foreach (var target in new[] { villain, minion, villain })
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities!, []));
            Assert.Equal(1, prompt.Player);
            Assert.Contains(prompt.Affordances, option => option.AnchorId == target.ObjectId);
            Sequence.Answer(world, world.Facts, world.Abilities!, prompt, Decision.Take(target.ObjectId), []);
        }
        Sequence.Finish(world, world.Facts, world.Abilities!, []);
        Assert.Equal(4, villain.Damage);
        Assert.Equal(2, minion.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attack-player-ability-type.3")]
    [Rule("rr:stun-stunned.5")]
    public void DanceOfDeathMakesThreeSeparatelyTargetedAttacks(bool stunned)
    {
        // "Each of those instances is considered a separate attack."
        // Stun cancels the next attack; the other two attacks still resolve.
        var world = Board();
        if (stunned) Statuses.Give(world, world.Seats[1].IdentityCard, Statuses.Stunned);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var source = PlayEvent(world, "08004");
        for (int attack = 0; attack < 3; attack++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities!, []));
            Assert.Equal(1, prompt.Player);
            int[] cumulative = stunned ? [0, 0, 2] : [0, 1, 3];
            Assert.Equal(cumulative[attack], villain.Damage);
            Sequence.Answer(world, world.Facts, world.Abilities!, prompt, Decision.Take(villain.ObjectId), []);
        }
        Sequence.Finish(world, world.Facts, world.Abilities!, []);
        Assert.Equal(stunned ? 5 : 6, villain.Damage);
        Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Stunned));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}




