using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeResponseEventTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [Rule("rr:initiating-abilities.step.5")]
    public void MomentumRequiresFullHealingAndRejectedPaymentIsPure(int damage)
    {
        // "Pay the cost(s)" requires the complete heal-two arrow cost.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(damage);
        var source = world.CreateCard("09016", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed, world.Agenda.Count);
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        var offers = runner.Actions(world, 0).Where(a => a.Card == source.ObjectId).ToArray();
        Assert.Equal(damage == 2 ? 1 : 0, offers.Length);
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed, world.Agenda.Count));
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        if (damage != 2) return;
        var action = Assert.Single(offers);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [], [villain.ObjectId])));
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed, world.Agenda.Count));
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], []));
        var choice = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, choice,
            Decision.Take(Assert.Single(choice.Affordances, a => a.AnchorId == villain.ObjectId).Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(2, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Theory]
    [InlineData(0, false, false, 2, true, false)]
    [InlineData(0, false, false, 2, true, true)]
    [InlineData(1, false, false, 2, true, false)]
    [InlineData(1, true, false, 2, false, false)]
    [InlineData(1, false, true, 2, false, false)]
    [InlineData(1, false, false, 0, false, false)]
    [Rule("rr:prevent.1")]
    public void WarningReducesOnlyPositivePendingHeroDamage(int targetSeat, bool alterEgo, bool ally, int amount, bool eligible, bool fromAttack)
    {
        // "Reduce the amount of damage the target takes" belongs to this instance.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var target = ally ? world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(targetSeat), cardOwner: targetSeat))
            : world.Seats[targetSeat].IdentityCard;
        if (alterEgo) target.TurnTo("01001b");
        var source = world.CreateCard("09021", world.Seats[1].Hand);
        var events = new List<GameEvent>();
        var result = DamagePlacement.Resolve(world, world.Facts,
            new(world.TheCardIn(DeckType.VillainArea)!, target, amount, "Warning probe", "Damage") { FromAttack = fromAttack }, events);
        bool used = false;
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            var offer = prompt.Affordances.SingleOrDefault(a => a.AnchorId == source.ObjectId);
            if (offer is not null) { Assert.True(eligible); used = true; }
            Sequence.Answer(world, world.Facts, runner, prompt, offer is null ? Decision.Decline : Decision.Take(offer.Id), events);
        }
        Assert.Equal(eligible, used);
        Assert.True(result.IsComplete);
        Assert.Equal(amount, result.Dealt);
        Assert.Equal(eligible ? amount - 1 : amount, result.Taken);
        Assert.Equal(eligible ? DeckType.DiscardPile : DeckType.HandsArea, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void SkilledStrikeFollowsExhaustionAndAffectsOnlyItsOwnBasicAttack()
    {
        // "A character must exhaust to use this power." Ruling 3eb282172bb5df66 agrees.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("09037", world.Seats[1].Hand);
        var hero = world.Seats[1].IdentityCard;
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        long power = StateFields.Modified(world, hero, "attack", world.Facts, world.Players);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, enemy, []);
        Sequence.Finish(world, world.Facts, runner, []);
        long previous = enemy.Damage;
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 1, enemy, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.False(hero.Ready);
        Assert.Equal(previous, enemy.Damage);
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == source.ObjectId).Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(previous + power + 2, enemy.Damage);
        hero.Refresh();
        BasicPowerInitiation.BasicAttack(world, world.Facts, 1, enemy, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(previous + power * 2 + 2, enemy.Damage);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.2")]
    public void SkilledStrikeDoesNotApplyToAnEventAttack()
    {
        // "Resolving that ability is considered to attack the specified target."
        // Skilled Strike specifically requires a basic attack.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("09037", world.Seats[1].Hand);
        var attack = world.CreateCard("01005", world.Seats[1].Hand);
        var first = world.CreateCard("01088", world.Seats[1].Hand);
        var second = world.CreateCard("01089", world.Seats[1].Hand);
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == attack.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action,
            [first.ObjectId, second.ObjectId], []));
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == source.ObjectId);
            var target = prompt.Affordances.SingleOrDefault(a => a.AnchorId == enemy.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt,
                target is null ? Decision.Decline : Decision.Take(target.Id), []);
        }
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(8, enemy.Damage);
        Assert.Equal(DeckType.DiscardPile, attack.Area.Type);
        Assert.Equal(DeckType.HandsArea, source.Area.Type);
    }


    [Fact]
    public void ResponseInventoryAddsExactlyFourFacesWithoutCounters()
    {
        using var book = System.Text.Json.JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        string[] added = ["09016", "09021", "09037", "09038"];
        Assert.Equal(709, rows.Length);
        Assert.Equal(added, rows.Skip(661).Take(4).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(661).Take(4), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.Equal(40, AuthoredCards.Book.Authored.Count(face => face.StartsWith("09", StringComparison.Ordinal)));
        Assert.Contains("09001a", AuthoredCards.Book.Authored);
        Assert.DoesNotContain("90001", AuthoredCards.Book.Authored);
    }

}
