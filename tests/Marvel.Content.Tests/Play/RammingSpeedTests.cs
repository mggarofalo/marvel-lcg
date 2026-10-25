using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical 07051 in arranged worlds; no public scenario admission claim.</summary>
public sealed class RammingSpeedTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void RevealAttachesToBulldozerAndAddsHisPrintedTwoAttack()
    {
        var (world, enemy, attachment, runner) = Board(attached: false);
        world.CreateCard("07017", world.AreaOf(DeckType.VillainArea));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 1, Subject: attachment.ObjectId, Seat: 0));
        WreckingSchemeFixture.Finish(world, runner);

        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
        Assert.Equal(enemy.ObjectId, attachment.Area.Host);
        Assert.Equal(5, StateFields.Modified(world, enemy, "attack", Cards, world.Players));
    }

    [Fact]
    public void OwnReadyAllyIsRequiredAndAttachmentDiscardsAfterTheAttack()
    {
        // 07051: "you must defend ... with an ally you control, if able."
        var (world, enemy, attachment, runner) = Board();
        var own = Ally(world, 0, "01011");
        var foreign = Ally(world, 1, "01041");
        var exhausted = Ally(world, 0, "01002");
        exhausted.Exhaust();
        var events = new List<GameEvent>();
        var prompt = Begin(world, enemy, runner, events);
        Assert.Equal(0, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.Equal(own.ObjectId, Assert.Single(prompt.Affordances).AnchorId);
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(own.ObjectId), events);
        WreckingSchemeFixture.Finish(world, runner, events);

        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.True(foreign.Ready);
        var completed = Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(own.ObjectId, completed.Defender);
        Assert.Equal(5, completed.DamageDealt);
        Assert.Empty(world.Effects.Registered);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoReadyOwnAllyAllowsOrdinaryOptionalDefense(bool exhaustedAlly)
    {
        var (world, enemy, attachment, runner) = Board();
        if (exhaustedAlly) Ally(world, 0, "01011").Exhaust();
        var prompt = Begin(world, enemy, runner, []);
        Assert.True(prompt.Cancellable);
        Assert.Contains(prompt.Affordances, offer => offer.AnchorId == world.Seats[0].IdentityCard.ObjectId);
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
    }

    [Fact]
    public void StunnedAttackKeepsAttachmentAndCannotLeakRequirementToOtherEnemy()
    {
        var (world, enemy, attachment, runner) = Board();
        Ally(world, 0, "01011");
        Statuses.Give(world, enemy, Statuses.Stunned);
        var events = new List<GameEvent>();
        Schedule(world, enemy);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
        Assert.Empty(world.Effects.Registered);
        Assert.Empty(events.OfType<AttackCompleted>());
        var other = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        Assert.True(Begin(world, other, runner, events).Cancellable);
        WreckingSchemeFixture.Finish(world, runner, events);
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
    }

    [Fact]
    public void FollowingBulldozerAttackHasNoRequirementOrAttachmentBonus()
    {
        var (world, enemy, attachment, runner) = Board();
        var own = Ally(world, 0, "01011");
        var prompt = Begin(world, enemy, runner, []);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(own.ObjectId), []);
        WreckingSchemeFixture.Finish(world, runner);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Ally(world, 0, "01041");
        Assert.True(Begin(world, enemy, runner, []).Cancellable);
        Assert.Equal(3, StateFields.Modified(world, enemy, "attack", Cards, world.Players));
    }

    private static Prompt Begin(World world, Card enemy, AbilityRunner runner, List<GameEvent> events)
    {
        Schedule(world, enemy);
        for (int step = 0; step < 10; step++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
            if (!prompt.Affordances.All(offer => offer.Timing == AbilityType.ForcedInterrupt)) return prompt;
            Sequence.Answer(world, Cards, runner, prompt,
                Decision.Take(prompt.Affordances[0].Id), events);
        }
        throw new InvalidOperationException("Attack initiation did not reach defense.");
    }

    private static void Schedule(World world, Card enemy) => world.Agenda.Add(
        new PhaseStep(new EnemyAttackOperation(), Round: 1, Number: 2, Subject: enemy.ObjectId, Seat: 0));

    private static Card Ally(World world, int player, string face) => world.CreateCard(face,
        world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));

    private static (World, Card, Card, AbilityRunner) Board(bool attached = true)
    {
        var runner = AuthoredCards.Runner();
        var world = new World(Cards, players: 2) { Abilities = runner };
        foreach (var face in new[] { "01010a", "01029a" })
        {
            var seat = world.CreateSeat(face);
            seat.IdentityCard = world.CreateCard(face, seat.Hero);
        }
        world.Seats[1].IdentityCard.Exhaust();
        var enemy = world.CreateCard("07046", world.AreaOf(DeckType.VillainArea));
        for (int index = 0; index < 8; index++)
            world.CreateCard("01097a", world.AreaOf(DeckType.EncounterDeck));
        var attachment = world.CreateCard("07051", attached
            ? world.AreaOf(DeckType.UpgradesArea, host: enemy.ObjectId)
            : world.AreaOf(DeckType.RevealingArea));
        return (world, enemy, attachment, runner);
    }
}
