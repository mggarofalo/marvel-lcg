using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeCounterspellCandidateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RevealAttachesOnlyToResolvingPlayersHero(bool alterEgo)
    {
        // ruling:638d99c719a38af3: alter-ego has no valid attachment target; discard with no effect.
        var world = DoctorStrangeNemesisFixture.Board();
        if (alterEgo) world.Seats[1].IdentityCard.TurnTo("09001b");
        var card = DoctorStrangeNemesisFixture.Counterspell(world);
        Assert.Equal(alterEgo ? DeckType.EncounterDiscardPile : DeckType.UpgradesArea, card.Area.Type);
        if (!alterEgo) Assert.Equal(world.Seats[1].IdentityCard.ObjectId, card.Area.Host);
        Assert.NotEqual(world.Seats[0].IdentityCard.ObjectId, card.Area.Host);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [Rule("rr:event.2")]
    [Rule("rr:then.1")]
    public void AttachedSeatEventCancelsAndDiscardsBeforeAttachment(bool otherSeat, bool alterEgo)
    {
        // Canceled events are "still considered to have been played, and its costs remain paid".
        // Then: preceding effect "must resolve in full" before the subsequent effect.
        var world = DoctorStrangeNemesisFixture.Board();
        var counterspell = DoctorStrangeNemesisFixture.Counterspell(world);
        int player = otherSeat ? 0 : 1;
        if (alterEgo) world.Seats[1].IdentityCard.TurnTo("09001b");
        var source = world.CreateCard(alterEgo ? "09006" : "09016", world.Seats[player].Hand);
        var payment = world.CreateCard("01088", world.Seats[player].Hand);
        if (alterEgo) world.CreateCard("09007", world.Seats[player].Deck);
        else world.Seats[player].IdentityCard.TakeDamage(2);
        int before = world.Seats[player].Hand.Cards.Count;
        var runner = world.Abilities!;
        var action = Assert.Single(runner.Actions(world, player), offer => offer.Card == source.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action,
            [payment.ObjectId], alterEgo ? [] : [world.TheCardIn(DeckType.VillainArea)!.ObjectId])).Events.ToList();
        if (otherSeat)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events)!;
            Assert.Equal(player, prompt.Player);
            Sequence.Answer(world, world.Facts, runner, prompt,
                Decision.Take(world.TheCardIn(DeckType.VillainArea)!.ObjectId), events);
        }
        if (!otherSeat) Order(world, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(before - 2, world.Seats[player].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(otherSeat ? DeckType.UpgradesArea : DeckType.EncounterDiscardPile, counterspell.Area.Type);
        if (!otherSeat) AssertDiscardOrder(events, source, counterspell);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static void AssertDiscardOrder(List<GameEvent> events, Card source, Card counterspell)
    {
        var discards = events.OfType<CardsMoved>().Where(move => move.Verb == "Discard").ToArray();
        int eventIndex = Array.FindIndex(discards, move => move.Cards.Any(card => card.Card == source.ObjectId));
        int attachmentIndex = Array.FindIndex(discards, move => move.Cards.Any(card => card.Card == counterspell.ObjectId));
        Assert.True(eventIndex >= 0 && attachmentIndex > eventIndex);
    }

    [Fact]
    [Rule("rr:status-cards.2")]
    public void StunnedAttackEventRemovesStunAndRetainsCounterspell()
    {
        // "Status card abilities have timing priority over all conflicting triggered abilities."
        // ruling:4432bdf7194d532c specifically retains Counterspell in this case.
        var world = DoctorStrangeNemesisFixture.Board();
        var counterspell = DoctorStrangeNemesisFixture.Counterspell(world);
        var hero = world.Seats[1].IdentityCard;
        Statuses.Give(world, hero, Statuses.Stunned);
        var source = world.CreateCard("09004", world.Seats[1].Hand);
        int[] payment = [world.CreateCard("01088", world.Seats[1].Hand).ObjectId,
            world.CreateCard("01089", world.Seats[1].Hand).ObjectId];
        var action = Assert.Single(world.Abilities!.Actions(world, 1), offer => offer.Card == source.ObjectId);
        world.Abilities.ExecuteAction(world, new PlayerAction(action, payment,
            [world.TheCardIn(DeckType.VillainArea)!.ObjectId]));
        Sequence.Finish(world, world.Facts, world.Abilities, []);
        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.Equal(DeckType.UpgradesArea, counterspell.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvocationResolutionIsNotEventPlayButMasterIs(bool master)
    {
        // pack:mc08:the-invocation-deck and ruling:277ab401b1269ec7 distinguish resolving from playing.
        var runner = DoctorStrangeNemesisFixture.Runner();
        var (world, game, deck, _) = SpellMasteryFixture.BoardActualWithRunner(runner, "09036", master);
        var top = deck.PublicTop!;
        var source = DoctorStrangeNemesisFixture.Counterspell(world);
        var hero = world.Seats[1].IdentityCard;
        int before = world.Seats[1].Hand.Cards.Count;
        if (master)
        {
            var played = Assert.Single(world.Seats[1].Hand.Cards, card => card.FaceId == "09005");
            var action = Assert.Single(runner.Actions(world, 1), offer => offer.Card == played.ObjectId);
            var payment = world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088");
            runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [top.ObjectId]));
        }
        else
        {
            var action = Assert.Single(runner.Actions(world, 1), offer => offer.Card == hero.ObjectId);
            runner.ExecuteAction(world, new PlayerAction(action, [], [top.ObjectId]));
        }
        if (master) Order(world, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(master ? DeckType.EncounterDiscardPile : DeckType.UpgradesArea, source.Area.Type);
        Assert.Equal(master ? deck.Deck : deck.Discard, top.Area);
        Assert.Equal(master ? before - 2 : before + 3, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(master, hero.Ready);
        Assert.NotNull(game);
    }
    private static void Order(World world, List<GameEvent> events)
    {
        var prompt = Sequence.Work(world, world.Facts, world.Abilities, events)!;
        Assert.Equal(Question.Order, prompt.Asking);
        var offer = Assert.Single(prompt.Affordances);
        Sequence.Answer(world, world.Facts, world.Abilities, prompt,
            new Decision(offer.Id, [0, 1]), events);
    }
}




