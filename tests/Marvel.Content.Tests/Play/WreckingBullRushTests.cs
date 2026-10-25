using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingBullRushTests
{
    [Theory]
    [InlineData(0, true, "normal")]
    [InlineData(1, true, "normal")]
    [InlineData(0, false, "normal")]
    [InlineData(0, true, "canceled")]
    [InlineData(1, true, "canceled")]
    [InlineData(0, false, "canceled")]
    [InlineData(0, true, "tough")]
    [InlineData(1, true, "tough")]
    [Rule("rr:damage.3.2")]
    public void DiscardsExactlyTheActualBulldozerActivationResult(int player, bool hero, string status)
    {
        // Bull Rush: discard one top-deck card "for each damage dealt by this attack"
        // or "for each threat placed by this activation". rr:damage.3.2: prevention changes
        // damage taken, but "the amount of damage dealt is not modified".
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07055", 3,
            hero ? "01001a" : "01001b", 2);
        for (int seat = 0; seat < 2; seat++)
            for (int count = 0; count < 6; count++) world.CreateCard("01088", world.Seats[seat].Deck);
        var original = world.Seats.Select(seat => seat.Deck.Cards.Count).ToArray();
        if (status == "canceled") Statuses.Give(world, groups[3].Villain, hero ? Statuses.Stunned : Statuses.Confused);
        if (status == "tough") Statuses.Give(world, world.Seats[player].IdentityCard, Statuses.Tough);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, player);
        Finish(world, runner);
        AssertOutcome(world, groups, original, player, hero, status);
    }

    private static void AssertOutcome(World world, EncounterDeckGroup[] groups, int[] original,
        int player, bool hero, string status)
    {
        int result = status == "canceled" ? 0 : hero ? 3 : 1;
        Assert.Equal(original[player] - result, world.Seats[player].Deck.Cards.Count);
        Assert.Equal(original[1 - player], world.Seats[1 - player].Deck.Cards.Count);
        Assert.Equal(hero && status == "normal" ? result : 0, world.Seats[player].IdentityCard.Damage);
        Assert.Equal(hero ? 0 : result, groups[3].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(status == "canceled" ? 2 : 1, groups[1].Deck.Cards.Count);
        Assert.Equal(2, groups[3].Deck.Cards.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefendedAttackDiscardsFromOriginalRevealerUsingDealtDamage(bool foreignAlly)
    {
        // ruling:fc57e61be5945ac9 (identical Rage of Ultron wording):
        // "The player that revealed Rage of Ultron is the same player that discards from their deck."
        var (world, _, source) = WreckingEncounterCardFixture.Create("07055", 3, players: 2);
        foreach (var seat in world.Seats)
            for (int count = 0; count < 6; count++) world.CreateCard("01088", seat.Deck);
        var original = world.Seats.Select(seat => seat.Deck.Cards.Count).ToArray();
        var defender = foreignAlly ? world.CreateCard("01002",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1)) : world.Seats[0].IdentityCard;
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, 0);
        Assert.True(Finish(world, runner, defender));
        Assert.Equal(original[0] - (foreignAlly ? 3 : 0), world.Seats[0].Deck.Cards.Count);
        Assert.Equal(original[1], world.Seats[1].Deck.Cards.Count);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(foreignAlly ? 1 : 0, world.Seats[1].IdentityCard.Damage);
        if (foreignAlly) Assert.Equal(DeckType.DiscardPile, defender.Area.Type);
    }

    private static bool Finish(World world, AbilityRunner runner, Card? defender = null)
    {
        bool defended = false;
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) return defended;
            Assert.True(step < 10);
            Assert.True(prompt.Cancellable);
            bool select = prompt.Asking == Question.Defender && defender is not null
                && prompt.Affordances.Any(option => option.Id == defender.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt,
                select ? Decision.Take(defender!.ObjectId) : Decision.Decline, []);
            defended |= select;
        }
    }
}
