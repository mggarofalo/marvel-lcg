using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual 05014 and isolated synthetic boost variants, without public pack admission.</summary>
public sealed class IndividualBoostCancellationTests : CoreCardTestBase
{
    private static AbilityRunner Runner(string extra = "") => extra.Length == 0
        ? AuthoredCards.Runner() : new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05014","abilities":[{
          "labels":["Defense"],
          "trigger":{"event":"WhenAttackBoostCardTurnedFaceup","timing":"Interrupt","actor":"villain","form":"hero"},
          "effect":{"then":{"effect":{"cancelBoostIcons":"trigger"},
            "then":{"dealDamage":{"cards":"trigger.actor","amount":{"result":"cancelledBoostIcons"}}}}}
        }]}
        """ + extra + "]}"));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:attack-enemy-activation.step.3")]
    public void EachPhysicalCardHasItsOwnFaceupInterruptAndCancellationReceipt(int paying)
    {
        // "Resolve the following steps one card at a time". Rulings 1ea208998735ef4b,
        // 33d8c914aa86d19f: faceup interrupts precede Boost; canceled icons cannot be reused.
        var runner = Runner();
        var world = Hero("03001a", 2);
        world.Abilities = runner;
        foreach (var seat in world.Seats) world.CreateCard("01089", seat.Deck);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard("05014", world.Seats[paying].Hand);
        var secondEvent = world.CreateCard("05014", world.Seats[paying].Hand);
        var payment = world.CreateCard("01089", world.Seats[paying].Hand);
        world.CreateCard("01089", world.Seats[paying].Hand);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 8; i++) world.CreateCard("01104", deck);
        var second = world.CreateCard("01101", deck);
        var first = world.CreateCard("01102", world.AreaOf(DeckType.BoostCardsDeck,
            villain.Area.PlayArea, host: villain.ObjectId));
        Assert.Equal(2, Cards.PrintedValue(first.FaceId, "Boost", 2));
        Assert.Equal(1, Cards.PrintedValue(second.FaceId, "Boost", 2));
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: villain.ObjectId, Seat: 0));
        var prompt = Sequence.Work(world, Cards, runner, events)!;
        while (prompt is not null && !prompt.Affordances.Any(a => a.AnchorId == source.ObjectId))
        {
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
            prompt = Sequence.Work(world, Cards, runner, events)!;
        }
        Assert.True(prompt is not null, $"No offer; boosted {string.Join(",", events.OfType<BoostResolved>().Select(e => e.Icons))}");
        Assert.Contains("Boost card revealed", prompt.Description);
        Assert.DoesNotContain(BoostFaceupOperation.Faceup, prompt.Description);
        var occurrence = world.Agenda.Occurrence!;
        Assert.Equal(first.ObjectId, occurrence.Subject);
        Assert.Equal(2, occurrence.BoostIcons!.Remaining);
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        Sequence.Answer(world, Cards, runner, prompt, new Decision(offer.Id, [], [payment.ObjectId]), events);
        prompt = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(2, villain.Damage);
        Assert.Equal(paying, world.Attack!.Player);
        Assert.Equal(world.Seats[paying].IdentityCard.ObjectId, world.Attack.Defender);
        Assert.Equal(0, occurrence.BoostIcons.Remaining);
        Assert.False(world.IsOver);
        Assert.NotNull(world.Agenda.Occurrence);
        Assert.Equal(second.ObjectId, world.Agenda.Occurrence!.Subject);
        Assert.Equal(1, world.Agenda.Occurrence.BoostIcons!.Remaining);
        Assert.Throws<InvalidOperationException>(() => occurrence.BoostIcons.Cancel(world));
        Assert.Contains(prompt!.Affordances, a => a.AnchorId == secondEvent.ObjectId);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(new long[] { 0, 1 }, events.OfType<BoostResolved>().Select(e => e.Icons));
        Assert.Equal(3, world.Seats[paying].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.HandsArea, secondEvent.Area.Type);
        Assert.Null(world.Activation);
        Assert.Null(world.Attack);
        Assert.False(world.Agenda.IsBusy);
        Assert.Single(events.OfType<AttackCompleted>());
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(2, 0, 3)]
    [InlineData(0, 1, 2)]
    [Rule("rr:attack-enemy-activation.step.3")]
    [Rule("rr:amplify-icon.1")]
    public void ModifiedAndAmplifiedIconsAreCancelledBeforeTheStarAddsAnother(int modifier, int amplify, long cancelled)
    {
        // Ruling 1ea208998735ef4b: constants and amplify precede interrupts;
        // "Boost" effects resolve after the point in which icons are canceled.
        var runner = Runner("""
            ,{"card":"02030","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},
              "effect":{"grantUntil":{"card":"this","keyword":"boost_const","amount":1,"until":"EndOfActivation"}}
            }]}
            """);
        var world = Board(runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var boost = world.CreateCard("02030", world.AreaOf(DeckType.EncounterDeck));
        Assert.Equal(1, Cards.PrintedValue(boost.FaceId, "Boost", 1));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "boost_const", modifier, Affects: boost.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfActivation)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "amplify", amplify, Affects: villain.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfActivation)));
        var source = world.CreateCard("05014", world.Seats[0].Hand);
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var events = new List<GameEvent>();
        var prompt = Faceup(world, runner, source, events);
        Assert.Equal(cancelled, world.Agenda.Occurrence!.BoostIcons!.Remaining);
        Sequence.Answer(world, Cards, runner, prompt,
            new Decision(Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId).Id, [], [payment.ObjectId]), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(cancelled, villain.Damage);
        Assert.Equal(1, Assert.Single(events.OfType<BoostResolved>()).Icons);
        Assert.Equal(3, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.False(world.IsOver);
        Assert.Null(world.Activation);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void ZeroIconsCannotPayAndInvalidPaymentLeavesTheLiveReceiptUntouched()
    {
        var runner = Runner();
        var world = Board(runner);
        var source = world.CreateCard("05014", world.Seats[0].Hand);
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var zero = world.CreateCard("01104", world.AreaOf(DeckType.BoostCardsDeck,
            world.TheCardIn(DeckType.VillainArea)!.Area.PlayArea, host: world.TheCardIn(DeckType.VillainArea)!.ObjectId));
        world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        var prompt = Faceup(world, runner, source, events);
        Assert.Equal(DeckType.EncounterDiscardPile, zero.Area.Type);
        var receipt = world.Agenda.Occurrence!.BoostIcons!;
        var digest = world.Digest().Canonical();
        var pending = world.Agenda.Outstanding.ToArray();
        int count = events.Count;
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        var invalid = Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, runner, prompt,
            new Decision(offer.Id, [], [zero.ObjectId]), events));
        Assert.Contains("names a source that is not available", invalid.Message, StringComparison.Ordinal);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(count, events.Count);
        Assert.Equal(2, receipt.Remaining);
        Sequence.Answer(world, Cards, runner, prompt, new Decision(offer.Id, [], [payment.ObjectId]), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, runner, prompt,
            new Decision(offer.Id, [], [payment.ObjectId]), events));
        Assert.Equal(new long[] { 0, 0 }, events.OfType<BoostResolved>().Select(e => e.Icons));
        Assert.False(world.IsOver);
        Assert.Null(world.Activation);
    }

    [Fact]
    public void CompletionFromAnotherActivationCannotContributeToTheSameEnemy()
    {
        // Synthetic stale completion boundary: identical enemy, seat and attack
        // kind cannot substitute for the exact originating activation.
        var runner = Runner();
        var world = Board(runner);
        var source = world.CreateCard("05014", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        var boost = world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        _ = Faceup(world, runner, source, events);
        var original = world.Activation!;
        var receipt = world.Agenda.Occurrence!.BoostIcons!;
        world.Activation = original with { Id = original.Id + 1 };
        Attack.FinishBoostCard(world, Cards, runner, new PhaseStep(
            new BoostCompletionOperation(original.Enemy, true) { Icons = receipt },
            1, 3, Subject: boost.ObjectId, Seat: original.Player) { ActivationId = original.Id }, events);
        Assert.Empty(events.OfType<BoostResolved>());
        Assert.Equal(2, Modified(world, world.Cards[original.Enemy], "attack"));
        Assert.Equal(DeckType.EncounterDiscardPile, boost.Area.Type);
        Assert.Equal(original.Id + 1, world.Activation!.Id);
    }

    [Fact]
    public void UnprojectedCancellationResultFailsBeforeCommittingCosts()
    {
        // Engine choice: projection supports a direct dependent predecessor.
        // Sequential speculative result reads fail closed until implemented.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"05014","abilities":[{
              "trigger":{"event":"WhenAttackBoostCardTurnedFaceup","timing":"Interrupt","actor":"villain","form":"hero"},
              "effect":{"seq":[{"cancelBoostIcons":"trigger"},
                {"dealDamage":{"cards":"trigger.actor","amount":{"result":"cancelledBoostIcons"}}}]}
            }]}]}
            """));
        var world = Board(runner);
        var source = world.CreateCard("05014", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        var thrown = Assert.Throws<RulesNotImplementedException>(() => Faceup(world, runner, source, events));
        Assert.Contains("directly projected cancellation receipt", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(2, world.Agenda.Occurrence!.BoostIcons!.Remaining);
    }

    private static World Board(AbilityRunner runner)
    {
        var world = Hero("03001a");
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01089", world.Seats[0].Deck);
        for (int i = 0; i < 8; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private static Prompt Faceup(World world, AbilityRunner runner, Card source, List<GameEvent> events)
    {
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: world.TheCardIn(DeckType.VillainArea)!.ObjectId, Seat: 0));
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            Assert.NotNull(prompt);
            if (prompt.Affordances.Any(a => a.AnchorId == source.ObjectId)) return prompt;
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
        throw new InvalidOperationException("faceup interrupt was not offered");
    }
}



