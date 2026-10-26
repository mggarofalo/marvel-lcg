using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowPreventionIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:prevent.2")]
    public void CounterintelligencePreventsOnlyOneThreatPlacementOrMayBeDeclined(bool decline)
    {
        // "Reduce the amount of threat being assigned before it is placed on the scheme."
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08017");
        var main = f.World.TheCardIn(DeckType.MainSchemesArea)!;
        Threat.Schedule(f.World, new([main], f.Villain, 1, ThreatCause.CardAbility, "first instance", 0));
        var prompt = f.Find(source);
        if (decline) f.Answer(prompt, Decision.Decline); else f.Use(prompt, source);
        f.Finish();
        Assert.Equal(decline ? 1 : 0, main.Tokens.GetValueOrDefault("k_threat"));
        Threat.Schedule(f.World, new([main], f.Villain, 2, ThreatCause.CardAbility, "second instance", 0));
        if (decline) f.Answer(f.Find(source), Decision.Decline);
        f.Finish();
        Assert.Equal(decline ? 3 : 2, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(decline ? DeckType.UpgradesArea : DeckType.DiscardPile, source.Area.Type);
        Assert.False(f.World.IsOver);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:prevent.1")]
    public void DefensiveStancePreventsOnlyOneDamageInstanceOrMayBeDeclined(bool decline)
    {
        // "Reduce the amount of damage the target takes."
        var f = new BlackWidowPreventionFixture();
        var source = f.Upgrade("08032");
        f.World.Effects.Register(new(EffectSource.LastingEffect, "attack", -1, Affects: f.Villain.ObjectId));
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false },
            1, 2, Subject: f.Villain.ObjectId, Seat: 1));
        var prompt = f.Find(source);
        if (decline) f.Answer(prompt, Decision.Decline); else f.Use(prompt, source);
        f.Finish();
        Assert.Equal(decline ? 1 : 0, f.Hero.Damage);
        f.World.Effects.Register(new(EffectSource.LastingEffect, "attack", 1, Affects: f.Villain.ObjectId));
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false },
            1, 2, Subject: f.Villain.ObjectId, Seat: 1));
        if (decline) f.Answer(f.Find(source), Decision.Decline);
        f.Finish();
        Assert.Equal(decline ? 3 : 2, f.Hero.Damage);
        Assert.Equal(decline ? DeckType.UpgradesArea : DeckType.DiscardPile, source.Area.Type);
        Assert.False(f.World.IsOver);
    }
}
