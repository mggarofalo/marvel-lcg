using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Production rows on synthetic multiplayer boards; the pack is not admitted.
public sealed class AntManHelmetReviewTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:ownership-and-control.2")]
    public void HelmetRespondsOnlyToItsControllersFormChange(int helmetController)
    {
        // rr:ownership-and-control.2: "Cards enter play under their owner's control."
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = AntManF1Fixture.Runner(["12005", "12008"]);
        var world = new World(cards, 2, 241);
        var first = world.CreateSeat("first");
        first.IdentityCard = world.CreateCard("01001a,01001b", first.Hero);
        var actor = world.CreateSeat("actor");
        actor.IdentityCard = world.CreateCard("12001a,12001b,12001c", actor.Hero);
        actor.IdentityCard.TurnTo("12001c");
        for (int i = 0; i < 4; i++)
        {
            world.CreateCard("01087", first.Deck);
            world.CreateCard("01087", actor.Deck);
        }
        var helmet = world.CreateCard("12008", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(helmetController), cardOwner: helmetController));
        var resize = world.CreateCard("12005", actor.Hand);
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == resize.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, cards, runner, events);
        Assert.Equal("12001a", actor.IdentityCard.FaceId);
        if (helmetController == 1)
        {
            Assert.NotNull(prompt);
            Assert.Equal(1, prompt.Player);
            var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == helmet.ObjectId);
            Sequence.Answer(world, cards, runner, prompt, Decision.Take(offer.Id), events);
            Sequence.Finish(world, cards, runner, events);
        }
        else
        {
            Assert.Null(prompt);
        }
        Assert.Equal(helmetController == 1 ? 2 : 1, actor.Hand.Cards.Count);
        Assert.Empty(first.Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, resize.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:traits.2")]
    public void BlankingOneSourceSuppressesItsResponseButNotTheOther(bool blankIdentity)
    {
        // rr:traits.2: a trait is "not considered to be part of a card's text box".
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = AntManF1Fixture.Runner(["12001a", "12005", "12008"]);
        var world = new World(cards, 1, 241);
        var seat = world.CreateSeat("actor");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        seat.IdentityCard.TurnTo("12001c");
        var scheme = world.CreateCard("01116b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 2);
        var helmet = world.CreateCard("12008", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        for (int i = 0; i < 4; i++) world.CreateCard("01087", seat.Deck);
        var resize = world.CreateCard("12005", seat.Hand);
        world.Abilities = runner;
        // Synthetic blank tests read policy; direct blanking of these card kinds is not supported.
        var blanked = blankIdentity ? seat.IdentityCard : helmet;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, PrintedText.Blanked,
            Card: seat.IdentityCard.ObjectId, Affects: blanked.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfPhase)) { TargetIncarnation = blanked.Incarnation });
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == resize.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var prompt = Sequence.Work(world, cards, runner, events);
        Assert.NotNull(prompt);
        var expected = blankIdentity ? helmet : seat.IdentityCard;
        var excluded = blankIdentity ? seat.IdentityCard : helmet;
        Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == excluded.ObjectId);
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == expected.ObjectId);
        Sequence.Answer(world, cards, runner, prompt,
            Decision.Take(offer.Id, blankIdentity ? [] : [scheme.ObjectId], []), events);
        Sequence.Finish(world, cards, runner, events);
        Assert.Equal(blankIdentity ? 2 : 1, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(blankIdentity ? 2 : 1, seat.Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, resize.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}
