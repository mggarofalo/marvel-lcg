using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowPreventionCardTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [Rule("rr:prevent.1.1")]
    public void DefensiveStancePreventsOnlyThreeAndOffersActualCompletionResponses(int amount)
    {
        // Prevention reduces damage "takes", but "dealt" is not reduced.
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08032");
        var suit = f.Upgrade("08009");
        f.Hero.Exhaust();
        f.World.Effects.Register(new(EffectSource.LastingEffect, "attack", amount - 2, Affects: f.Villain.ObjectId));
        f.World.CreateCard("01104", f.World.AreaOf(DeckType.EncounterDeck));
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: f.Villain.ObjectId, Seat: 1));
        var prompt = f.Find(source);
        Assert.Equal(1, prompt.Player);
        var digest = f.World.Digest().Canonical();
        long random = f.World.Random.Generator.WordsConsumed;
        var pending = f.World.Agenda.Outstanding.ToArray();
        var offer = Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId);
        Assert.ThrowsAny<Exception>(() => f.Answer(prompt, Decision.Take(int.MaxValue)));
        Assert.Equal(digest, f.World.Digest().Canonical());
        Assert.Equal(random, f.World.Random.Generator.WordsConsumed);
        Assert.Equal(pending, f.World.Agenda.Outstanding);
        f.Use(prompt, source);
        var completion = f.Find(suit);
        var receipt = Assert.IsType<ResolvedPreparationAbility>(f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Equal("08032", receipt.Face);
        Assert.Equal(1, receipt.Controller);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Contains(completion.Affordances, a => a.AnchorId == f.Hero.ObjectId);
        f.Use(completion, suit);
        Assert.True(f.Hero.Ready);
        Assert.False(suit.Ready);
        f.Finish();
        Assert.Equal(Math.Max(0, amount - 3), f.Hero.Damage);
        Assert.Equal(amount, Assert.Single(f.Events.OfType<AttackCompleted>()).DamageDealt);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DefensiveStanceCannotProtectAlterEgoOrAnotherIdentityOrAlly(bool hero, bool ally)
    {
        var f = new BlackWidowPreventionFixture(hero);
        var source = f.Upgrade("08032");
        var target = !hero ? f.Hero : ally
            ? f.World.CreateCard("01058", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1))
            : f.World.Seats[0].IdentityCard;
        f.World.Effects.Register(new(EffectSource.LastingEffect, "attack", -1, Affects: f.Villain.ObjectId));
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(!hero ? target.ObjectId : -1)
            { DealBoostCards = false }, 1, 2, Subject: f.Villain.ObjectId, Seat: !hero || ally ? 1 : 0));
        if (ally) f.FinishDefendingWith(target, source); else f.Finish(source);
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        Assert.Equal(1, target.Damage);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.False(f.World.Agenda.IsBusy);
        Assert.False(f.World.IsOver);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 5)]
    [Rule("rr:prevent.2")]
    public void CounterintelligencePreventsUpToThreeOnTheMainSchemeInEitherForm(bool hero, int amount)
    {
        // "Reduce the amount of threat being assigned before it is placed on the scheme."
        var f = new BlackWidowPreventionFixture(hero);
        var source = f.Upgrade("08017");
        var main = f.World.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");
        Threat.Schedule(f.World, new([main], f.Villain, amount, ThreatCause.CardAbility, "actual-card probe", 0));
        var prompt = f.Find(source);
        Assert.Equal(1, prompt.Player);
        var receipt = f.World.Agenda.Occurrence!.Threat!;
        Assert.Equal(amount, receipt.Assigned);
        f.Use(prompt, source);
        f.Finish();
        Assert.Equal(Math.Max(0, amount - 3), receipt.Remaining);
        Assert.Equal(before + Math.Max(0, amount - 3), main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CounterintelligenceCannotPreventSideSchemeOrZeroThreat(bool side)
    {
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08017");
        var scheme = side ? f.World.CreateCard("01107", f.World.AreaOf(DeckType.SideSchemesArea))
            : f.World.TheCardIn(DeckType.MainSchemesArea)!;
        Threat.Schedule(f.World, new([scheme], f.Villain, side ? 2 : 0, ThreatCause.CardAbility, "negative probe", 0));
        f.Finish(source);
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cancel.4")]
    public void GrapplingHookCancelsOwnTreacheryIncludingSurgeOrDeclinesWithoutPaying(bool decline)
    {
        // A canceled treachery is "still regarded as revealed" and placed in the encounter discard pile.
        // Local ruling 49d5aba04226396f: all effects includes keywords.
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08008");
        var treachery = f.World.CreateCard("01190", f.World.AreaOf(DeckType.RevealingArea));
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 2, Subject: treachery.ObjectId, Seat: 1));
        var prompt = f.Find(source);
        if (decline) f.Answer(prompt, Decision.Decline); else f.Use(prompt, source);
        f.Finish();
        Assert.Equal(DeckType.EncounterDiscardPile, treachery.Area.Type);
        Assert.Equal(decline ? DeckType.UpgradesArea : DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(decline ? 1 : 0, f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count);
        Assert.False(f.World.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attack-enemy-activation.step.3")]
    public void AttacrobaticsCancelsExactBoostAndDamagesVillainDuringAttackOrScheme(bool attack)
    {
        // "Resolve the following steps one card at a time"; cancellation retains that physical card.
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08006");
        var boost = f.World.CreateCard("01102", f.World.AreaOf(DeckType.EncounterDeck));
        f.World.Agenda.Add(new PhaseStep(attack ? new EnemyAttackOperation() : new EnemySchemeOperation(),
            1, 2, Subject: f.Villain.ObjectId, Seat: 0));
        var prompt = f.Find(source);
        Assert.Equal(1, prompt.Player);
        Assert.Equal(boost.ObjectId, f.World.Agenda.Occurrence!.Subject);
        f.Use(prompt, source);
        f.Finish();
        Assert.Equal(2, f.Villain.Damage);
        Assert.Equal(0, Assert.Single(f.Events.OfType<BoostResolved>()).Icons);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.False(f.World.IsOver);
        Assert.Null(f.World.Activation);
        Assert.False(f.World.Agenda.IsBusy);
    }
}

