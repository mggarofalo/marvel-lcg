using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class TargetReferenceANoOpFormChangeTests : TargetReferenceTestBase
{
    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void ANoOpFormChangeDoesNotOpenTheLaterFormBranch()
    {
        // Changing to the form already shown is a no-op. It cannot make the
        // later alter-ego branch reachable or let that branch's prohibited
        // target hide the valid hero thwart.
        var runner = Runner("01006", """
            { "seq": [
              { "changeForm": { "player": "you", "to": "hero" } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01001a");
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        ResolveAction(game, source!);
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:form-change-form.2")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void ADeterministicFormRestorationClosesTheEarlierFormBranch()
    {
        // A form change “changes from their current form to their other form.”
        // The second change therefore returns the identity to hero form before
        // the thwart. The earlier alter-ego state cannot make the prohibited
        // branch reachable when the target is checked at initiation.
        var runner = Runner("01006", """
            { "seq": [
              { "changeForm": { "player": "you", "to": "alter-ego" } },
              { "changeForm": { "player": "you", "to": "hero" } },
              { "if": {
                "test": { "inForm": { "player": "you", "form": "hero" } },
                "then": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } }
                } },
                "else": { "thwart": {
                  "target": { "query": "mainScheme" },
                  "effect": { "removeThreat": {
                    "scheme": { "query": "mainScheme" }, "amount": 1
                  } }
                } }
              } }
            ] }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01001a");
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        Assert.Contains(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        Assert.Equal("01001a", world.Seats[0].IdentityCard.FaceId);
    }

    [Rule("rr:target.6")]
    [Fact]
    public void ASearchNeedsAnAreaRatherThanAMatchingCard()
    {
        // “An ability with a search effect requires only a searchable game
        // area.” No card matches the invented id, but the encounter deck is a
        // searchable area, so the action remains initiable.
        var runner = Runner("01006", """{ "search": { "in": [ { "encounterDeck": 1 } ], "for": "missing-card" } }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, "01006", DeckType.SupportsArea), runner);
        Resolution resolved = ResolveAction(game, source!);
        Assert.Contains(resolved.Information, signal => signal.Kind == InformationKind.Search);
    }

    [Fact]
    public void AShortCircuitedConcealedQueryDoesNotRecordASearch()
    {
        var runner = Runner("01006", """
            { "if": {
              "test": { "and": [
                { "exists": { "query": "minions" } },
                { "exists": { "cardsIn": { "area": "yourDeck", "kind": "Upgrade" } } }
              ] },
              "then": { "placeCounters": { "card": "this", "counter": "test", "count": 1 } },
              "else": { "placeCounters": { "card": "this", "counter": "test", "count": 1 } }
            } }
            """);
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, "01006", DeckType.SupportsArea), runner);
        Resolution resolved = ResolveAction(game, source!);
        Assert.DoesNotContain(resolved.Information, signal => signal.Kind == InformationKind.Search);
    }
}
