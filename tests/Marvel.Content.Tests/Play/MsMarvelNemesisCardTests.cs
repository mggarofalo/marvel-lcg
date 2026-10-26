using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelNemesisCardTests : ChoosingCardsTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:you-your")]
    public void EdisonProtectionFollowsHisEngagedPlayerAndUpdatesAfterDeparture(int attackingPlayer)
    {
        // Printed 05027: "cannot take damage while you are engaged with another minion."
        // rr:you-your.1: interpret "you" as the identity whenever possible.
        // Vendored ruling:3aba0c6abbd5c2ad: all players are prevented while his engaged player has another minion.
        var world = Deal("spider_man", "captain_marvel");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var edison = world.CreateCard("05027", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var source = world.Seats[attackingPlayer].IdentityCard;
        Assert.True(runner.CanTakeDamage(world, edison, source));
        var foreign = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.True(runner.CanTakeDamage(world, edison, source));
        var guard = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Assert.False(runner.CanTakeDamage(world, edison, source));
        Discard.Card(world, guard, "fixture", []);
        Assert.True(runner.CanTakeDamage(world, edison, source));
        Assert.Equal(DeckType.EngagedEnemiesArea, foreign.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:obligation.1")]
    public void HomeByDawnDiscardsOnlyTheResolvingPlayersPersonaOrSurges(bool hasOwnPersona)
    {
        // "Abilities on obligations that use the words 'you' or 'your' apply only to the player whose play area the obligation is in."
        var world = Deal("spider_man", "captain_marvel");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var foreign = world.CreateCard("05006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ordinary = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        Card? own = hasOwnPersona ? world.CreateCard("05008", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1)) : null;
        var obligation = world.CreateCard("05025", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)));
        int dealt = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count;
        runner.ExecuteReveal(world, obligation, 1);
        Choose(world, obligation, 1);
        Choose(world, obligation, 1);
        if (hasOwnPersona) Choose(world, obligation, own!.ObjectId);
        Assert.Equal(DeckType.EncounterDiscardPile, obligation.Area.Type);
        Assert.Equal(DeckType.SupportsArea, foreign.Area.Type);
        Assert.Equal(DeckType.SupportsArea, ordinary.Area.Type);
        if (hasOwnPersona) Assert.Equal(DeckType.DiscardPile, own!.Area.Type);
        Assert.Equal(dealt + (hasOwnPersona ? 0 : 1), world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count);
        Assert.True(world.Seats[1].IdentityCard.Ready);
    }

    [Fact]
    [Rule("rr:exhausted.1")]
    public void RemovingHomeByDawnRequiresExhaustingTheAlterEgo()
    {
        // "An exhausted card cannot be exhausted again until it is ready."
        var world = Deal();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var obligation = world.CreateCard("05025", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        runner.ExecuteReveal(world, obligation, 0);
        Choose(world, obligation, 1);
        Choose(world, obligation, 0);
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(DeckType.RemovedArea, obligation.Area.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:form-change-form.4")]
    public void OptionalFlipControlsWhetherKamalaCanPayRemoval(bool flip)
    {
        // "While a player is in hero form, card abilities that interact with their alter-ego do not interact with their identity."
        var world = Deal("ms_marvel", "captain_marvel");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var identity = world.Seats[0].IdentityCard;
        identity.TurnTo("05001a");
        var obligation = world.CreateCard("05025", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        runner.ExecuteReveal(world, obligation, 0);
        Choose(world, obligation, flip ? 0 : 1);
        var prompt = runner.Choosing(world, AbilityContinuationFixtures.Pending(world, obligation))!;
        Assert.Equal(flip, prompt.Affordances.Any(offer => offer.Id == 0));
        Assert.Equal(flip ? "05001b" : "05001a", identity.FaceId);
        Choose(world, obligation, flip ? 0 : 1);
        Assert.Equal(flip ? DeckType.RemovedArea : DeckType.EncounterDiscardPile, obligation.Area.Type);
        Assert.Equal(!flip, identity.Ready);
    }

    [Fact]
    [Rule("rr:exhausted.1")]
    public void ExhaustedKamalaCannotRemoveHomeByDawn()
    {
        // "An exhausted card cannot be exhausted again until it is ready."
        var world = Deal("ms_marvel");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Seats[0].IdentityCard.Exhaust();
        var obligation = world.CreateCard("05025", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        runner.ExecuteReveal(world, obligation, 0);
        Choose(world, obligation, 1);
        var prompt = runner.Choosing(world, AbilityContinuationFixtures.Pending(world, obligation))!;
        Assert.DoesNotContain(prompt.Affordances, offer => offer.Id == 0);
        Choose(world, obligation, 1);
        Assert.Equal(DeckType.EncounterDiscardPile, obligation.Area.Type);
        Assert.False(world.Seats[0].IdentityCard.Ready);
    }

    [Fact]
    [Rule("rr:obligation.4")]
    [Rule("rr:reveal.4.1")]
    public void AnotherPlayersRevealGivesHomeByDawnToKamalaAndAsksHer()
    {
        // "that player is considered to be revealing it" when the card names a player to give it to.
        var world = Deal("captain_marvel", "ms_marvel");
        world.Seats[1].IdentityCard.TurnTo("05001a");
        var obligation = world.CreateCard("05025", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: obligation.ObjectId, Seat: 0));
        var prompt = Sequence.Work(world, Cards, AuthoredCards.Runner(), [])!;
        Assert.Equal(1, prompt.Player);
        Assert.Equal(DeckType.ObligationsArea, obligation.Area.Type);
        Assert.Equal(PlayArea.Of(1), obligation.Area.PlayArea);
        Assert.Equal(2, prompt.Affordances.Count);
        Assert.Equal("05001a", world.Seats[1].IdentityCard.FaceId);
    }

    private static void Choose(World world, Card source, int id)
    {
        var runner = AuthoredCards.Runner();
        var pending = AbilityContinuationFixtures.Pending(world, source);
        var prompt = runner.Choosing(world, pending)!;
        Assert.Contains(prompt.Affordances, offer => offer.Id == id);
        runner.Chose(world, pending, Decision.Take(id));
    }
}
