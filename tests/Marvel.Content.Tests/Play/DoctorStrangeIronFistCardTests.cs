using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Corrected 09014 on nonpublic boards using the actual authored program.</summary>
public sealed class DoctorStrangeIronFistCardTests
{
    [Fact]
    public void ActualAllyPaysFourAndEntersWithTwoOrdinaryMysticCounters()
    {
        Card? ally = null;
        Card[] payment = [];
        var (game, world) = Playing(board =>
        {
            ally = board.CreateCard("09014", board.Seats[0].Hand);
            payment = Enumerable.Range(0, 4).Select(_ => board.CreateCard("01088", board.Seats[0].Hand)).ToArray();
        }, abilities: AuthoredCards.Runner());
        var play = Assert.Single(game.Pending!.Affordances, item => item.PlaysCard && item.AnchorId == ally!.ObjectId);
        var cost = Assert.Single(play.CostOptions);
        Assert.Equal("4", cost.Cost);
        int[] paid = [.. payment.Select(card => card.ObjectId)];
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], paid,
            Allocations: ResourcePayment.Allocate(cost, paid)));
        Assert.Equal(DeckType.AlliesArea, ally!.Area.Type);
        Assert.Equal(2, ally.Tokens["c_mystic"]);
        Assert.True(ally.Ready);
        Assert.All(payment, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [InlineData(true, 1)]
    [InlineData(true, 0)]
    [Rule("rr:consequential-damage.1")]
    public void OptionalInterruptPrecedesBasicDamageAndLastCounterDoesNotDiscard(bool accept, int counters)
    {
        // Consequential damage is dealt "after resolving abilities that are triggered by the ally attacking".
        var world = ThorActualFixture.Board("06001b");
        var ally = Ally(world, 0, counters);
        var other = Ally(world, 0, 2);
        var target = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<GameEvent>();
        AllyBasicPowers.AllyPower(world, world.Facts, ally, target, BasicPowers.AttackVerb, events);
        var prompt = Sequence.Work(world, world.Facts, world.Abilities!, events);
        if (counters > 0)
        {
            Assert.NotNull(prompt);
            Assert.True(prompt.Cancellable);
            Assert.Equal(0, target.Damage);
            Assert.Equal(0, ally.Damage);
            var option = Assert.Single(prompt.Affordances);
            Sequence.Answer(world, world.Facts, world.Abilities!, prompt,
                accept ? Decision.Take(option.Id) : Decision.Decline, events);
        }
        else Assert.Null(prompt);
        Finish(world, events);
        bool used = accept && counters > 0;
        Assert.Equal(used ? 3 : 2, target.Damage);
        Assert.Equal(used, Statuses.Has(world, target, Statuses.Stunned));
        Assert.Equal(counters - (used ? 1 : 0), ally.Tokens["c_mystic"]);
        Assert.Equal(2, other.Tokens["c_mystic"]);
        Assert.Equal(1, ally.Damage);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.False(ally.Ready);
        Assert.False(world.Agenda.IsBusy);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:consequential-damage.2")]
    [Rule("rr:consequential-damage.2.1")]
    [Rule("rr:consequential-damage.2.2")]
    public void LethalInterruptAbortsExactAttackWithoutConsequentialDamage(bool foreignOwner)
    {
        // "If this effect defeats the enemy ... the basic attack aborts as soon as the target leaves play".
        var world = ThorActualFixture.Board("06001b", 2);
        var ally = Ally(world, 1, 2, foreignOwner ? 0 : 1);
        var target = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        long hp = world.Facts.PrintedValue(target.FaceId, "HP", world.Players);
        Assert.True(hp > 1);
        target.TakeDamage(hp - 1);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<GameEvent>();
        AllyBasicPowers.AllyPower(world, world.Facts, ally, target, BasicPowers.AttackVerb, events);
        var prompt = Sequence.Work(world, world.Facts, world.Abilities!, events)!;
        Assert.Equal(1, prompt.Player);
        Assert.Equal(hp - 1, target.Damage);
        Sequence.Answer(world, world.Facts, world.Abilities!, prompt,
            Decision.Take(Assert.Single(prompt.Affordances).Id), events);
        Finish(world, events);
        Assert.Equal(DeckType.EncounterDiscardPile, target.Area.Type);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(1, ally.Tokens["c_mystic"]);
        Assert.False(ally.Ready);
        Assert.False(world.Agenda.IsBusy);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("tough", 2, true)]
    [InlineData("stalwart", 3, false)]
    [InlineData("stunned", 3, true)]
    [Rule("rr:tough.1")]
    [Rule("rr:stalwart.1")]
    public void EnemyStatusRestrictionsDoNotSuppressIndependentDamage(string status, int damage, bool stunned)
    {
        // Tough discards "instead of taking damage"; stalwart "cannot have confused or stunned status cards".
        var world = ThorActualFixture.Board("06001a");
        var ally = Ally(world, 0, 2);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        if (status == "stalwart") world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "stalwart", Amount: 1, Card: enemy.ObjectId, Affects: enemy.ObjectId));
        else Statuses.Give(world, enemy, status);
        AllyBasicPowers.AllyPower(world, world.Facts, ally, enemy, BasicPowers.AttackVerb, []);
        var prompt = Sequence.Work(world, world.Facts, world.Abilities!, [])!;
        Sequence.Answer(world, world.Facts, world.Abilities!, prompt,
            Decision.Take(Assert.Single(prompt.Affordances).Id), []);
        Finish(world, []);
        Assert.Equal(damage, enemy.Damage);
        Assert.Equal(stunned, Statuses.Has(world, enemy, Statuses.Stunned));
        Assert.False(Statuses.Has(world, enemy, Statuses.Tough));
        Assert.Equal(1, ally.Tokens["c_mystic"]);
        Assert.Equal(1, ally.Damage);
    }

    [Fact]
    [Rule("rr:stun-stunned.5")]
    public void StunnedIronFistSpendsNoCounterAndThwartDoesNotTriggerInterrupt()
    {
        // "costs associated with the attack attempt ... must still be paid"; stun replaces that attack.
        var world = ThorActualFixture.Board("06001a");
        var ally = Ally(world, 0, 2);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, ally, Statuses.Stunned);
        AllyBasicPowers.AllyPower(world, world.Facts, ally, enemy, BasicPowers.AttackVerb, []);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities!, []));
        Assert.Equal(0, enemy.Damage);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(2, ally.Tokens["c_mystic"]);
        Assert.False(Statuses.Has(world, ally, Statuses.Stunned));
        ally.Refresh();
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 2);
        AllyBasicPowers.AllyPower(world, world.Facts, ally, scheme, BasicPowers.ThwartVerb, []);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities!, []));
        Assert.Equal(1, scheme.Tokens["k_threat"]);
        Assert.Equal(2, ally.Tokens["c_mystic"]);
        Assert.Equal(1, ally.Damage);
    }

    [Fact]
    public void InventoryAppendsOnlyIronFistWithTwoOrdinaryCounters()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(700, rows.Length);
        Assert.Equal("09014", rows[678].GetProperty("card").GetString());
        Assert.Equal(40, AuthoredCards.Book.Authored.Count(face => face.StartsWith("09", StringComparison.Ordinal)));
        var pool = rows[678].GetProperty("startingCounters");
        Assert.Equal("mystic", pool.GetProperty("type").GetString());
        Assert.Equal(2, pool.GetProperty("count").GetInt32());
        Assert.False(pool.GetProperty("uses").GetBoolean());
        Assert.DoesNotContain("90001", AuthoredCards.Book.Authored);
    }

    private static void Finish(World world, List<GameEvent> events)
    {
        for (int count = 0; count < 4; count++)
        {
            var prompt = Sequence.Work(world, world.Facts, world.Abilities!, events);
            if (prompt is null) return;
            Assert.Equal(Question.Order, prompt.Asking);
            var option = Assert.Single(prompt.Affordances);
            Sequence.Answer(world, world.Facts, world.Abilities!, prompt,
                new Decision(option.Id, [0, 1]), events);
        }
        Assert.Fail("Iron Fist's two independent effects did not complete");
    }

    private static Card Ally(World world, int controller, int counters, int? owner = null)
    {
        var ally = world.CreateCard("09014", world.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(controller), cardOwner: owner ?? controller));
        ally.PlaceTokens("c_mystic", counters);
        return ally;
    }
}
