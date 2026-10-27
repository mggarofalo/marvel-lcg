using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkClashCardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:first-player.1")]
    public void FullRevealLetsNonzeroFirstPlayerDecideBothTiesAndRetainsAlly(bool defend)
    {
        // "The first player selects among the eligible options."
        var (world, runner) = HulkNemesisFixture.Create();
        world.FirstPlayer = 1;
        var allies = Enumerable.Range(0, 2).Select(player => world.CreateCard(player == 0 ? "06011" : "01050",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: 0))).ToArray();
        var enemies = Enumerable.Range(0, 2).Select(_ => world.CreateCard("01167",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)))).ToArray();
        var source = HulkNemesisFixture.Dealt(world, "10028");
        var character = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, character.Player);
        Assert.Equal(allies.Select(c => c.ObjectId), character.Affordances.Select(o => o.Id));
        Sequence.Answer(world, world.Facts, runner, character, Decision.Take(allies[1].ObjectId), []);
        var enemy = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, enemy.Player);
        var offer = Assert.Single(enemy.Affordances);
        Assert.Equal(enemies.Select(c => c.ObjectId), offer.Targets!.Legal);
        Sequence.Answer(world, world.Facts, runner, enemy,
            Decision.Take(offer.Id, [enemies[1].ObjectId], []), []);
        if (defend)
        {
            for (int i = 0; ; i++)
            {
                Assert.True(i < 20);
                var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
                if (prompt.Asking == Question.Defender && prompt.Player == 0)
                {
                    Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(allies[0].ObjectId), []);
                    break;
                }
                Assert.True(prompt.Cancellable);
                Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
            }
        }
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(3, allies[defend ? 0 : 1].Damage);
        Assert.Equal(0, allies[defend ? 1 : 0].Damage);
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [Rule("rr:stun-stunned.7")]
    [Rule("rr:tough.2")]
    public void FullRevealRanksModifiedStatsAndSurgesOnlyWhenNoAttackMade(bool stunned, bool hero, bool tough)
    {
        // Stun: "not considered to have attacked". Tough: "prevent all of that damage."
        var (world, runner) = HulkNemesisFixture.Create();
        if (hero) Forms.Change(world.Seats[1], world.Facts);
        var ally = world.CreateCard("01050", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)));
        var recipient = hero ? world.Seats[1].IdentityCard : ally;
        var enemy = world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "attack", Amount: 3, Card: recipient.ObjectId, Affects: recipient.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "attack", Amount: 1, Card: enemy.ObjectId, Affects: enemy.ObjectId));
        if (stunned) Statuses.Give(world, enemy, Statuses.Stunned);
        if (tough) Statuses.Give(world, recipient, Statuses.Tough);
        var source = HulkNemesisFixture.Dealt(world, "10028");
        WreckingRemainingSchemeFixture.Finish(world, runner);
        Assert.Equal(stunned || tough ? 0 : 4, recipient.Damage);
        Assert.Equal(stunned ? 1 : 0,
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.False(Statuses.Has(world, enemy, Statuses.Stunned));
        Assert.False(Statuses.Has(world, recipient, Statuses.Tough));
        Assert.Null(world.Activation);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
