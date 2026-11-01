using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.ScarletWitchBoards;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards; no public pack admission.
public sealed class ScarletWitchSignatureTests
{
    private const string HexBolt = "15004";
    private const string NextEvolution = "15024";
    private const string Quicksilver = "15002";
    private const string MolecularDecay = "15005";
    private const string SwiftRetribution = "15014";
    private const string BaitAndSwitch = "15030";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:for-each.1")]
    public void HexBoltResolvesEachDiscardedCardByItsCount(bool evolution)
    {
        // rr:for-each.1: the effect resolves once per card. Rulings a5028ed150b39433
        // and 805b3f9b199ca68b: each card is counted as it resolves, so The Next
        // Evolution raises Advance from 0 to 1 and Stampede from 1 to 2.
        var runner = AuthoredCards.Runner();
        var world = Board();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 6);
        if (evolution) world.CreateCard(NextEvolution, world.AreaOf(DeckType.SideSchemesArea)).PlaceTokens("k_threat", 9);
        var cards = Stack(world, ThreeBoost, OneBoost, ZeroBoost);
        var bolt = world.CreateCard(HexBolt, world.Seats[0].Hand);
        int[] paying = [world.CreateCard(Energy, world.Seats[0].Hand).ObjectId, world.CreateCard(Energy, world.Seats[0].Hand).ObjectId];
        int hand = world.Seats[0].Hand.Cards.Count - 3;
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == bolt.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, paying, []));
        var order = new List<int>();
        Drive(world, runner, prompt =>
        {
            // Scarlet Witch declines Chaos Control on each count.
            if (IsCount(world, prompt)) return null;
            // Resolve Advance first, then Stampede, then Sonic Converter.
            foreach (var card in new[] { cards[2], cards[1], cards[0] })
                if (prompt.Affordances.FirstOrDefault(offer => offer.AnchorId == card.ObjectId) is { } pick)
                {
                    order.Add(card.ObjectId);
                    return Decision.Take(pick.Id);
                }
            if (prompt.Affordances.FirstOrDefault(offer => offer.AnchorId == villain.ObjectId) is { } enemy) return Decision.Take(enemy.Id);
            if (prompt.Affordances.FirstOrDefault(offer => offer.AnchorId == main.ObjectId) is { } scheme) return Decision.Take(scheme.Id);
            return prompt.Affordances.Count > 0 ? Decision.Take(prompt.Affordances[0].Id) : null;
        });
        Assert.Equal([cards[2].ObjectId, cards[1].ObjectId, cards[0].ObjectId], order);
        Assert.All(cards, card => Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type));
        // Without the scheme: 0 deals 2 damage and 1 removes 2 threat. With it: 1 removes
        // 2 threat and 2 draws a card. Sonic Converter (3, or 4) gives a status card.
        Assert.Equal(evolution ? 0 : 2, villain.Damage);
        Assert.Equal(4, main.Tokens["k_threat"]);
        Assert.Equal(hand + (evolution ? 1 : 0), world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, bolt.Area.Type);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 3)]
    [Rule("rr:team-up.2")]
    public void SuperpoweredSiblingsDrawsThreeWhilePietroMaximoffIsInPlay(bool pietro, int drawn)
    {
        // rr:team-up.2 counts a character named by title or subtitle; ruling
        // 82607e2d70cbf9dd applies that to Quicksilver's subtitle Pietro Maximoff.
        var runner = AuthoredCards.Runner();
        var world = Board(AlterEgo);
        if (pietro) InPlay(world, Quicksilver, DeckType.AlliesArea);
        int[] discards = [world.CreateCard(Energy, world.Seats[0].Hand).ObjectId, world.CreateCard(Energy, world.Seats[0].Hand).ObjectId];
        int deck = world.Seats[0].Deck.Cards.Count;
        var identity = world.Seats[0].IdentityCard;
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == identity.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], discards));
        Drive(world, runner, prompt => prompt.Affordances.Count > 0 ? Decision.Take(prompt.Affordances[0].Id) with { Targets = discards } : null);
        Assert.Equal(deck - drawn, world.Seats[0].Deck.Cards.Count);
        Assert.All(discards, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
    }

    [Fact]
    [Rule("rr:for-each.2")]
    public void MolecularDecayAddsTheDiscardedBoostIconsToOneAttack()
    {
        // rr:for-each.2: the additional damage is part of the same instance. Charge (2)
        // and Stampede (1) add 3 to the printed 5.
        var runner = AuthoredCards.Runner();
        var world = Board();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Stack(world, OneBoost, TwoBoost);
        var decay = world.CreateCard(MolecularDecay, world.Seats[0].Hand);
        int[] paying = [.. Enumerable.Range(0, 3).Select(_ => world.CreateCard(Energy, world.Seats[0].Hand).ObjectId)];
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == decay.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, paying, []));
        Drive(world, runner, prompt => IsCount(world, prompt) ? null
            : prompt.Affordances.FirstOrDefault(offer => offer.AnchorId == villain.ObjectId) is { } enemy ? Decision.Take(enemy.Id) : null);
        Assert.Equal(8, villain.Damage);
    }

    [Fact]
    [Rule("rr:activation.1")]
    public void SwiftRetributionMakesTheVillainSchemeThenDealsFour()
    {
        // rr:activation.1: the villain scheming is an activation; the attack's damage follows.
        var runner = AuthoredCards.Runner();
        var world = Board();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");
        var swift = world.CreateCard(SwiftRetribution, world.Seats[0].Hand);
        int energy = world.CreateCard(Energy, world.Seats[0].Hand).ObjectId;
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == swift.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [energy], []));
        Drive(world, runner, _ => null);
        Assert.Equal(4, villain.Damage);
        Assert.True(main.Tokens.GetValueOrDefault("k_threat") > before);
    }

    [Fact]
    [Rule("rr:activation.1")]
    public void BaitAndSwitchMakesTheVillainAttackThenRemovesFour()
    {
        // rr:activation.1: the villain attacking is an activation; the thwart's removal follows.
        var runner = AuthoredCards.Runner();
        var world = Board();
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 6);
        var bait = world.CreateCard(BaitAndSwitch, world.Seats[0].Hand);
        int energy = world.CreateCard(Energy, world.Seats[0].Hand).ObjectId;
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == bait.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [energy], []));
        Drive(world, runner, _ => null);
        Assert.Equal(2, main.Tokens["k_threat"]);
        Assert.True(world.Seats[0].IdentityCard.Damage > 0);
    }
}
