using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HealingPaymentCommitmentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.3")]
    [Rule("rr:initiating-abilities.step.5")]
    public void HealingCostMustRemainPayableAfterAResourceGeneratorSuspends(bool healedBeforeCommit)
    {
        // Rechannel's spend-and-heal cost shape is exercised with a synthetic
        // generator whose defeat interrupt lets another effect heal the identity first.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"seq":[{"spend":"Y"},{"heal":{"card":"you","amount":1}}]},
                "effect":{"draw":{"player":"you","count":1}}
              }]},
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},"effect":{"generate":"Y"}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        Card? source = null;
        Card? generator = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            var allies = board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
            generator = board.CreateCard("01030", allies);
            generator.TakeDamage(2);
            board.Seats[0].IdentityCard.TakeDamage(1);
            InPlay(board, "01092");
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances,
            offer => offer.Verb == Game.ActionVerb && offer.AnchorId == source!.ObjectId);
        int[] paying = [generator!.ObjectId];
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(Assert.Single(action.Costs!), paying));
        game.Resolve(new Decision(action.Id, [], paying, Allocations: allocation));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        var identity = world.Seats[0].IdentityCard;
        if (healedBeforeCommit) DamageRecovery.Heal(world, world.Facts, identity, 1, "test", "Heal", []);
        int handBefore = world.Seats[0].Hand.Cards.Count;
        if (healedBeforeCommit)
        {
            // Step 5 says to "abort this process" when a required cost cannot be paid.
            var error = Assert.Throws<RulesNotImplementedException>(() => game.Resolve(Decision.Decline));
            Assert.Contains("required 1 damage", error.Message, StringComparison.Ordinal);
            Assert.Equal(handBefore, world.Seats[0].Hand.Cards.Count);
            Assert.Equal(0, identity.Damage);
            return;
        }
        game.Resolve(Decision.Decline);
        Assert.Equal(handBefore + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(0, identity.Damage);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }
}
