using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// A card that asks twice.
/// </summary>
/// <remarks>
/// <para>
/// "You may flip to alter-ego form. <b>Choose:</b> • Exhaust Peter Parker →
/// remove Eviction Notice from the game. • Discard 1 card at random from your
/// hand. This card gains surge. Discard this obligation."
/// </para>
/// <para>
/// Two questions in a row, and until now a suspended ability had nowhere to
/// remember where it had stopped. 36 cards in the pool pair a "may" with a
/// listed choice, and every "may" is itself a question.
/// </para>
/// </remarks>
public sealed class EvictionNoticeTests
{
    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:form-change-form.3")]
    [Fact]
    public void FlippingDownDoesNotUseUpTheTurnsFlip()
    {
        // "If a card ability causes a player to change forms, it does not count
        // against the one voluntary form change the player is permitted during
        // their turn that round." So a hero flipped down by this may still flip
        // back up on their turn.
        var (world, card) = Reveal(hero: true);
        var seat = world.Seats[0];
        int before = seat.FormChangedInRound;

        AuthoredCards.Runner().Chose(world, card, 0, First, Decision.Take(0));
        var resolved = new Resolution(world, Prompt: null, Events: []);

        Assert.True(Forms.In(world, seat, Cards, Forms.AlterEgo));
        Assert.Equal(before, seat.FormChangedInRound);
        Assert.DoesNotContain(
            resolved.Information,
            signal => signal.Kind == InformationKind.Reveal);
    }

    [Rule("rr:may")]
    [Rule("rr:ability.7.1")]
    [Fact]
    public void DecliningTheFlipLeavesTheHeroStandingAndStillAsksTheSecond()
    {
        // The second option of the first choice is an empty sequence -- "you
        // may" with the "may" declined -- and the card goes on to ask the
        // question that is not optional.
        var (world, card) = Reveal(hero: true);

        AuthoredCards.Runner().Chose(world, card, 0, First, Decision.Take(1));

        Assert.True(Forms.In(world, world.Seats[0], Cards, Forms.Hero));

        // And the second question is scheduled, saying where to pick up.
        Assert.Contains(
            world.Agenda.Outstanding,
            step => step.What == Steps.ChooseOption && step.Index == Second);
    }

    [Rule("rr:choose-option.2")]
    [Fact]
    public void AnOptionThatCannotChangeFormIsNotOffered()
    {
        // An option must be able to resolve at least partially. The same
        // continuation filter applies to every authored choice; Eviction
        // Notice supplies the regression because alter-ego is already its
        // requested destination.
        var (world, card) = Reveal(hero: false);
        var waiting = Assert.Single(world.Agenda.Outstanding, step =>
            step.What == Steps.ChooseOption);

        var prompt = AuthoredCards.Runner().Choosing(
            world, card, 0, waiting.Index, waiting.Tier)!;

        var remain = Assert.Single(prompt.Affordances);
        Assert.Equal(1, remain.Id);
        Assert.Equal("Remain in your current form", remain.Description);
    }

    [Rule("rr:removed-from-the-game")]
    [Fact]
    public void ExhaustingPeterParkerRemovesTheObligation()
    {
        // Removed, not discarded: a card in the discard pile can be drawn
        // again, and an obligation that came back every time it was answered
        // would be a different card.
        var (world, card) = Reveal(hero: false);
        var identity = world.Seats[0].IdentityCard;

        var runner = AuthoredCards.Runner();
        runner.Chose(world, card, 0, First, Decision.Take(1));
        runner.Chose(world, card, 0, Second, Decision.Take(0));

        Assert.False(identity.Ready);
        Assert.Equal(DeckType.RemovedArea, card.Area.Type);
    }

    [Fact]
    public void TheOtherOptionCostsACardAndDealsAnother()
    {
        // "Discard 1 card at random from your hand. This card gains surge.
        // Discard this obligation." Three effects in one option, and the surge
        // is `rr:surge.1` -- "deal yourself 1 facedown encounter card".
        var (world, card) = Reveal(hero: false);
        int held = world.Seats[0].Hand.Cards.Count;
        int queued = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;

        var runner = AuthoredCards.Runner();
        runner.Chose(world, card, 0, First, Decision.Take(1));
        runner.Chose(world, card, 0, Second, Decision.Take(1));

        Assert.Equal(held - 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(
            queued + 1,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.True(world.Seats[0].IdentityCard.Ready);
    }

    /// <summary>
    /// Where the ability stops, for each of its two questions.
    /// </summary>
    /// <remarks>
    /// One past the step that asked, which is what a suspended ability
    /// remembers: the flip is step 0 of the sequence and resumes at 1, the
    /// choice is step 1 and resumes at 2. Written out rather than read off the
    /// agenda, because these tests answer the runner directly and never take
    /// the step off it.
    /// </remarks>
    private const int First = 1;
    private const int Second = 2;

    private static (World World, Card Card) Reveal(bool hero)
    {
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            ["Spider-Man"],
            12345);
        world.Abilities = AuthoredCards.Runner();

        if (hero)
        {
            world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        }

        var card = world.CreateCard(
            AuthoredCards.EvictionNotice, world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        AuthoredCards.Runner().WhenRevealed(world, card, 0);
        return (world, card);
    }
}
