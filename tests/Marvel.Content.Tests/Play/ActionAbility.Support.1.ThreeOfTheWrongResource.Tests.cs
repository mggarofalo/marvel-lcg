using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class ActionAbilitySupportThreeOfTheWrongResourceTests
{
    [Rule("rr:resource.4")]
    [Fact]
    public void ThreeOfTheWrongResourceIsNotThreePhysicals()
    {
        // Enough cards and the wrong kind. `rr:resource.4`: "many abilities
        // require specific resource types, and the specified types in the
        // specified quantities must be generated **in order to pay the cost**"
        // -- so a cost of three physicals is not a cost of three.
        //
        // The board is the same as the paying one except for the letter on the
        // cards, which is what makes this a test of the types rather than of
        // the count.
        Card? horn = null;
        var(game, world) = Playing(board =>
        {
            horn = board.CreateCard(AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));
            // Through the reveal: `rr:attach-to` makes "Attach to Rhino" a
            // rule about the card entering play rather than a "When
            // Revealed" ability, so the route in is what attaches it.
            board.Abilities = AuthoredCards.Runner();
            Reveal.Resolve(board, CardCatalogData, horn, 0, []);
            Hand(board, Mentals, 3);
        }, hero: true);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
        // And paying with them anyway is refused by name rather than half-paid:
        // `rr:initiating-abilities.step.5` aborts "without paying any costs".
        var ability = Assert.Single(AuthoredCards.Runner().Actions(world, 0).Where(pending => pending.Card == horn!.ObjectId).DefaultIfEmpty(new PendingAbility(horn!.ObjectId, AbilityType.Action, 0)));
        int before = world.Seats[0].Hand.Cards.Count;
        var thrown = Assert.Throws<RulesNotImplementedException>(() => AuthoredCards.Runner().Act(world, ability, [..world.Seats[0].Hand.Cards.Select(card => card.ObjectId)], []));
        Assert.Contains("requiring 'RRR'", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(before, world.Seats[0].Hand.Cards.Count);
    }
}
