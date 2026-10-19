using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PowerGauntletsTests : CoreCardTestBase
{
    [Rule("rr:you-your.2")]
    [Rule("rr:overkill.1")]
    [Theory]
    [InlineData(false, false, false, false, 0)]
    [InlineData(false, true, false, false, 0)]
    [InlineData(true, false, false, false, 0)]
    [InlineData(true, true, false, false, 0)]
    [InlineData(true, true, true, false, 0)]
    [InlineData(true, true, true, true, 0)]
    [InlineData(false, true, false, true, 0)]
    [InlineData(false, true, false, false, 1)]
    [InlineData(true, true, true, false, 1)]
    public void DiscardRequiresTheAttackToDamageTheIdentity(bool allyDefender, bool damaged, bool overkill, bool tough, int player)
    {
        // rr:you-your.2: damage to you is applied to your identity.
        // ruling:e324e06b32860347: "checks if the attacked player’s identity was damaged as a result of the attack."
        // 04074: "After the attached villain attacks and damages you, discard 1 card from your hand."
        var world = Hero("01001a", players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var gauntlets = world.CreateCard("04074", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        var identity = world.Seats[player].IdentityCard;
        var target = allyDefender ? world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player)) : identity;
        var hand = world.CreateCard("01088", world.Seats[player].Hand);
        world.CreateCard("01090", world.Seats[player].Deck);
        if (overkill)
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Keywords.Overkill, Amount: 1, Affects: villain.ObjectId));
        if (tough) Statuses.Give(world, identity, Statuses.Tough);
        world.Attack = new(villain.ObjectId, player, target.ObjectId,
            allyDefender ? target.ObjectId : -1, CalculatedDamage: AttackAmount(damaged, overkill));
        world.Activation = new(villain.ObjectId, player, Attacking: true);
        Attack.DealDamage(world, Cards, []);
        Attack.End(world, []);
        var occurrence = new Occurrence(1, [Steps.AttackEnds], Subject: villain.ObjectId,
            Actor: villain.ObjectId, Target: target.ObjectId, Player: player,
            ActorFacts: new(villain.ObjectId, CardKind.EncounterVillain, -1, -1),
            TargetFacts: new(target.ObjectId, allyDefender ? CardKind.Ally : CardKind.Hero, player, player));
        var responses = runner.Waiting(world, occurrence, WindowKind.Response)
            .Where(response => response.Card == gauntlets.ObjectId).ToArray();
        if (IdentityWasDamaged(damaged, allyDefender, overkill, tough))
        {
            var response = Assert.Single(responses);
            runner.ResolveWindow(world, occurrence, response, new Decision(response.Card, [], []));
            var prompt = Sequence.Work(world, Cards, runner, [])!;
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(Assert.Single(prompt.Affordances).Id), []);
            Sequence.Finish(world, Cards, runner, []);
            Assert.Equal(DeckType.DiscardPile, hand.Area.Type);
        }
        else
        {
            Assert.Empty(responses);
            Assert.Same(world.Seats[player].Hand, hand.Area);
        }
    }
    private static int AttackAmount(bool damaged, bool overkill) => damaged ? (overkill ? 4 : 1) : 0;

    private static bool IdentityWasDamaged(bool damaged, bool allyDefender, bool overkill, bool tough) =>
        damaged && (!allyDefender || overkill) && !tough;

}

