using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical printed 07035 in arranged multi-villain games; no scenario admission claim.</summary>
public sealed class DistractingTauntsTests
{
    [Fact]
    [Rule("rr:hit-points.2.3")]
    public void RevealAttachesToPiledriverAndHealthEndsWhenDiscarded()
    {
        // "When ... gets +X hit points ... increase that character’s hit point dial by X."
        // Printed 07035: "Attach to Piledriver. Piledriver gets +3 hit points."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07035", 2);
        long before = Damage.Health(world, world.Facts, groups[2].Villain);
        world.Abilities = AuthoredCards.Runner();
        Reveal.Resolve(world, world.Facts, source, 0, []);
        Assert.Equal(groups[2].Villain.ObjectId, source.Area.Host);
        Assert.Equal(before + 3, Damage.Health(world, world.Facts, groups[2].Villain));
        World.MoveToTop(source, groups[2].Discard);
        Assert.Equal(before, Damage.Health(world, world.Facts, groups[2].Villain));
        Assert.Equal(4, BasicPowers.Attackable(world, world.Facts, 0).Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EveryPlayerCanAttackOnlyPiledriverOrMinions(int player)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07035", 2, players: 2);
        world.Abilities = AuthoredCards.Runner();
        Reveal.Resolve(world, world.Facts, source, 0, []);
        var minion = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));
        Assert.Equal(new[] { groups[2].Villain.ObjectId, minion.ObjectId }.Order(),
            BasicPowers.Attackable(world, world.Facts, player).Select(card => card.ObjectId).Order());
        Assert.True(world.DamageAbilities.CanTakeDamage(world, groups[0].Villain, world.Seats[player].IdentityCard));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeroAndAllyCannotAttackAnotherVillainBeforeExhaustion(bool useAlly)
    {
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07035", 2);
        world.Abilities = AuthoredCards.Runner();
        Reveal.Resolve(world, world.Facts, source, 0, []);
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        string digest = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() =>
        {
            if (useAlly) BasicPowers.AllyPower(world, world.Facts, ally, groups[0].Villain, BasicPowers.AttackVerb, []);
            else BasicPowers.BasicAttack(world, world.Facts, 0, groups[0].Villain, []);
        });
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.True(ally.Ready);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.Null(world.CharacterAttack);
    }

    [Theory]
    [InlineData(0, "hero", true)]
    [InlineData(1, "hero", true)]
    [InlineData(0, "ally", false)]
    [InlineData(0, "insufficient", false)]
    [InlineData(0, "wrongResource", false)]
    [Rule("rr:response")]
    public void ResponseRequiresThisPlayersHeroAttackingPiledriverAndTwoPhysicalResources(
        int player, string mode, bool expected)
    {
        // Responses resolve "after the specified triggering condition occurs".
        // Printed 07035: "After your hero attacks Piledriver, spend [physical] [physical] resources".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07035", 2, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, 0, []);
        world.CreateCard(PaymentCard(mode), world.Seats[player].Hand);
        var actor = mode == "ally"
            ? world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player))
            : world.Seats[player].IdentityCard;
        if (mode == "ally") BasicPowers.AllyPower(world, world.Facts, actor, groups[2].Villain, BasicPowers.AttackVerb, []);
        else BasicPowers.BasicAttack(world, world.Facts, player, groups[2].Villain, []);
        int offered = 0;
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.True(step < 15, prompt.Label);
            offered += prompt.Affordances.Count(offer => offer.AnchorId == source.ObjectId);
            Assert.True(prompt.Cancellable, prompt.Label);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
        Assert.Equal(expected ? 1 : 0, offered);
        Assert.Equal(groups[2].Villain.ObjectId, source.Area.Host);
    }

    private static string PaymentCard(string mode) => mode switch
    {
        "wrongResource" => "01088", // Energy supplies two energy resources.
        "insufficient" => "01003", // Backflip supplies one physical resource.
        _ => "01090", // Strength supplies two physical resources.
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:response.4")]
    public void CompletedHeroAttackCanPayOrDeclineTheResponse(bool pay)
    {
        // "Once all players decide they do not wish to resolve ... responses" the window closes.
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07035", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Reveal.Resolve(world, world.Facts, source, 0, []);
        var payment = world.CreateCard("01090", world.Seats[0].Hand);
        long health = Damage.Health(world, world.Facts, groups[2].Villain);
        BasicPowers.BasicAttack(world, world.Facts, 0, groups[2].Villain, []);
        int offered = 0;
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            Assert.True(step < 12, prompt.Label);
            var option = prompt.Affordances.SingleOrDefault(offer => offer.AnchorId == source.ObjectId);
            var answer = Decision.Decline;
            if (option is not null)
            {
                offered++;
                Assert.Equal(0, prompt.Player);
                Assert.True(prompt.Cancellable);
                if (pay) answer = new Decision(option.Id, [], [payment.ObjectId]);
            }
            Sequence.Answer(world, world.Facts, runner, prompt, answer, []);
        }
        Assert.Equal(1, offered);
        Assert.Equal(pay ? DeckType.EncounterDiscardPile : DeckType.UpgradesArea, source.Area.Type);
        Assert.Equal(pay ? DeckType.DiscardPile : DeckType.HandsArea, payment.Area.Type);
        Assert.Equal(health - (pay ? 3 : 0), Damage.Health(world, world.Facts, groups[2].Villain));
        Assert.Equal(pay ? 4 : 1, BasicPowers.Attackable(world, world.Facts, 0).Count);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }
}
