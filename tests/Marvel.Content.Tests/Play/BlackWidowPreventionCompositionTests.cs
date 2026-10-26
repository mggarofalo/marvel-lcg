using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
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

public sealed class BlackWidowPreventionCompositionTests
{
    [Fact]
    [Rule("rr:guard.1")]
    [Rule("rr:labeled-ability.2")]
    public void AttacrobaticsUnderGuardCancelsIconsWithoutAttackingTheVillain()
    {
        // Guard: "The engaged player cannot attack any villain."
        // Ruling 0fc2a7cc9197e127 permits icon cancellation, with no attacked enemy.
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08006");
        f.World.CreateCard("01101", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.World.CreateCard("01102", f.World.AreaOf(DeckType.EncounterDeck));
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: f.Villain.ObjectId, Seat: 0));
        f.Use(f.Find(source), source);
        f.Finish();
        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(0, Assert.Single(f.Events.OfType<BoostResolved>()).Icons);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(f.World.IsOver);
        Assert.Null(f.World.Activation);
    }

    [Theory]
    [InlineData(true, 0, "01104")]
    [InlineData(false, 1, "01104")]
    [InlineData(true, 1, "01101")]
    public void GrapplingHookDoesNotCancelAnotherSeatsRevealAlterEgoOrMinion(bool hero, int seat, string face)
    {
        var f = new BlackWidowPreventionFixture(hero);
        var source = f.Upgrade("08008");
        f.Villain.TakeDamage(4);
        var card = f.World.CreateCard(face, f.World.AreaOf(DeckType.RevealingArea));
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 2, Subject: card.ObjectId, Seat: seat));
        f.Finish(source);
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        Assert.Equal(face == "01101" ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Equal(face == "01101" ? 4 : 0, f.Villain.Damage);
        Assert.False(f.World.IsOver);
    }

    [Fact]
    [Rule("rr:resolve.2")]
    public void GrapplingHookCancellationResolvesActualPreparationAndReadiesThroughSynthSuit()
    {
        // "An ability is resolved when it is triggered and one or more of its effects resolve."
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08008");
        var suit = f.Upgrade("08009");
        f.Hero.Exhaust();
        Statuses.Give(f.World, f.Hero, Statuses.Stunned);
        var treachery = f.World.CreateCard("01190", f.World.AreaOf(DeckType.RevealingArea));
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 2, Subject: treachery.ObjectId, Seat: 1));
        f.Use(f.Find(source), source);
        var completion = f.Find(suit);
        var receipt = Assert.IsType<ResolvedPreparationAbility>(f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Equal("08008", receipt.Face);
        Assert.Equal(1, receipt.Controller);
        Assert.Contains(completion.Affordances, a => a.AnchorId == f.Hero.ObjectId);
        f.Use(completion, suit);
        f.Finish();
        Assert.True(f.Hero.Ready);
        Assert.False(suit.Ready);
        Assert.True(Statuses.Has(f.World, f.Hero, Statuses.Stunned));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, treachery.Area.Type);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.False(f.World.IsOver);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:labeled-ability.2")]
    public void PartialAttackHasOneActorNoEnemyAndCompletesItsPreparation(bool attack)
    {
        // "that ability is considered to be an attack made by that player's identity."
        // Synthetic observers distinguish "after you attack" from an attacked villain.
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var observer = book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "01092")!;
        observer["abilities"] = JsonNode.Parse("""
            [{"trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"you"},
              "effect":{"draw":{"player":"you","count":1}}},
             {"trigger":{"event":"WhenAttackEnds","timing":"Response","actor":"you","target":"villain"},
              "effect":{"draw":{"player":"you","count":2}}}]
            """);
        var f = new BlackWidowPreventionFixture(runner: new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString())));
        var source = f.Upgrade("08006");
        var suit = f.Upgrade("08009");
        f.World.Effects.Register(new(Marvel.Rules.Timing.EffectSource.LastingEffect,
            "retaliate", 2, Affects: f.Villain.ObjectId));
        f.Hero.Exhaust();
        var witness = f.World.CreateCard("01092", f.World.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        f.World.CreateCard("01101", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.World.CreateCard("01102", f.World.AreaOf(DeckType.EncounterDeck));
        f.World.Agenda.Add(new PhaseStep(attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 2, Subject: f.Villain.ObjectId, Seat: 0));
        f.Use(f.Find(source), source);
        var completion = f.Work();
        Assert.Contains(completion.Affordances, a => a.AnchorId == suit.ObjectId);
        Assert.Equal("08006", f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility!.Face);
        f.Use(completion, suit);
        var prompt = f.Find(witness);
        var occurrence = f.World.Agenda.Occurrence!;
        Assert.Equal(f.Hero.ObjectId, occurrence.Actor);
        Assert.Equal(-1, occurrence.Target);
        Assert.Null(occurrence.TargetFacts);
        Assert.Single(prompt.Affordances, a => a.AnchorId == witness.ObjectId);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        int hand = f.World.Seats[1].Hand.Cards.Count;
        f.Use(prompt, witness);
        f.Finish();
        Assert.Equal(hand + 1, f.World.Seats[1].Hand.Cards.Count);
        Assert.True(f.Hero.Ready);
        Assert.False(suit.Ready);
        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(0, f.Hero.Damage);
        var starts = f.Events.OfType<AttackBoundary>().Where(e => e.Source == f.Hero.ObjectId && e.Started && !e.Resumed);
        Assert.Equal(-1, Assert.Single(starts).Target);
        Assert.All(f.Events.OfType<AttackBoundary>(), e => Assert.DoesNotContain(-1, e.Subjects!.Keys));
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:labeled-ability.6")]
    public void StunCancelsBothSentencesAfterDiscardEvenWithGuard(bool guard)
    {
        // "the entire ability (except for its costs) is canceled."
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08006");
        Statuses.Give(f.World, f.Hero, Statuses.Stunned);
        if (guard) f.World.CreateCard("01101", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.World.CreateCard("01102", f.World.AreaOf(DeckType.EncounterDeck));
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: f.Villain.ObjectId, Seat: 0));
        f.Use(f.Find(source), source);
        f.Finish();
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(Statuses.Has(f.World, f.Hero, Statuses.Stunned));
        Assert.Equal(2, Assert.Single(f.Events.OfType<BoostResolved>()).Icons);
        Assert.Equal(0, f.Villain.Damage);
        Assert.DoesNotContain(f.Events.OfType<AttackBoundary>(), e => e.Source == f.Hero.ObjectId);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ability.3")]
    public void ZeroIconsCannotFundAnOtherwiseEffectlessAttack(bool guard)
    {
        // "that ability can only be initiated if it has at least one valid target."
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08006");
        if (guard) f.World.CreateCard("01101", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.World.CreateCard("01104", f.World.AreaOf(DeckType.EncounterDeck));
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: f.Villain.ObjectId, Seat: 0));
        Prompt? prompt;
        while ((prompt = Sequence.Work(f.World, f.World.Facts, f.Runner, f.Events)) is not null)
        {
            Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == source.ObjectId);
            f.Answer(prompt, Decision.Decline);
        }
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        Assert.Equal(0, f.Villain.Damage);
        Assert.False(f.World.Agenda.IsBusy);
    }
}

