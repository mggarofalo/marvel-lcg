using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual authored research rows; these cards are not publicly admitted.</summary>
public sealed class GoblinGimmicksEngineTests : CoreCardTestBase
{
    [Theory]
    [InlineData("01094", 9, 7)]
    [InlineData("01095", 9, 5)]
    [InlineData("01096", 9, 3)]
    [InlineData("01096", 1, 0)]
    [InlineData("01096", 0, 0)]
    [Rule("rr:heal")]
    [Rule("rr:surge.1")]
    public void RegenerativeHealingReadsCurrentVillainStageAndActualHealing(
        string face, int damage, int remaining)
    {
        // 02036: "double the villain's stage number"; "If no damage was healed this way".
        // rr:heal: "damage the character has sustained can be removed".
        // rr:surge.1: "deal yourself 1 facedown encounter card".
        var (world, runner, villain) = Create(face);
        villain.TakeDamage(damage);
        var source = world.CreateCard("02036", world.AreaOf(DeckType.RevealingArea));

        runner.ExecuteReveal(world, source, 1);
        Finish(world, runner);

        Assert.Equal(remaining, villain.Damage);
        Assert.Equal(damage == 0 ? 1 : 0,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
    }

    [Theory]
    [InlineData("01094", 7)]
    [InlineData("01096", 7)]
    [InlineData("01096", 0)]
    [Rule("rr:heal")]
    public void RegenerativeHealingBoostHealsTwoIndependentlyOfStageWithoutSurge(string face, int damage)
    {
        // 02036 Boost: "The villain heals 2 damage."
        // rr:heal: "damage the character has sustained can be removed".
        var (world, runner, villain) = Create(face);
        villain.TakeDamage(damage);
        var source = world.CreateCard("02036", world.AreaOf(DeckType.BoostingArea));

        runner.ExecuteBoost(world, source, 1);
        Finish(world, runner);

        Assert.Equal(Math.Max(0, damage - 2), villain.Damage);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:boost-boost-icon.6")]
    [Rule("rr:cost.3")]
    public void IntimidationPaysOrRetainsAFacedownBoostUntilTheNextActivation(bool pay)
    {
        // 02035: "spend 2 resources of any type or give the villain 1 facedown boost card".
        // rr:boost-boost-icon.6: "remains facedown ... until that enemy activates".
        // rr:cost.3: resources are generated "by discarding cards from their hand".
        var (world, runner, villain) = Create("01094");
        var payment = world.CreateCard("01088", world.Seats[1].Hand);
        var insufficient = world.CreateCard("01005", world.Seats[1].Hand);
        var source = world.CreateCard("02035", world.AreaOf(DeckType.RevealingArea));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var top = deck.Cards[^1];
        int before = deck.Cards.Count;
        runner.ExecuteReveal(world, source, 1);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(1, prompt.Player);
        var option = Assert.Single(prompt.Affordances, item => (item.Costs is { Count: > 0 }) == pay);
        int[] paying = [payment.ObjectId];
        var answer = pay ? new Decision(option.Id, [], paying,
            Allocations: ResourcePayment.Allocate(Assert.Single(option.Costs!), paying)) : Decision.Take(option.Id);
        if (pay)
        {
            Assert.Equal("2", Assert.Single(option.Costs!).Cost);
            Assert.Empty(Assert.Single(option.Costs!).Rule!);
            var digest = world.Digest().Canonical();
            long consumed = world.Random.Generator.WordsConsumed;
            Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, runner, prompt,
                new Decision(option.Id, [], [insufficient.ObjectId],
                    Allocations: [new(insufficient.ObjectId, 0, "B")]), []));
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
            Assert.Equal(prompt.Affordances.Select(item => item.Id), Sequence.Work(world, Cards, runner, [])!.Affordances.Select(item => item.Id));
        }
        Sequence.Answer(world, Cards, runner, prompt, answer, []);
        Finish(world, runner);

        var hosted = world.AreaOf(DeckType.BoostCardsDeck, villain.Area.PlayArea, villain.ObjectId);
        Assert.Equal(pay ? 0 : 1, hosted.Cards.Count);
        Assert.Equal(before - (pay ? 0 : 1), deck.Cards.Count);
        Assert.Same(pay ? world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)) : world.Seats[1].Hand, payment.Area);
        Assert.Null(world.Activation);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        if (pay) return;
        Assert.Same(top, Assert.Single(hosted.Cards));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: villain.ObjectId, Seat: 1));
        Finish(world, runner);
        Assert.Empty(hosted.Cards);
        Assert.Equal(DeckType.EncounterDiscardPile, top.Area.Type);
        Assert.Equal(before - 2, deck.Cards.Count);
        Assert.Equal(6, world.Seats[1].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(-1, false, "02021")]
    [InlineData(0, false, "02021")]
    [InlineData(1, false, "02021")]
    [InlineData(-1, true, "02021")]
    [InlineData(-1, false, "02034")]
    [InlineData(0, false, "02034")]
    [InlineData(1, false, "02034")]
    [InlineData(-1, true, "02034")]
    [Rule("rr:you-your.7")]
    [Rule("rr:defend-defense.5.1")]
    [Rule("rr:stun-stunned.7")]
    public void PumpkinBombsBindsTheCompletedAttackPlayerAcrossItsDiscard(int defender, bool stunned, string face)
    {
        // 02021/02034: "After the villain attacks you, discard Pumpkin Bombs and take 2 indirect damage."
        // rr:you-your.7: "the attacked player, even if that player defended with an ally".
        // rr:defend-defense.5.1: "the player whose character defended the attack".
        // rr:stun-stunned.7: the character "is not considered to have attacked".
        var (world, runner, villain) = Create("01094");
        var bombs = world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        if (stunned) Statuses.Give(world, villain, Statuses.Stunned);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: villain.ObjectId, Seat: 1));
        Finish(world, runner, defender);

        Assert.Equal(stunned ? DeckType.UpgradesArea : DeckType.EncounterDiscardPile, bombs.Area.Type);
        Assert.Equal(stunned ? 0 : defender == 0 ? 3 : 0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(stunned ? 0 : defender < 0 ? 6 : defender == 1 ? 3 : 0, world.Seats[1].IdentityCard.Damage);
    }

    private static (World World, AbilityRunner Runner, Card Villain) Create(string face)
    {
        var world = Hero("01001a", players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villain = world.CreateCard(face, world.AreaOf(DeckType.VillainArea));
        foreach (var seat in world.Seats)
            world.CreateCard("01088", seat.Deck);
        for (int index = 0; index < 8; index++)
            world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
        return (world, runner, villain);
    }

    [Fact]
    [Rule("rr:boost-boost-icon.4")]
    public void IntimidationBoostAddsAndResolvesItsAdditionalCardInTheSameActivation()
    {
        // 02035 Boost: "Give the villain 1 additional boost card for this activation."
        // rr:boost-boost-icon.4: "effects are cumulative".
        var (world, runner, villain) = Create("01094");
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var source = world.CreateCard("02035", deck);
        int before = deck.Cards.Count;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: villain.ObjectId, Seat: 1));
        Finish(world, runner);
        Assert.Equal(before - 2, deck.Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(5, world.Seats[1].IdentityCard.Damage);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("\"two\"")]
    public void GenericPaymentRequiresAPositiveInteger(string value) =>
        Assert.Throws<AbilityException>(() => new AbilityRunner(
            AbilityCatalog.Parse("{\"cards\":[{\"card\":\"02035\",\"abilities\":[{" +
                "\"trigger\":{\"timing\":\"WhenRevealed\"},\"effect\":{\"payOrEffect\":{\"generic\":" +
                value + ",\"otherwise\":{\"gainSurge\":1}}}}]}]}")));

    private static void Finish(World world, AbilityRunner runner, int defender = -1)
    {
        for (int count = 0; ; count++)
        {
            var prompt = Sequence.Work(world, Cards, runner, []);
            if (prompt is null) return;
            Assert.True(count < 20, prompt.Description);
            Decision answer;
            if (prompt.Asking == Question.Defender && prompt.Player == defender)
                answer = Decision.Take(world.Seats[defender].IdentityCard.ObjectId);
            else if (prompt.Cancellable) answer = Decision.Decline;
            else
            {
                Assert.Equal(defender < 0 ? 1 : defender, prompt.Player);
                var option = Assert.Single(prompt.Affordances);
                int identity = world.Seats[prompt.Player].IdentityCard.ObjectId;
                answer = Decision.Take(option.Id, [identity, identity], []);
            }
            Sequence.Answer(world, Cards, runner, prompt, answer, []);
        }
    }
}
