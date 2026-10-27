using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeThoughtcastingCardTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:choose-game-element.1")]
    [Rule("rr:printed")]
    public void ResolvingPlayerChoosesHighestPrintedCostTieAndSameReceiptSetsPenalty(bool alterEgo, bool second)
    {
        // The player resolving an ability chooses its game elements; "printed" means physically printed.
        var world = DoctorStrangeNemesisFixture.Board();
        if (alterEgo) world.Seats[1].IdentityCard.TurnTo("09001b");
        world.FirstPlayer = 0;
        var low = world.CreateCard("01093", world.Seats[1].Hand);
        var left = world.CreateCard("01005", world.Seats[1].Hand);
        var right = world.CreateCard("01005", world.Seats[1].Hand);
        CardPayment.ReduceNextCardCost(world, world.Seats[1].IdentityCard, 1, 3);
        Assert.Equal(0, CardPayment.CostOf(world, world.Facts, world.Seats[1], left).Amount);
        var foreign = world.CreateCard("01091", world.Seats[0].Hand);
        var card = world.CreateCard("09031", world.AreaOf(DeckType.RevealingArea));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard), 1, 1, Subject: card.ObjectId, Seat: 1));
        var prompt = Sequence.Work(world, world.Facts, world.Abilities, [])!;
        Assert.Equal(1, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Equal(new[] { left.ObjectId, right.ObjectId }, prompt.Affordances.Select(offer => offer.Id));
        Sequence.Answer(world, world.Facts, world.Abilities, prompt, Decision.Take(second ? right.ObjectId : left.ObjectId), []);
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        Assert.Equal(DeckType.DiscardPile, (second ? right : left).Area.Type);
        Assert.Equal(DeckType.HandsArea, (second ? left : right).Area.Type);
        Assert.Same(world.Seats[1].Hand, low.Area);
        Assert.Same(world.Seats[0].Hand, foreign.Area);
        Assert.Equal(alterEgo ? 0 : 3, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(alterEgo ? 3 : 0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(false, "01088")]
    [InlineData(true, "01088")]
    [InlineData(false, "14006")]
    [InlineData(true, "14006")]
    [Rule("rr:non-numerical-variable")]
    public void EmptyHandZeroCostAndUndefinedXAddNoPenalty(bool alterEgo, string face)
    {
        // An undefined non-numerical variable is "equal to 0".
        var world = DoctorStrangeNemesisFixture.Board();
        if (alterEgo) world.Seats[1].IdentityCard.TurnTo("09001b");
        Card? held = face.Length == 0 ? null : world.CreateCard(face, world.Seats[1].Hand);
        var card = world.CreateCard("09031", world.AreaOf(DeckType.RevealingArea));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard), 1, 1, Subject: card.ObjectId, Seat: 1));
        if (held is not null)
        {
            var prompt = Sequence.Work(world, world.Facts, world.Abilities, [])!;
            Assert.Equal(1, prompt.Player);
            Assert.Equal(held.ObjectId, Assert.Single(prompt.Affordances).Id);
            Sequence.Answer(world, world.Facts, world.Abilities, prompt, Decision.Take(held.ObjectId), []);
        }
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        Assert.Empty(world.Seats[1].Hand.Cards);
        if (held is not null) Assert.Equal(DeckType.DiscardPile, held.Area.Type);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
    }
}


