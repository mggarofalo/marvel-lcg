using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeConstantCardTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:ability.13")]
    [Rule("rr:ability.8.2")]
    public void CloakGrantsBothFormsAndReadiesNamedHeroOnlyWhenEffective()
    {
        // "A constant ability ... remains active while the card is in play."
        // Hero abilities "can only be used ... in the specified form."
        var (world, runner) = Board();
        var hero = world.Seats[1].IdentityCard;
        var source = world.CreateCard("09009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        Assert.True(Traits.Has(world, hero, "AERIAL", Cards));
        Assert.False(Traits.Has(world, world.Seats[0].IdentityCard, "AERIAL", Cards));
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        hero.Exhaust();
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.True(hero.Ready);
        Assert.False(source.Ready);
        hero.Exhaust();
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        source.Refresh();
        hero.TurnTo("09001b");
        Assert.True(Traits.Has(world, hero, "AERIAL", Cards));
        Assert.DoesNotContain(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        Discard.Card(world, source, "test", []);
        Assert.False(Traits.Has(world, hero, "AERIAL", Cards));
    }

    [Theory]
    [InlineData("09001a", 6)]
    [InlineData("09001b", 6)]
    [Rule("rr:ability.9")]
    public void SupremeHandSizeFollowsCurrentFormWithoutDiscard(string face, int size)
    {
        // Constant effects "are active anytime the specific condition is met."
        var (world, _) = Board();
        var hero = world.Seats[1].IdentityCard;
        hero.TurnTo(face);
        var source = world.CreateCard("09026", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        Assert.Equal(size, Modified(world, hero, "hand_size"));
        Assert.Equal(5, Modified(world, world.Seats[0].IdentityCard, "hand_size"));
        hero.TurnTo(face == "09001a" ? "09001b" : "09001a");
        Assert.Equal(6, Modified(world, hero, "hand_size"));
        Assert.Equal(DeckType.UpgradesArea, source.Area.Type);
        Discard.Card(world, source, "test", []);
        Assert.Equal(face == "09001a" ? 6 : 5, Modified(world, hero, "hand_size"));
    }

    [Theory]
    [InlineData("09001a", true)]
    [InlineData("09001b", true)]
    [InlineData("01001a", false)]
    [Rule("rr:initiating-abilities.step.2")]
    public void SupremeRequiresCurrentMysticWithValidSameCostControl(string face, bool allowed)
    {
        // "Check play restrictions: can the card be played ... at this time?"
        var (world, runner) = Board();
        var seat = world.Seats[1];
        if (face == "01001a")
        {
            World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
            seat.IdentityCard = world.CreateCard(face, seat.Hero);
        }
        else seat.IdentityCard.TurnTo(face);
        var target = world.CreateCard("09026", seat.Hand);
        var control = world.CreateCard("09009", seat.Hand);
        var pay = world.CreateCard("09022", seat.Hand);
        Assert.Equal(CardPayment.CostOf(world, Cards, seat, target).Amount, CardPayment.CostOf(world, Cards, seat, control).Amount);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.NotNull(CardPayment.Price(world, Cards, seat, control));
        Assert.Equal(allowed, CardPayment.Price(world, Cards, seat, target) is not null);
        if (allowed)
        {
            CardPlay.Play(world, Cards, runner, seat, target, [pay.ObjectId], []);
            Assert.Equal(DeckType.UpgradesArea, target.Area.Type);
        }
        else
        {
            Assert.Throws<RulesNotImplementedException>(() => CardPlay.Play(world, Cards, runner, seat, target, [pay.ObjectId], []));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(pending, world.Agenda.Outstanding);
        }
    }

    [Fact]
    [Rule("rr:ability.13")]
    [Rule("rr:initiating-abilities.step.5")]
    public void EyeGeneratesOneWildForControllerOnlyAndCannotPayTwice()
    {
        // Hero abilities require "the specified form"; "Pay the cost(s)."
        var (world, runner) = Board();
        var seat = world.Seats[1];
        var source = world.CreateCard("09011", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var target = world.CreateCard("01006", seat.Hand);
        Assert.Equal("G", Assert.Single(CardPayment.Generators(world, Cards, seat, runner, target), g => g.Effect == source.ObjectId).Generates);
        Assert.DoesNotContain(runner.ResourceAbilities(world, 0), g => g.Effect == source.ObjectId);
        seat.IdentityCard.TurnTo("09001b");
        Assert.DoesNotContain(runner.ResourceAbilities(world, 1), g => g.Effect == source.ObjectId);
        seat.IdentityCard.TurnTo("09001a");
        CardPlay.Play(world, Cards, runner, seat, target, [source.ObjectId], []);
        Assert.Equal(DeckType.SupportsArea, target.Area.Type);
        Assert.False(source.Ready);
        Assert.DoesNotContain(runner.ResourceAbilities(world, 1), g => g.Effect == source.ObjectId);
        string before = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteResource(world, 1, source.ObjectId));
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Fact]
    public void ConstantInventoryAddsExactlyThreeFacesWithoutCounters()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(684, rows.Length);
        string[] added = ["09009", "09011", "09026"];
        Assert.Equal(added, rows.Skip(651).Take(3).Select(r => r.GetProperty("card").GetString()));
        Assert.All(rows.Skip(651).Take(3), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.Equal(39, AuthoredCards.Book.Authored.Count(face => face.StartsWith("09", StringComparison.Ordinal)));
    }

    [Fact]
    [Rule("rr:play-restrictions-and-permissions.1")]
    [Rule("rr:ability.9")]
    public void GrantedMysticAllowsActualSupremeAndLaterTraitLossDoesNotDiscardIt()
    {
        // "In order to ... play a card, all of its play restrictions must be observed."
        // Constant effects "are active anytime the specific condition is met."
        var world = Hero("01010a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var seat = world.Seats[1];
        world.CreateCard("01090", seat.Deck);
        var upgrade = world.CreateCard("09026", seat.Hand);
        var payment = world.CreateCard("09022", seat.Hand);
        Assert.False(Traits.Has(world, seat.IdentityCard, "MYSTIC", Cards));
        Assert.Null(CardPayment.Price(world, Cards, seat, upgrade));
        var grant = world.Effects.Register(new(EffectSource.LastingEffect,
            Traits.Granted + "MYSTIC", Affects: seat.IdentityCard.ObjectId));
        Assert.NotNull(CardPayment.Price(world, Cards, seat, upgrade));
        CardPlay.Play(world, Cards, runner, seat, upgrade, [payment.ObjectId], []);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(6, Modified(world, seat.IdentityCard, "hand_size"));
        grant.Dispose();
        Assert.False(Traits.Has(world, seat.IdentityCard, "MYSTIC", Cards));
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
        Assert.Equal(6, Modified(world, seat.IdentityCard, "hand_size"));
        Assert.Equal(5, Modified(world, world.Seats[0].IdentityCard, "hand_size"));
    }

    private static (World World, AbilityRunner Runner) Board()
    {
        var world = Hero("01001a", 2);
        World.MoveToTop(world.Seats[1].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        world.Seats[1].IdentityCard = world.CreateCard("09001a,09001b", world.Seats[1].Hero);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        return (world, runner);
    }
}
