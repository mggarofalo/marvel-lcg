using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkCustomizationCardTests
{
    [Fact]
    public void ThreeActualFacesAppendWithoutAdmittingTheRemainder()
    {
        using var document = JsonDocument.Parse(AuthoredCards.Text);
        var rows = document.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(712, rows.Length);
        string[] expected = ["10030", "10031", "10032"];
        Assert.Equal(expected,
            rows.Skip(709).Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(709), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.Equal(27, AuthoredCards.Book.Authored.Count(id => id.StartsWith("10", StringComparison.Ordinal)));
        string[] absent = ["10015", "10029", "10025", "10026", "10027", "10028", "90001"];
        Assert.All(absent,
            id => Assert.DoesNotContain(id, AuthoredCards.Book.Authored));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [Rule("rr:and.1")]
    public void InspiringPresencePaysOneAndIndependentlyHealsAndReadiesAnyAlly(int damage, bool ready)
    {
        // "Individual effects connected by 'and' are not dependent on each other."
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        var runner = AuthoredCards.Runner();
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var source = world.CreateCard("10030", world.Seats[0].Hand);
        var payment = world.CreateCard("10002", world.Seats[0].Hand);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        ally.TakeDamage(damage);
        if (!ready) ally.Exhaust();
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [])).Events.ToList();
        Answer(world, runner, events, ally.ObjectId);
        Assert.True(ally.Ready);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
    }

    [Theory]
    [InlineData("10001a", true)]
    [InlineData("10001b", false)]
    [InlineData("05001a", false)]
    public void InspiringPresenceRequiresHeroFormAndAvengerIdentity(string face, bool allowed)
    {
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        var runner = AuthoredCards.Runner();
        if (face == "05001a") world.Seats[0].IdentityCard = world.CreateCard(face, world.Seats[0].Hero);
        else world.Seats[0].IdentityCard.TurnTo(face);
        if (face == "10001b") world.Effects.Register(new(EffectSource.LastingEffect,
            "trait:AVENGER", 1, Affects: world.Seats[0].IdentityCard.ObjectId));
        var source = world.CreateCard("10030", world.Seats[0].Hand);
        world.CreateCard("10002", world.Seats[0].Hand);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        ally.Exhaust();
        Assert.Equal(allowed, runner.Actions(world, 0).Any(a => a.Card == source.ObjectId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ResourcefulDiscardPaysExactlyOneWildForItsControllerInEitherForm(bool hero)
    {
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, hero: hero, owner: 1);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("10032", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 0));
        source.Exhaust();
        Assert.DoesNotContain(runner.ResourceAbilities(world, 0), a => a.Effect == source.ObjectId);
        Assert.Equal("G", Assert.Single(runner.ResourceAbilities(world, 1), a => a.Effect == source.ObjectId).Generates);
        var target = world.CreateCard("10032", world.Seats[1].Hand);
        CardPlay.Play(world, world.Facts, runner, world.Seats[1], target, [source.ObjectId], []);
        Assert.Equal(DeckType.UpgradesArea, target.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.DoesNotContain(runner.ResourceAbilities(world, 1), a => a.Effect == source.ObjectId);
    }

    [Fact]
    [Rule("rr:ownership-and-control.2.1")]
    public void ArmorCanBePlayedUnderAnotherPlayersControlWithOnePerRecipient()
    {
        // An upgrade attached to another player's card "is controlled by that other player."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard("10031", world.Seats[0].Hand);
        var payment = world.CreateCard("10002", world.Seats[0].Hand);
        var recipient = world.Seats[1].IdentityCard;
        CardPlay.Play(world, world.Facts, runner, world.Seats[0], source, [payment.ObjectId], [], [recipient.ObjectId]);
        Assert.Equal(0, source.Owner);
        Assert.Equal(1, source.Area.PlayArea.Player);
        var second = world.CreateCard("10031", world.Seats[0].Hand);
        Assert.DoesNotContain(recipient.ObjectId, runner.AttachmentTargets(world, second)!);
        Assert.Contains(world.Seats[0].IdentityCard.ObjectId, runner.AttachmentTargets(world, second)!);
    }

    private static void Answer(World world, Marvel.Cards.Run.AbilityRunner runner,
        List<GameEvent> events, int target)
    {
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        var offer = prompt.Affordances.Single(a => a.Targets?.Legal.Contains(target) == true || a.Id == target);
        Sequence.Answer(world, world.Facts, runner, prompt,
            offer.Targets is null ? Decision.Take(offer.Id) : new(offer.Id, [target]), events);
        for (int i = 0; i < 8; i++)
        {
            var next = Sequence.Work(world, world.Facts, runner, events);
            if (next is null) break;
            var ordered = next.Affordances[0];
            Sequence.Answer(world, world.Facts, runner, next,
                ordered.Targets is null ? Decision.Take(ordered.Id) : new(ordered.Id, ordered.Targets.Legal.ToArray()), events);
        }
        Assert.Empty(world.Agenda.Outstanding);
    }
}
