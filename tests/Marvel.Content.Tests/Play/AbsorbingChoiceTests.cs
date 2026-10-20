using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbsorbingChoiceTests
{
    [Theory]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    public void AvalancheEachPlayerCanIndependentlyPayOrTakeThresholdDamage(int delay, int damage)
    {
        var f = AbsorbingManFixture.Create(hero: true, players: 2);
        f.Scheme.PlaceTokens("c_delay", delay);
        Hand(f.World, "01088", 1);
        var payment = Assert.Single(f.World.Seats[0].Hand.Cards);
        f.Reveal("04091");
        f.Finish(prompt =>
        {
            if (prompt.Cancellable) return Decision.Decline;
            if (prompt.Affordances is [{ Verb: "Order", Targets: { } targets } order])
                return new Decision(order.Id, targets.Legal.ToArray());
            if (prompt.Player == 0)
            {
                var pay = Assert.Single(prompt.Affordances, option => option.Costs is { Count: > 0 });
                int[] cards = [payment.ObjectId];
                return new Decision(pay.Id, [], cards,
                    Allocations: ResourcePayment.Allocate(Assert.Single(pay.Costs!), cards));
            }
            return Decision.Take(Assert.Single(prompt.Affordances, option => option.Costs is not { Count: > 0 }).Id);
        });
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.Equal(damage, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Fact]
    public void BallAndChainBoostRevealsAndAttachesWithPrintedModifiers()
    {
        var f = AbsorbingManFixture.Create(hero: true);
        var attachment = f.Card("04084", DeckType.BoostingArea);
        f.Runner.ExecuteBoost(f.World, attachment, 0);
        f.Finish();
        Assert.Equal(f.Villain.ObjectId, attachment.Area.Host);
        Assert.Equal(3, StateFields.Modified(f.World, f.Villain, "attack", f.World.Facts, 1));
        Assert.Equal(2, StateFields.Modified(f.World, f.Villain, "scheme", f.World.Facts, 1));
        Hand(f.World, "01090", 1);
        var payment = Assert.Single(f.World.Seats[0].Hand.Cards);
        var ability = Assert.Single(f.Runner.Actions(f.World, 0), option => option.Card == attachment.ObjectId);
        f.Runner.ExecuteAction(f.World, new PlayerAction(ability, [payment.ObjectId], []));
        f.Finish();
        Assert.Equal(DeckType.EncounterDeck, attachment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(2, StateFields.Modified(f.World, f.Villain, "attack", f.World.Facts, 1));
    }
    [Fact]
    public void StageTwoSearchRevealsPowerFromDiscard()
    {
        var f = AbsorbingManFixture.Create();
        var power = f.Card("04092", DeckType.EncounterDiscardPile);
        f.Reveal("04077");
        f.Finish(prompt => new Decision(Assert.Single(prompt.Affordances).Id, [power.ObjectId]));
        Assert.Equal(DeckType.SideSchemesArea, power.Area.Type);
        Assert.Equal(3, power.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void MainSchemeAddsOneDelayAfterVillainThreatStep()
    {
        var f = AbsorbingManFixture.Create();
        f.World.Agenda.Add(new PhaseStep(new ThreatStageOperation(ThreatStageKind.PlaceThreat), 1, 1));
        f.Finish();
        Assert.Equal(1, f.Scheme.Tokens.GetValueOrDefault("c_delay"));
        Assert.Equal(1, f.Scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void PiercingThornsWoodDiscardsAControlledCardAndNeverOffersIdentity()
    {
        var f = AbsorbingManFixture.Create(hero: true);
        f.Card("04080", DeckType.EnvironmentArea);
        var support = InPlay(f.World, "01006");
        Hand(f.World, "01003", 2);
        f.Reveal("04088");
        f.Finish(prompt =>
        {
            var option = Assert.Single(prompt.Affordances);
            Assert.Equal(support.ObjectId, option.AnchorId);
            return Decision.Take(option.Id);
        });
        Assert.Single(f.World.Seats[0].Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, support.Area.Type);
        Assert.True(DeckTypes.IsInPlay(f.World.Seats[0].IdentityCard.Area.Type));
    }
}
