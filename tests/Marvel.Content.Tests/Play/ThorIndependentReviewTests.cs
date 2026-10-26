using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorIndependentReviewTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:restricted")]
    public void MjolnirAsThirdRestrictedCardOffersItselfForDisposal()
    {
        // "They must immediately choose and discard ... until they have only two."
        // Synthetic Restricted grants isolate the printed keyword on actual Mjolnir.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var first = world.CreateCard("06008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var second = world.CreateCard("06008", first.Area);
        foreach (var card in new[] { first, second })
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "restricted", 1, Affects: card.ObjectId));
        var hammer = world.CreateCard("06009", world.Seats[0].Hand);
        var resource = world.CreateCard("01088", world.Seats[0].Hand);
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], hammer, [resource.ObjectId], []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(new[] { first.ObjectId, second.ObjectId, hammer.ObjectId }.Order(), prompt.Affordances.Select(a => a.AnchorId).Order());
        Assert.Equal(3, Modified(world, world.Seats[0].IdentityCard, "attack"));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(hammer.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.DiscardPile, hammer.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, first.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, second.Area.Type);
        Assert.Equal(2, Modified(world, world.Seats[0].IdentityCard, "attack"));
        Assert.False(Traits.Has(world, world.Seats[0].IdentityCard, "AERIAL", world.Facts));
    }

    [Theory]
    [InlineData("06001a", 14, true)]
    [InlineData("06001b", 14, true)]
    [InlineData("06001a", 13, false)]
    [Rule("rr:hit-points.2.3")]
    [Rule("rr:hit-points.2.1")]
    public void LosingHelmetEliminatesOnlyAtZeroRemainingHealth(string face, int damage, bool eliminated)
    {
        // "If that ability later ceases to be in effect, reduce ... by X."
        // "If a player's hit point dial is reduced to zero, that player is defeated."
        var world = ThorActualFixture.Board(face, 2);
        var hero = world.Seats[0].IdentityCard;
        var helmet = world.CreateCard("06010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        hero.TakeDamage(damage);
        Assert.False(world.Seats[0].Eliminated);
        Assert.Equal(19 - damage, DamagePlacement.Health(world, world.Facts, hero) - hero.Damage);
        var treachery = world.CreateCard(AuthoredCards.CaughtOffGuard, world.AreaOf(DeckType.RevealingArea));
        world.Abilities.ExecuteReveal(world, treachery, 0);
        var discard = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, world.Abilities, []));
        Sequence.Answer(world, world.Facts, world.Abilities, discard, Decision.Take(helmet.ObjectId), []);
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        Assert.Equal(eliminated, world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.False(world.IsOver);
        if (!eliminated) Assert.Equal(1, DamagePlacement.Health(world, world.Facts, hero) - hero.Damage);
    }

    [Theory]
    [InlineData("01088", "06022")]
    [InlineData("01089", "06023")]
    [InlineData("01090", "06024")]
    [InlineData("01055", "06016")]
    [Rule("rr:max-maximum.2")]
    public void PrintedDeckMaximumCountsCoreAndThorReprintsTogether(string core, string thor)
    {
        // "Max X per deck" restricts copies by title, including reprints.
        int maximum = (int)Cards.PrintedValue(thor, "MaxPerDeck", 1);
        var legal = Enumerable.Repeat(core, maximum - 1).Append(thor)
            .Select(face => new Creation(face, CreationSource.PlayerDeck, 0)).ToArray();
        Assert.Equal(maximum, Blueprints.From(legal, Cards).Count);
        var refused = Assert.Throws<ArgumentException>(() => Blueprints.From(
            [.. legal, new Creation(core, CreationSource.PlayerDeck, 0)], Cards));
        Assert.Contains(Cards.Title(thor), refused.Message, StringComparison.Ordinal);
        Assert.Contains($"maximum is {maximum}", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void GodOfThunderPaymentStaysSpentAcrossTargetChoiceAndRejectedRetry()
    {
        // "Pay the cost(s)." Target selection follows the committed payment.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("06008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var kick = world.CreateCard("01005", world.Seats[0].Hand);
        var energy = world.CreateCard("06022", world.Seats[0].Hand);
        var minion = world.CreateCard("01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == kick.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [source.ObjectId, energy.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.False(source.Ready);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(0, minion.Damage);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(source.ObjectId), []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(minion.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.False(source.Ready);
        Assert.Equal(DeckType.DiscardPile, kick.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }
}
