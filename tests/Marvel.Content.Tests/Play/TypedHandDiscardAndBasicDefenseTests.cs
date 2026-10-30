using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Synthetic contracts for the <c>discardResourceFromHand</c> cost and the
/// <c>heroBasicDefended</c> condition. Each replaces Zola's Algorithm's row in
/// the authored book and puts it in a non-first player's obligations area; no
/// played scenario is claimed.
/// </summary>
public sealed class TypedHandDiscardAndBasicDefenseTests : CoreCardTestBase
{
    // Zola's Algorithm (04163): after an attack its holder's hero basic defended,
    // they take 1 damage; Action: discard a physical resource → discard this.
    private const string Synthetic = """
        [{"trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","actor":"enemy","target":"you"},
          "when":{"heroBasicDefended":"you"},
          "effect":{"dealDamage":{"cards":"you","amount":1}}},
         {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
          "cost":{"discardResourceFromHand":"R"},
          "effect":{"discard":"this"}}]
        """;

    [Theory]
    [InlineData("""{"heroBasicDefended":"trigger"}""", "expected 'you'")]
    [InlineData("""{"heroBasicDefended":"you","extra":1}""", "is not a node")]
    public void AMalformedBasicDefenseConditionIsRefused(string when, string says)
    {
        var thrown = Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse("""
            {"cards":[{"card":"04163","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","actor":"enemy","target":"you"},
              "when":WHEN,"effect":{"dealDamage":{"cards":"you","amount":1}}}]}]}
            """.Replace("WHEN", when, StringComparison.Ordinal))));
        Assert.Contains(says, thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    [Rule("rr:defend-defense.4.3")]
    public void TheConditionDistinguishesABasicDefenseFromADefenseLabeledOne(bool basic, int damage)
    {
        // rr:defend-defense.4.3: "Resolving a defense-labeled ability is not a basic
        // defense." Spider-Man (DEF 3) either basic defends Rhino's 2 or plays
        // Backflip (cost 0) to prevent it; only the basic defense satisfies it.
        var (world, f) = Board();
        var backflip = world.CreateCard("01003", world.Seats[1].Hand);
        KangTemporalFixture.Attack(world, f.Villain, 1);

        KangTemporalFixture.Drive(world, f.Runner, prompt => prompt.Affordances.FirstOrDefault(a => prompt.Player == 1
            && (basic ? a.Verb == "Defense" : a.AnchorId == backflip.ObjectId)) is { } choice
            ? Decision.Take(choice.Id) : null);

        Assert.Equal(basic ? DeckType.HandsArea : DeckType.DiscardPile, backflip.Area.Type);
        Assert.Equal(damage, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:wild-resource.3")]
    public void TheTypedDiscardOffersOnlyCardsPrintingThatResource()
    {
        // rr:wild-resource.3: outside generation "a wild resource does not have any
        // characteristic other than 'wild resource'". Strength (RR) is offered;
        // Vibranium (GG) and Energy (YY) are not. Paying discards the chosen card.
        var (world, f) = Board();
        var zola = world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)).Cards[0];
        var hand = world.Seats[1].Hand;
        var strength = world.CreateCard("01090", hand);
        world.CreateCard("01044", hand);
        world.CreateCard("01088", hand);
        var action = Assert.Single(f.Runner.Actions(world, 1), ability => ability.Card == zola.ObjectId);
        Assert.Equal([strength.ObjectId], f.Runner.Describe(world, action).Targets!.Legal);

        f.Runner.ExecuteAction(world, new PlayerAction(action, [], [strength.ObjectId]));
        f.Finish();

        Assert.Equal(DeckType.DiscardPile, strength.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, zola.Area.Type);
        Assert.Equal(2, hand.Cards.Count);
    }

    [Fact]
    public void TheTypedDiscardIsUnavailableWithoutAQualifyingCard()
    {
        var (world, f) = Board();
        var zola = world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)).Cards[0];
        world.CreateCard("01044", world.Seats[1].Hand);

        Assert.DoesNotContain(f.Runner.Actions(world, 1), ability => ability.Card == zola.ObjectId);
    }

    // The authored book with Zola's Algorithm's abilities replaced by the synthetic ones.
    private static (World World, RedSkullFixture Fixture) Board()
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var row = book["cards"]!.AsArray().Single(card => (string?)card!["card"] == "04163")!;
        row["abilities"] = JsonNode.Parse(Synthetic);
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var (world, _) = KangTemporalFixture.Board(players: 2, encounter: 2);
        world.Abilities = runner;
        KangTemporalFixture.Obligations(world, 1, 1);
        return (world, new RedSkullFixture(world, runner));
    }
}
