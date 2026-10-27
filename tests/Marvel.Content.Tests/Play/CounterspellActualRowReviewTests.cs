using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CounterspellActualRowReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:status-cards.2")]
    [Rule("rr:event.2")]
    public void ActualAstralPaysSchemeChoiceAndOnlyConfusionPreemptsCounterspell(bool confused)
    {
        // "Status card abilities have timing priority over all conflicting triggered abilities."
        // Canceled events' "costs remain paid. Only the effects are canceled."
        // Printed 09003 chooses its scheme before the arrow; its thwart does not consume stun.
        var world = DoctorStrangeNemesisFixture.Board();
        var attachment = DoctorStrangeNemesisFixture.Counterspell(world);
        var hero = world.Seats[1].IdentityCard;
        Statuses.Give(world, hero, confused ? Statuses.Confused : Statuses.Stunned);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 8);
        var source = world.CreateCard("09003", world.Seats[1].Hand);
        var payment = world.CreateCard("01088", world.Seats[1].Hand);
        var encounterTop = world.AreaOf(DeckType.EncounterDeck).Cards[^1];
        long words = world.Random.Generator.WordsConsumed;
        var action = Assert.Single(world.Abilities!.Actions(world, 1), offer => offer.Card == source.ObjectId);
        var events = world.Abilities.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], [scheme.ObjectId])).Events.ToList();
        if (!confused)
        {
            var order = Sequence.Work(world, world.Facts, world.Abilities, events)!;
            Assert.Equal(Question.Order, order.Asking);
            Sequence.Answer(world, world.Facts, world.Abilities, order,
                new Decision(Assert.Single(order.Affordances).Id, [1, 0]), events);
        }
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities, events));
        Assert.False(Statuses.Has(world, hero, Statuses.Confused));
        Assert.Equal(!confused, Statuses.Has(world, hero, Statuses.Stunned));
        Assert.Equal(confused ? DeckType.UpgradesArea : DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(8, scheme.Tokens["k_threat"]);
        Assert.Same(encounterTop, world.AreaOf(DeckType.EncounterDeck).Cards[^1]);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
