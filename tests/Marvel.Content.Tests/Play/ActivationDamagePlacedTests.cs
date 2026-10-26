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

/// <summary>Local Overconfidence-shaped probes; no expansion runtime admission.</summary>
public sealed class ActivationDamagePlacedTests
{
    [Theory]
    [InlineData(0, false, 3)]
    [InlineData(1, false, 2)]
    [InlineData(0, true, 0)]
    [Rule("rr:damage.step.5")]
    [Rule("rr:damage.3.2")]
    public void PlacementIsAfterPreventionWhileDealtKeepsItsMeaning(int prevention, bool tough, long expected)
    {
        // "When ... damage ... takes is modified ... damage dealt is not modified."
        // The local 02031 threshold reads placement, while the legacy result still reads dealt.
        var runner = ProbeRunner();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        Prepare(world);
        var hero = world.Seats[0].IdentityCard;
        if (tough) Statuses.Give(world, hero, Statuses.Tough);
        if (prevention > 0) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "preventDamage", Amount: prevention, Affects: hero.ObjectId, Lasts: new Duration(Uses: 1)));
        int held = world.Seats[0].Hand.Cards.Count;
        RevealAndFinish(world, runner);
        Assert.Equal(expected, hero.Damage);
        Assert.Equal(held + (expected >= 3 ? 1 : 0) + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Theory]
    [InlineData(false, false, 3)]
    [InlineData(true, false, 5)]
    [InlineData(true, true, 3)]
    [Rule("rr:damage.2")]
    [Rule("rr:overkill.1")]
    [Rule("rr:overkill.2")]
    public void AllyExcessAndOverkillRetainEachPlacement(bool overkill, bool toughIdentity, long total)
    {
        // "place the specified value of damage tokens"; overkill deals damage
        // "on that ally beyond its hit points" to its controller's identity.
        var runner = ProbeRunner(total);
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        Prepare(world);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        if (overkill) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Keywords.Overkill, Affects: world.TheCardIn(DeckType.VillainArea)!.ObjectId));
        if (toughIdentity) Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        int held = world.Seats[0].Hand.Cards.Count;
        RevealAndFinish(world, runner, ally);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(overkill && !toughIdentity ? 2 : 0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    [Rule("rr:status-cards.2")]
    public void ASecondCanceledActivationReplacesTheEarlierPlacementReceipt()
    {
        // Status-card replacement "takes precedence over all other abilities".
        var runner = ProbeRunner(secondAttack: true);
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        Prepare(world);
        int held = world.Seats[0].Hand.Cards.Count;
        var source = world.CreateCard("02031", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 0);
        var events = new List<GameEvent>();
        var defense = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Defender, defense.Asking);
        Statuses.Give(world, world.Cards[world.Activation!.Enemy], Statuses.Stunned);
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:damage.step.5")]
    public void UnrelatedDamageByTheSameEnemyDoesNotJoinTheAttackPlacement()
    {
        // Step 5 places each instance; temporal overlap does not establish causation.
        var runner = ProbeRunner(4);
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        Prepare(world);
        int held = world.Seats[0].Hand.Cards.Count;
        var source = world.CreateCard("02031", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 0);
        var events = new List<GameEvent>();
        var defense = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(Question.Defender, defense.Asking);
        var foreign = DamagePlacement.Resolve(world, world.Facts,
            new(world.Cards[world.Activation!.Enemy], world.Seats[0].IdentityCard, 1,
                "SyntheticForeignEffect", "Damage"), events);
        Assert.True(foreign.IsComplete);
        Sequence.Answer(world, world.Facts, runner, defense, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:overkill.1")]
    public void AnotherPlayersAllyRoutesOverkillToItsOwnIdentityAndTheSameReceipt()
    {
        // "the identity of the player who controls the ally" receives overkill.
        var runner = ProbeRunner(5);
        var (_, world) = Playing(_ => { }, hero: true,
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Prepare(world);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        ally.TakeDamage(2);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Keywords.Overkill,
            Affects: world.TheCardIn(DeckType.VillainArea)!.ObjectId));
        int held = world.Seats[0].Hand.Cards.Count;
        RevealAndFinish(world, runner, ally);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    private static AbilityRunner ProbeRunner(long threshold = 3, bool secondAttack = false) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"02031","abilities":[{
          "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
          "effect":{"seq":[
            {"enemyAttacks":{"enemies":{"query":"villain"}}},
            SECOND_ATTACK
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamagePlaced"},"count":THRESHOLD}},
              "then":{"draw":{"player":"you","count":1}}}},
            {"if":{"test":{"atLeast":{"value":{"result":"activationDamage"},"count":3}},
              "then":{"draw":{"player":"you","count":1}}}}
          ]}
        }]}]}
        """.Replace("THRESHOLD", threshold.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
        .Replace("SECOND_ATTACK", secondAttack ? "{\"enemyAttacks\":{\"enemies\":{\"query\":\"villain\"}}}," : "", StringComparison.Ordinal)));

    private static void Prepare(World world)
    {
        // A synthetic Rhino attack fixes three damage, with zero-icon boosts.
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attack", Amount: 1,
            Affects: world.TheCardIn(DeckType.VillainArea)!.ObjectId));
        var encounter = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 12; i++) world.CreateCard("01087", encounter);
    }

    private static void RevealAndFinish(World world, AbilityRunner runner, Card? defender = null)
    {
        var source = world.CreateCard("02031", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 0);
        var events = new List<GameEvent>();
        bool defended = false;
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            Decision decision = Decision.Decline;
            if (defender is not null && prompt.Asking == Question.Defender)
            {
                decision = Decision.Take(Assert.Single(prompt.Affordances, offer => offer.AnchorId == defender.ObjectId).Id);
                defended = true;
            }
            Sequence.Answer(world, world.Facts, runner, prompt, decision, events);
        }
        Assert.Equal(defender is not null, defended);
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Contains(events.OfType<AttackCompleted>(), completed => completed.DamageDealt == 3);
    }
}
