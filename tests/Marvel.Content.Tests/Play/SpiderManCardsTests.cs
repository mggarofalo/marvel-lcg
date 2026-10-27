using Marvel.Rules.Setup;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SpiderManCardsTests : SpiderManCardTestBase
{
    [Rule("rr:printed")]
    [Fact]
    public void BlackCatRecoversOnlyCardsWithAPrintedMentalResource()
    {
        // "Add each card with a printed mental resource discarded this way."
        var world = Deal();
        var runner = AuthoredCards.Runner();
        var cat = world.CreateCard(
            AuthoredCards.BlackCat,
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var energy = world.CreateCard("01088", world.Seats[0].Deck);
        var genius = world.CreateCard("01089", world.Seats[0].Deck);
        var occurrence = new Occurrence(
            1, [Steps.CardPlayed], Subject: cat.ObjectId, Player: 0);

        var events = runner.ResolveWindow(world, occurrence, new PendingAbility(cat.ObjectId, AbilityType.ForcedResponse, 0), new Decision(cat.ObjectId, [], [])).Events;
        var receipt = Assert.Single(events.OfType<AbilityWindowResult>());
        Assert.Equal(cat.ObjectId, receipt.Source);
        Assert.Equal(occurrence.Id, receipt.Occurrence);
        Assert.NotEmpty(events.OfType<CardsMoved>());
        Assert.All(events.OfType<CardsMoved>(), result =>
        {
            Assert.Equal(receipt.Source, result.AbilitySource);
            Assert.Equal(receipt.Occurrence, result.AbilityOccurrence);
        });
        var resolved = new Resolution(world, Prompt: null, Events: []);

        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(DeckType.HandsArea, genius.Area.Type);
        Assert.Contains(
            resolved.Information,
            signal => signal.Kind == InformationKind.Reveal);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:event")]
    [Rule("rr:initiating-abilities.step.1")]
    [Rule("rr:play-put-into-play.2")]
    [Fact]
    public void SwingingWebKickPaysChoosesDamagesAndThenDiscards()
    {
        // An Action on an event is played from hand. It sits faceup and out of
        // play while its target question is waiting, then goes to discard after
        // damage.
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        EmptyHand(world);
        var kick = world.CreateCard(AuthoredCards.SwingingWebKick, world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(runner.Actions(world, 0));
        Assert.True(runner.Describe(world, action).PlaysCard);

        var events = runner.ExecuteAction(world, new PlayerAction(action, [genius.ObjectId, energy.ObjectId], [])).Events.ToList();
        Assert.Equal(DeckType.RevealingArea, kick.Area.Type);
        Assert.True(kick.FaceUp);
        Assert.False(DeckTypes.IsInPlay(kick.Area.Type));
        Assert.True(world.Agenda.Occurrence!.Is(Steps.CardPlayed));

        var waiting = Assert.Single(world.Agenda.Outstanding);
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        var target = Assert.Single(prompt.Affordances);
        Assert.Equal("01094", target.Label);
        Assert.Equal("Rhino", target.DisplayLabel);
        Assert.Contains("14/14 → 6/14 HP", target.Description);
        Sequence.Answer(
            world, Cards, runner, prompt, Decision.Take(villain.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);

        Assert.Equal(8, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, kick.Area.Type);
        var damage = Assert.Single(events.OfType<FieldSet>(), change =>
            change.Card == villain.ObjectId && change.Verb == BasicPowers.AttackVerb);
        Assert.Equal(Steps.TurnAction, damage.Trigger);
    }

    [Rule("rr:villain-defeat.3")]
    [Fact]
    public void LethalAttackPreviewNamesTheNextVillainStage()
    {
        var world = Deal(hero: true);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        villain.TakeDamage(6);
        var hero = world.Seats[0].IdentityCard;

        string preview = Damage.PreviewAttack(
            world, Cards, hero, hero, villain, 8);

        Assert.Equal(
            "Rhino stage 1: 8/14 → defeated · Rhino stage 2 will enter at 15/15 HP",
            preview);
    }

    [Rule("rr:event.4")]
    [Rule("rr:event.2")]
    [Rule("rr:cancel.2")]
    [Rule("rr:cancel.3")]
    [Rule("rr:stun-stunned.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.step.6")]
    [Fact]
    public void StunCancelsSwingingWebKickAfterItsCostsArePaid()
    {
        // "Only the effects are prevented from initiating, and do not resolve."
        // "If the effects of an event card are canceled, the card is still
        // considered played, and it is discarded." Stun replaces the attack
        // after payment, so the canceled effect requests no enemy target.
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        EmptyHand(world);
        var kick = world.CreateCard(AuthoredCards.SwingingWebKick, world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, hero, Statuses.Stunned);

        var action = Assert.Single(runner.Actions(world, 0));
        runner.ExecuteAction(world, new PlayerAction(action, [genius.ObjectId, energy.ObjectId], []));
        Assert.Null(Sequence.Work(world, Cards, runner, []));

        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.Equal(0, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, kick.Area.Type);
        Assert.Equal(DeckType.DiscardPile, genius.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
    }

    [Rule("rr:replacement-effect")]
    [Rule("rr:damage.step.1")]
    [Fact]
    public void SwingingWebKickPreviewNamesArmoredRhinoSuitAsTheReplacement()
    {
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        EmptyHand(world);
        var kick = world.CreateCard(AuthoredCards.SwingingWebKick, world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard(
            AuthoredCards.ArmoredSuit,
            world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId));

        var action = runner.Actions(world, 0).Single(ability => ability.Card == kick.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [genius.ObjectId, energy.ObjectId], []));

        string? preview = Assert.Single(
            Sequence.Work(world, Cards, runner, [])!.Affordances).Description;

        Assert.Contains("14/14 → 14/14 HP", preview);
        Assert.Contains(
            "Armored Rhino Suit takes the damage instead and will be discarded", preview);
        Assert.DoesNotContain("excess", preview, StringComparison.OrdinalIgnoreCase);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void GuardRestrictsSwingingWebKickToMinions()
    {
        // "While any minions with this keyword are engaged with a player,
        // that player cannot attack villains without this keyword."
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        EmptyHand(world);
        var kick = world.CreateCard(AuthoredCards.SwingingWebKick, world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var guard = world.CreateCard(
            "01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        var action = runner.Actions(world, 0).Single(ability => ability.Card == kick.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [genius.ObjectId, energy.ObjectId], []));
        var offers = Sequence.Work(world, Cards, runner, [])!.Affordances;

        Assert.Equal([guard.ObjectId], offers.Select(offer => offer.AnchorId));
        Assert.DoesNotContain(offers, offer => offer.AnchorId == villain.ObjectId);
    }

    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void AQueuedCardAttackCanBeResolvedByAReconstructedRunner()
    {
        // The engine chooses a stable authored address for an agenda payload;
        // an in-memory runner instance is not part of saved game state.
        var world = Deal(hero: true);
        var firstRunner = AuthoredCards.Runner();
        world.Abilities = firstRunner;
        EmptyHand(world);
        var kick = world.CreateCard(AuthoredCards.SwingingWebKick, world.Seats[0].Hand);
        var genius = world.CreateCard("01089", world.Seats[0].Hand);
        var energy = world.CreateCard("01088", world.Seats[0].Hand);
        var villain = world.TheCardIn(DeckType.VillainArea)!;

        var action = Assert.Single(firstRunner.Actions(world, 0));
        firstRunner.ExecuteAction(world, new PlayerAction(action, [genius.ObjectId, energy.ObjectId], []));
        var prompt = Sequence.Work(world, Cards, firstRunner, [])!;
        Sequence.Answer(
            world, Cards, firstRunner, prompt, Decision.Take(villain.ObjectId), []);

        var reconstructed = AuthoredCards.Runner();
        world.Abilities = reconstructed;
        Sequence.Finish(world, Cards, reconstructed, []);

        Assert.Equal(8, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, kick.Area.Type);
    }

}
