using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The authored response with a synthetic simultaneous attack; the pack remains excluded.
public sealed class MomentOfTriumphActualSimultaneousTests
{
    [Rule("rr:overkill.3")]
    [Rule("rr:triggering-condition.2")]
    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 1)]
    public void ResponseUsesTheSelectedDefeatedRecipientsExcess(bool second, int healed)
    {
        // rr:overkill.3 counts the same excess as overkill; the single attack's
        // separate triggering instances retain their own defeated recipients.
        var book = AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01004","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"attack":{"target":{"query":"villain"},"effect":{
                  "dealAttackDamage":{"cards":{"query":"minions"},"amount":5,"simultaneous":1}
                }}}
              }]}
            ]}
            """);
        var runner = new AbilityRunner(book with
        {
            Abilities = [.. book.Abilities, .. AuthoredCards.Book.On("12030")],
            Authored = book.Authored.Append("12030").ToHashSet(StringComparer.Ordinal),
        });
        Card? source = null;
        Card? payment = null;
        Card? response = null;
        Card? first = null;
        Card? other = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01004", board.Seats[0].Hand);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
            response = board.CreateCard("12030", board.Seats[0].Hand);
            var area = board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
            first = board.CreateCard("01102", area);
            other = board.CreateCard("01130", area);
            foreach (var enemy in new[] { first, other })
                board.Effects.Register(new(EffectSource.LastingEffect, "health", 10, Affects: enemy.ObjectId));
            first.TakeDamage(DamagePlacement.Health(board, board.Facts, first) - 2);
            other.TakeDamage(DamagePlacement.Health(board, board.Facts, other) - 4);
            board.Seats[0].IdentityCard.TakeDamage(6);
        }, hero: true, abilities: runner);
        int firstIncarnation = first!.Incarnation;
        int secondIncarnation = other!.Incarnation;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        int[] paying = [payment!.ObjectId];
        game.Resolve(new Decision(action.Id, [], paying,
            Allocations: ResourcePayment.Allocate(Assert.Single(action.Costs!), paying)));
        var occurrence = world.Agenda.Occurrence!;
        Assert.Contains(occurrence.Defeats, item => item.Card == first.ObjectId && item.Incarnation == firstIncarnation);
        Assert.Contains(occurrence.Defeats, item => item.Card == other.ObjectId && item.Incarnation == secondIncarnation);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, other.Area.Type);
        int selected = second ? other.ObjectId : first.ObjectId;
        var choice = Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == response!.ObjectId && option.ContextCardIds.Contains(selected));
        Assert.Contains($"up to {healed}", choice.Description);
        Assert.Contains("limited by damage present", choice.Description);
        game.Resolve(Decision.Take(choice.Id));
        Assert.Equal(6 - healed, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, response!.Area.Type);
    }
}
