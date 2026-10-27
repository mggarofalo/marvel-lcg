using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkObligationNemesisTests
{
    [Fact]
    public void FourExactFacesAppendAndTwoOutstandingFacesRemainUnknown()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(716, rows.Length);
        string[] expected = ["10025", "10026", "10027", "10028"];
        Assert.Equal(expected,
            rows.Skip(712).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(712), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.Equal(31, AuthoredCards.Book.Authored.Count(id => id.StartsWith("10", StringComparison.Ordinal)));
        Assert.DoesNotContain("10015", AuthoredCards.Book.Authored);
        Assert.DoesNotContain("10029", AuthoredCards.Book.Authored);
        Assert.DoesNotContain("90001", AuthoredCards.Book.Authored);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 3)]
    [InlineData(false, 3)]
    [Rule("rr:obligation.1")]
    [Rule("rr:reveal.4.1")]
    public void InnerDemonsRoutesFlipsBranchesAndDiscards(bool hero, int hand)
    {
        // "That player is considered to be revealing it"; "you" applies to its recipient.
        var (world, runner) = HulkNemesisFixture.Create();
        var seat = world.Seats[1];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("10001a,10001b", seat.Hero);
        if (!hero) seat.IdentityCard.TurnTo("10001b");
        HulkFirstSliceFixture.EmptyHand(world, 1);
        var cards = Enumerable.Range(0, hand).Select(_ => world.CreateCard("01090", seat.Hand)).ToArray();
        var source = HulkNemesisFixture.Dealt(world, "10025", 0);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        if (hero && hand > 0)
        {
            Assert.NotNull(prompt);
            Assert.Equal(1, prompt.Player);
            Assert.False(prompt.Cancellable);
            var offer = Assert.Single(prompt.Affordances);
            Assert.Equal(Math.Min(2, hand), offer.Targets!.Min);
            Sequence.Answer(world, world.Facts, runner, prompt,
                Decision.Take(offer.Id, cards.Take(2).Select(c => c.ObjectId).ToArray(), []), []);
        }
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(hero ? "10001b" : "10001a", seat.IdentityCard.FaceId);
        Assert.Equal(hero, seat.IdentityCard.Ready);
        Assert.Equal(hero ? Math.Max(0, hand - 2) : hand, seat.Hand.Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("01090", false, false)]
    [InlineData("01044", false, false)]
    [InlineData("01089", false, false)]
    [InlineData("01090", true, false)]
    [InlineData("01090", false, true)]
    [Rule("rr:wild-resource.3")]
    [Rule("rr:stun-stunned.7")]
    public void AbominationCountsPrintedPhysicalAfterCompletedAttack(string top, bool defend, bool stunned)
    {
        // Outside costs, "wild resources cannot be interpreted as any of the other resource types."
        // A stun replacement means the character "is not considered to have attacked."
        var (world, runner) = HulkNemesisFixture.Create();
        Forms.Change(world.Seats[1], world.Facts);
        var enemy = world.CreateCard("10026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var card = world.CreateCard(top, world.Seats[1].Deck);
        var ally = world.CreateCard("01050", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)));
        if (stunned) Statuses.Give(world, enemy, Statuses.Stunned);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), Round: 1, Number: 2,
            Subject: enemy.ObjectId, Seat: 1));
        if (defend) HulkNemesisFixture.Defend(world, runner, 1, ally);
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(stunned ? DeckType.PlayerDeck : DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(stunned ? 0 : (defend ? 0 : 3) + (top == "01090" ? 2 : 0), world.Seats[1].IdentityCard.Damage);
        Assert.Equal(defend ? 3 : 0, ally.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void TotalDestructionEntersWithPerPlayerThreatAndLocksWhileAbominationIsAnywhereInPlay()
    {
        var (world, runner) = HulkNemesisFixture.Create();
        var scheme = HulkNemesisFixture.Dealt(world, "10027");
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(4, scheme.Tokens["k_threat"]);
        Assert.Equal(1, Deal.HazardIcons(world, world.Facts));
        Assert.True(runner.CanRemoveThreat(world, scheme));
        var enemy = world.CreateCard("10026", world.Seats[1].Deck);
        Assert.True(runner.CanRemoveThreat(world, scheme));
        World.MoveToTop(enemy, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Assert.False(runner.CanRemoveThreat(world, scheme));
        World.MoveToTop(enemy, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.True(runner.CanRemoveThreat(world, scheme));
    }
}
