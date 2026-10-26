using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Power Drain rows exercised outside public product admission.</summary>
public sealed class PowerDrainCardTests
{
    [Fact]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void DefeatDiscardsEachPlayersResourcesBeforeTheSchemeLeaves()
    {
        // "When Defeated abilities resolve before the defeated card leaves play."
        // 02041: "Each player must choose and discard 1 resource ... for each boost icon."
        var runner = AuthoredCards.Runner();
        var world = Board(runner);
        world.CreateCard("01121", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("02041", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("02042", world.AreaOf(DeckType.EncounterDeck));
        var hands = Enumerable.Range(0, 2).Select(player => new[]
        {
            world.CreateCard("01088", world.Seats[player].Hand),
            world.CreateCard("01089", world.Seats[player].Hand),
            world.CreateCard("01011", world.Seats[player].Hand),
            world.CreateCard("01004", world.Seats[player].Hand),
        }).ToArray();
        var source = world.CreateCard("02041", world.AreaOf(DeckType.SideSchemesArea));
        Defeat.Scheme(world, world.Facts, source, "defeat", [], by: 1, recordOn: new Occurrence(1, []));
        for (int player = 0; player < 2; player++)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
            Assert.Equal(player, prompt.Player);
            Assert.Equal(DeckType.SideSchemesArea, source.Area.Type);
            var option = Assert.Single(prompt.Affordances);
            var selected = hands[player].Take(3).Select(card => card.ObjectId).ToArray();
            Assert.True(option.Targets!.Allows(selected)); // Two + two + wild = five icons.
            Assert.False(option.Targets.Allows(selected.Take(2).ToArray()));
            Assert.False(option.Targets.Allows(hands[player].Select(card => card.ObjectId).ToArray()));
            Sequence.Answer(world, world.Facts, runner, prompt, new(option.Id, selected, []), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.All(hands, hand => Assert.Equal(DeckType.HandsArea, hand[3].Area.Type));
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
    }

    [Theory]
    [InlineData("01121", "01121", 0)]
    [InlineData("02042", "02041", 5)]
    [Rule("rr:boost-boost-icon.1")]
    [Rule("rr:encounter-deck.2")]
    public void LightningBoltCountsNumericIconsThroughDeckReset(string first, string second, int damage)
    {
        // "A star icon is not a boost icon"; do not continue discarding after reset.
        var runner = AuthoredCards.Runner();
        var world = Board(runner);
        world.CreateCard("01121", world.AreaOf(DeckType.EncounterDiscardPile));
        world.CreateCard(first, world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard(second, world.AreaOf(DeckType.EncounterDeck));
        runner.ExecuteReveal(world, world.CreateCard("02044", world.AreaOf(DeckType.RevealingArea)), 1);
        Finish(world, runner);
        Assert.Equal(damage, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(3, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }

    [Theory]
    [InlineData(0, "02041", 0)]
    [InlineData(6, "02042", 2)]
    [InlineData(6, "01121", 6)]
    public void ShockTherapyDiscardsPerPlayerAndHealsOnlyNumericIcons(int initial, string face, int remaining)
    {
        // 02045: "Discard 1 [per hero] cards ... villain heals 1 damage for each boost icon."
        var runner = AuthoredCards.Runner();
        var world = Board(runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        villain.TakeDamage(initial);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01121", deck);
        world.CreateCard(face, deck);
        world.CreateCard(face, deck);
        runner.ExecuteReveal(world, world.CreateCard("02045", world.AreaOf(DeckType.RevealingArea)), 1);
        Finish(world, runner);
        Assert.Equal(remaining, villain.Damage);
        Assert.Single(deck.Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
    }

    [Theory]
    [InlineData("02042")]
    [InlineData("02043")]
    [InlineData("02044")]
    [InlineData("02045")]
    public void EveryBoostDiscardsExactlyThreeCards(string face)
    {
        var runner = AuthoredCards.Runner();
        var world = Board(runner);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 4; i++) world.CreateCard("02041", deck);
        runner.ExecuteBoost(world, world.CreateCard(face, world.AreaOf(DeckType.BoostingArea)), 1);
        Finish(world, runner);
        Assert.Single(deck.Cards);
        Assert.Equal(3, world.AreaOf(DeckType.EncounterDiscardPile).Cards.Count);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ElectroResponseDamagesAttackedPlayerEvenWhenAllyDefends(bool allyDefense)
    {
        // Vendored FAQ 02042: "If you defend with an ally, he has still attacked you – the player."
        var runner = AuthoredCards.Runner();
        var world = Board(runner);
        var electro = world.CreateCard("02042", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.CreateCard("01121", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("02041", world.AreaOf(DeckType.EncounterDeck));
        var ally = allyDefense ? world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1)) : null;
        if (!allyDefense) Statuses.Give(world, world.Seats[1].IdentityCard, Statuses.Tough);
        var events = new List<Marvel.Rules.Events.GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: electro.ObjectId, Seat: 1));
        for (int i = 0; i < 15; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            Decision answer;
            if (prompt.Asking == Question.Element)
            {
                Assert.Equal(1, prompt.Player);
                var allocation = Assert.Single(prompt.Affordances);
                var targets = Enumerable.Repeat(world.Seats[1].IdentityCard.ObjectId, 3).ToArray();
                Assert.True(allocation.Targets!.Allows(targets));
                answer = Decision.Take(allocation.Id, targets, []);
            }
            else
            {
                Assert.True(prompt.Cancellable, prompt.Label);
                answer = ally is not null && prompt.Asking == Question.Defender && prompt.Player == 1
                    ? Decision.Take(ally.ObjectId) : Decision.Decline;
            }
            Sequence.Answer(world, world.Facts, runner, prompt, answer, events);
        }
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(3, world.Seats[1].IdentityCard.Damage);
        if (ally is not null) Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        else Assert.False(Statuses.Has(world, world.Seats[1].IdentityCard, Statuses.Tough));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
    }

    private static World Board(AbilityRunner runner)
    {
        var world = new World(CardCatalogData, players: 2) { Abilities = runner };
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
            world.CreateCard("01003", seat.Deck);
        }
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01107", world.AreaOf(DeckType.MainSchemesArea));
        return world;
    }

    private static void Finish(World world, AbilityRunner runner)
    {
        for (int i = 0; i < 12; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) return;
            Assert.True(prompt.Cancellable, prompt.Label);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        }
        Assert.Fail("A single effect must finish within bounded optional windows.");
    }
}

