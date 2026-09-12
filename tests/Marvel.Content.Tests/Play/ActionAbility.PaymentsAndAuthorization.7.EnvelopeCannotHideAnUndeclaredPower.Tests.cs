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
public sealed class ActionAbilityPaymentsAndAuthorizationEnvelopeCannotHideAnUndeclaredPowerTests
{
    [Rule("rr:labeled-ability.5")]
    [Rule("rr:labeled-ability.6")]
    [Fact]
    public void EnvelopeCannotHideAnUndeclaredPower()
    {
        // The envelope's labels are the whole set. An attack-only ability may
        // not append a thwart that skips Confused merely because the attack
        // already persisted its performer into the continuation.
        var runner = Runner("01017", "Action", """
            { "seq": [
              { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } },
              { "thwart": {
                "target": { "query": "mainScheme" },
                "effect": { "removeThreat": {
                  "scheme": { "query": "mainScheme" }, "amount": 1
                } }
              } }
            ] }
            """, cost: """{ "exhaust": "this" }""", labels: "[ \"attack\" ]");
        Card? source = null;
        var(_, world) = Playing(board =>
        {
            source = board.CreateCard("01017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Confused);
        }, hero: true);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long threat = scheme.Tokens.GetValueOrDefault("k_threat");
        var forged = new PendingAbility(source!.ObjectId, AbilityType.Action, 0);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, forged, [], []));
        Assert.Contains("absent from its ability labels", thrown.Message);
        Assert.True(source.Ready);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(0, villain.Damage);
        Assert.Equal(threat, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:lasting-effects.6")]
    [Fact]
    public void AnUntilEndOfAttackEffectCannotBeginOutsideAnAttack()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "grantUntil": { "card": "this", "keyword": "attack", "amount": 1, "until": "EndOfAttack" } }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:action.2.2")]
    [Rule("rr:forced.3")]
    [Rule("rr:forced.3.1")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnIllegalForcedActionDoesNotPreventPhaseCompletion(bool targetless)
    {
        string effect = targetless ? """{ "chooseCard": { "from": { "query": "minions" }, "effect": { "draw": { "player": "you", "count": 1 } } } }""" : """{ "draw": { "player": "you", "count": 1 } }""";
        string? cost = targetless ? null : """{ "spend": "BBBBBBBBBBBB" }""";
        var runner = Runner(AuthoredCards.AuntMay, "ForcedAction", effect, cost: cost);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.True(source!.Ready);
    }
}
