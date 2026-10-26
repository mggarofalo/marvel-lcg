using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RemainingDamageReviewTests : CoreCardTestBase
{
    // Synthetic "would take" timing contract, using the engine's shared would-be-dealt key.
    // Healing distinguishes the trigger condition from a prevention effect body.
    private static AbilityRunner Runner(string timing = "Interrupt", string role = "target") => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05017","abilities":[{
          "trigger":{"event":"WhenDamageWouldBeDealt","timing":"TIMING","ROLE":"you"},
          "cost":{"removeCounters":{"card":"this","counter":"reflection","count":1}},
          "effect":{"heal":{"card":"you","amount":1}}
        }]}]}
        """.Replace("TIMING", timing, StringComparison.Ordinal).Replace("ROLE", role, StringComparison.Ordinal)));

    [Fact]
    [Rule("rr:prevent.1.1")]
    public void WouldDealOptionalTriggerStillExistsWhenOnlyTakenDamageIsPrevented()
    {
        // Request-changes regression: prevention reduces "takes", but "dealt" is not reduced.
        // Steps.DamageWouldBeDealt documents the dealer-side step-one family as the same step.
        var world = Hero("01001a");
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(3);
        var target = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var upgrade = Upgrade(world);
        var runner = Runner(role: "actor");
        world.Abilities = runner;
        world.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", 1,
            Affects: target.ObjectId, Lasts: Duration.NextUses(1)));
        var replacement = DamagePlacement.Replace(world, new(hero, target, 1, "Review", "Damage"), []);
        Assert.Equal(1, replacement.ResolvedAmount);
        var ability = Assert.Single(runner.Waiting(world, replacement.Execution.Occurrence, WindowKind.Interrupt));
        runner.ResolveWindow(world, replacement.Execution.Occurrence, ability, Decision.Take(upgrade.ObjectId));
        Assert.Equal(2, hero.Damage);
        Assert.Equal(2, upgrade.Tokens["c_reflection"]);
    }

    [Theory]
    [InlineData(2, 1, true, true)]
    [InlineData(2, 2, true, false)]
    [InlineData(2, 0, true, false)]
    [InlineData(2, 99, false, true)]
    [Rule("rr:prevent.1")]
    public void OptionalNonPreventionEffectUsesOnlyRecipientResidualDamage(
        int dealt, int prevented, bool sameRecipient, bool offered)
    {
        // "reduce the amount of damage the target takes"; another target's shield is irrelevant.
        var world = Hero("01001a", 2);
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(3);
        var source = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var upgrade = Upgrade(world);
        var runner = Runner();
        world.Abilities = runner;
        world.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", prevented,
            Affects: sameRecipient ? hero.ObjectId : world.Seats[1].IdentityCard.ObjectId,
            Lasts: Duration.NextUses(1)));
        var replacement = DamagePlacement.Replace(world, new(source, hero, dealt, "Review", "Damage"), []);
        var occurrence = replacement.Execution.Occurrence;
        Assert.Equal(dealt, replacement.ResolvedAmount);
        Assert.Equal(offered, runner.Waiting(world, occurrence, WindowKind.Interrupt).Any());
        if (offered)
        {
            var ability = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));
            runner.ResolveWindow(world, occurrence, ability, Decision.Take(upgrade.ObjectId));
            Assert.Equal(2, hero.Damage);
            Assert.Equal(2, upgrade.Tokens["c_reflection"]);
        }
        else Assert.Equal(3, upgrade.Tokens["c_reflection"]);
        Assert.Single(world.Effects.Active(), effect => effect.Kind == "preventDamage");
    }

    [Fact]
    [Rule("rr:prevent.1.1")]
    public void MandatoryReplacementIsNotSuppressedByPendingPrevention()
    {
        // Prevention reduces damage "takes", but the amount "dealt" is not reduced.
        var world = Hero("01001a");
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(3);
        var source = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var upgrade = Upgrade(world);
        var runner = Runner("ForcedInterrupt");
        world.Abilities = runner;
        world.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", 1,
            Affects: hero.ObjectId, Lasts: Duration.NextUses(1)));
        var replacement = DamagePlacement.Replace(world, new(source, hero, 1, "Review", "Damage"), []);
        Assert.True(replacement.IsComplete);
        Assert.Equal(1, replacement.ResolvedAmount);
        Assert.Equal(2, hero.Damage);
        Assert.Equal(2, upgrade.Tokens["c_reflection"]);
    }

    [Fact]
    public void StalePreflightPreservesPendingPromptEffectsAndRandomStateThenAllowsDecline()
    {
        var world = Hero("01001a");
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(3);
        var source = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var upgrade = Upgrade(world);
        var runner = Runner();
        world.Abilities = runner;
        world.Activation = new(source.ObjectId, 0, true);
        world.Attack = new(source.ObjectId, 0, hero.ObjectId, CalculatedDamage: 1);
        world.Agenda.Add(new PhaseStep(new AttackStageOperation(AttackStageKind.DealAttackDamage), 1, 5));
        world.Agenda.Add(new PhaseStep(new AttackCompletionOperation(), 1, 6, Subject: source.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        var occurrence = world.Windows.Current!.Value.Occurrence;
        var stale = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt));
        world.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", 1,
            Affects: hero.ObjectId, Lasts: Duration.NextUses(1)));
        var digest = world.Digest().Canonical();
        var pending = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var window = world.Windows.Current;
        var stage = world.Agenda.Stage;
        Assert.Throws<RulesNotImplementedException>(() =>
            runner.PrepareWindow(world, occurrence, stale, Decision.Take(upgrade.ObjectId)));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(window, world.Windows.Current);
        Assert.Equal(stage, world.Agenda.Stage);
        Assert.Equal(3, upgrade.Tokens["c_reflection"]);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(3, hero.Damage);
        Assert.Null(world.Activation);
        Assert.Null(world.Attack);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(1, world.FinishedActivation!.DamageDealt);
        Assert.Equal(0, world.FinishedActivation.DamagePlaced);
        Assert.DoesNotContain(world.Effects.Active(), effect => effect.Kind == "preventDamage");
    }

    private static Card Upgrade(World world)
    {
        var card = world.CreateCard("05017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        card.PlaceTokens("c_reflection", 3);
        return card;
    }
}
