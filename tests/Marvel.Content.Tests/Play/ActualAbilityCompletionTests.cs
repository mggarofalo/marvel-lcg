using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ActualAbilityCompletionTests
{
    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Rule("rr:target.4.1")]
    [Theory]
    [InlineData("exhaust", false, 1)]
    [InlineData("exhaust", true, 1)]
    [InlineData("ready", false, 1)]
    [InlineData("ready", true, 1)]
    public void ThenCountsOnlyValidReadinessTargets(string operation, bool alreadyChanged, int drawn)
    {
        // Synthetic contract: "the text preceding ... then must be fully true or
        // resolved." A multi-target effect does not resolve against invalid
        // elements, so already exhausted/ready characters do not prevent "then".
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"then":{"effect":{"OPERATION":{"query":"charactersYouControl"}},
              "then":{"draw":{"player":"you","count":1}}}}
            """.Replace("OPERATION", operation, StringComparison.Ordinal));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            var ally = board.CreateCard("01002", board.AreaOf(
                DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            if (operation == "ready") board.Seats[0].IdentityCard.Exhaust();
            if ((operation == "ready") != alreadyChanged) ally.Exhaust();
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId).Id));

        Assert.Equal(held + drawn, world.Seats[0].Hand.Cards.Count);
        Assert.All(world.Seats[0].Hero.Cards, card => Assert.Equal(operation == "ready", card.Ready));
        Assert.True(game.IsRootPrompt);
    }

    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Theory]
    [InlineData(4, 2)]
    [InlineData(3, 0)]
    public void ThenWaitsForEverySiblingChoiceAndUsesTheirCombinedActualResult(int damage, int drawn)
    {
        // "The text preceding ... then must be fully true or resolved."
        // The second heal reads the damage remaining after the first answer.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"then":{"effect":{"seq":[
              {"choose":{"options":[{"heal":{"card":"you","amount":2}},
                                     {"draw":{"player":"you","count":1}}]}},
              {"choose":{"options":[{"heal":{"card":"you","amount":2}},
                                     {"draw":{"player":"you","count":1}}]}}
            ]},"then":{"draw":{"player":"you","count":2}}}}
            """);
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(damage);
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId).Id));
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        game.Resolve(Decision.Take(0));
        Assert.Equal(damage - 2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        game.Resolve(Decision.Take(0));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + drawn, world.Seats[0].Hand.Cards.Count);
        Assert.True(game.IsRootPrompt);
    }

    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Rule("rr:otherwise.1.2")]
    [Theory]
    [InlineData("then", 0, 1)]
    [InlineData("then", 1, 0)]
    [InlineData("then", 2, 0)]
    [InlineData("otherwise", 0, 0)]
    [InlineData("otherwise", 1, 0)]
    [InlineData("otherwise", 2, 1)]
    public void ThreatCompletionWaitsForPreventionBeforeChoosingDependentText(
        string branch, int prevented, int drawn)
    {
        // An "otherwise" effect requires a preceding effect that cannot at least partially resolve.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "effect":{"BRANCH":{"effect":{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":2}},
                  "BRANCH":{"draw":{"player":"you","count":1}}}}}]},
              {"card":"01008","abilities":[{
                "trigger":{"event":"WhenThreatWouldBePlaced","timing":"Interrupt","subject":"game"},
                "cost":{"exhaust":"this"},"effect":{"preventThreat":PREVENTED}}]}
            ]}
            """.Replace("BRANCH", branch, StringComparison.Ordinal)
                .Replace("PREVENTED", Math.Max(1, prevented).ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            InPlay(board, AuthoredCards.WebShooter);
        }, abilities: runner);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long before = scheme.Tokens.GetValueOrDefault("k_threat");
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId).Id));
        Assert.Equal(before, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        var interruption = Assert.Single(game.Pending!.Affordances);
        game.Resolve(prevented == 0 ? Decision.Decline : Decision.Take(interruption.Id));
        Assert.Equal(before + 2 - prevented, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + drawn, world.Seats[0].Hand.Cards.Count);
        Assert.True(game.IsRootPrompt);
    }
}
