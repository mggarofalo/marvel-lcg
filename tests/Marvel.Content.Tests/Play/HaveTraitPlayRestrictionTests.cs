using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Research catalog prerequisites do not admit Doctor Strange's executable content.
public sealed class HaveTraitPlayRestrictionTests : CoreCardTestBase
{
    [Theory]
    [InlineData("03025", "Play only if your identity has the Avenger trait.", "AVENGER")]
    [InlineData("09026", "Play only if you have the Mystic trait.", "MYSTIC")]
    public void CatalogReadsBothPrintedIdentityTraitWordings(string face, string text, string trait)
    {
        Assert.StartsWith(text, Cards.Text(face));
        Assert.Equal(trait, Cards.RequiredIdentityTrait(face));
        Assert.Null(Cards.RequiredControlledCharacterTrait(face));
    }

    [Theory]
    [InlineData("Play only if you control a Spy character.")]
    [InlineData("You get +1 hand size while you have the Mystic trait.")]
    [InlineData("Response: Play only if you have the Mystic trait.")]
    [InlineData("Play only if you have the Mystic trait while in hero form.")]
    [InlineData("Play only if you have the trait.")]
    public void OtherSentencesDoNotCreateAnIdentityPrerequisite(string text)
    {
        // Synthetic catalog text tests the exact supported sentence boundary.
        var catalog = CardCatalog.Parse(JsonSerializer.Serialize(new
        {
            cards = new[] { new { card_id = "synthetic", text_plain = text } },
        }));
        Assert.Null(catalog.RequiredIdentityTrait("synthetic"));
    }

    [Theory]
    [InlineData("09001a", 0)]
    [InlineData("09001b", 0)]
    [InlineData("09001a", 1)]
    [InlineData("09001b", 1)]
    [Rule("rr:you-your")]
    [Rule("rr:identity.4")]
    [Rule("rr:traits.1")]
    public void EitherMysticFormAndActingSeatUseCurrentTraits(string face, int player)
    {
        // rr:you-your: if "you" can refer to the identity, "it must be resolved as such."
        // rr:identity.4: "The faceup side of the identity card is considered to be in play."
        // rr:traits.1: abilities "reference cards that possess or lack specific traits."
        var world = new World(Cards, 2);
        for (int index = 0; index < 2; index++)
        {
            var created = world.CreateSeat($"p{index}");
            created.IdentityCard = world.CreateCard(index == player ? face : "01001a", created.Hero);
        }
        var seat = world.Seats[player];
        var card = world.CreateCard("09026", seat.Hand);
        world.CreateCard("01088", seat.Hand);
        Assert.NotNull(CardPayment.Price(world, Cards, seat, card));
        var loss = world.Effects.Register(new(EffectSource.LastingEffect,
            Characteristics.Lost + Traits.Granted + "MYSTIC", Affects: seat.IdentityCard.ObjectId));
        Assert.Null(CardPayment.Price(world, Cards, seat, card));
        loss.Dispose();
        Assert.NotNull(CardPayment.Price(world, Cards, seat, card));
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.AsideDeck, PlayArea.Of(player)));
        seat.IdentityCard = world.CreateCard("01001b", seat.Hero);
        Assert.Null(CardPayment.Price(world, Cards, seat, card));
        var grant = world.Effects.Register(new(EffectSource.LastingEffect,
            Traits.Granted + "MYSTIC", Affects: seat.IdentityCard.ObjectId));
        Assert.NotNull(CardPayment.Price(world, Cards, seat, card));
        grant.Dispose();
        Assert.Null(CardPayment.Price(world, Cards, seat, card));
    }

    [Fact]
    [Rule("rr:you-your")]
    public void AnotherIdentityOrControlledMysticAllyDoesNotSupplyYourTrait()
    {
        // rr:you-your: if "you" can refer to the identity, "it must be resolved as such."
        var world = Hero("09001a", 2);
        var seat = world.Seats[1];
        var card = world.CreateCard("09026", seat.Hand);
        world.CreateCard("01088", seat.Hand);
        world.CreateCard("09002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)));
        Assert.Null(CardPayment.Price(world, Cards, seat, card));
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    public void RejectedNonMysticPlayAndQueriesPreserveExecution()
    {
        // Step 2: "Check play restrictions: can the card be played, or the ability initiated, at this time?"
        var runner = AuthoredCards.Runner();
        var (game, world) = Playing(_ => { }, abilities: runner);
        var (controlGame, control) = Playing(_ => { }, abilities: runner);
        var card = world.CreateCard("09026", world.Seats[0].Hand);
        control.CreateCard("09026", control.Seats[0].Hand);
        var sameCost = world.CreateCard("01093", world.Seats[0].Hand);
        var expectedCard = control.CreateCard("01093", control.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var expectedPayment = control.CreateCard("01088", control.Seats[0].Hand);
        var pending = game.Pending;
        string before = Snapshot(world);
        Assert.Equal(2, CardPayment.CostOf(world, Cards, world.Seats[0], card).Amount);
        Assert.Equal(2, CardPayment.CostOf(world, Cards, world.Seats[0], sameCost).Amount);
        Assert.NotNull(CardPayment.Price(world, Cards, world.Seats[0], sameCost));
        for (int i = 0; i < 3; i++) Assert.Null(CardPayment.Price(world, Cards, world.Seats[0], card));
        var events = new List<GameEvent>();
        Assert.Throws<RulesNotImplementedException>(() =>
            CardPlay.Play(world, Cards, runner, world.Seats[0], card, [payment.ObjectId], events));
        Assert.Equal(before, Snapshot(world));
        Assert.Same(pending, game.Pending);
        Assert.Empty(events);
        var expectedEvents = new List<GameEvent>();
        CardPlay.Play(world, Cards, runner, world.Seats[0], sameCost, [payment.ObjectId], events);
        CardPlay.Play(control, Cards, runner, control.Seats[0], expectedCard,
            [expectedPayment.ObjectId], expectedEvents);
        Assert.Equal(DeckType.UpgradesArea, sameCost.Area.Type);
        Assert.Equal(expectedEvents.Select(Describe), events.Select(Describe));
        Assert.Equal(Snapshot(control), Snapshot(world));
        Assert.Equal(controlGame.Pending?.Label, game.Pending?.Label);
    }

    private static string Describe(GameEvent item) => JsonSerializer.Serialize(item, item.GetType());

    private static string Snapshot(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(),
        Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        world.Agenda.Stage, world.Agenda.IsBusy, Effects = world.Effects.Registered,
        Frames = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject }),
    });
}
