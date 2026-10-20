using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AuxiliaryDeckAbilityTests
{
    [Theory]
    [InlineData("experimentalWeapons", DeckType.ExperimentalWeaponsDeck)]
    [InlineData("sideSchemes", DeckType.SideSchemeDeck)]
    [Rule("rr:reveal.step.4")]
    public void AuxiliaryTopUsesTheEncounterRevealProcedure(string deck, DeckType expected)
    {
        // Synthetic Core cards isolate the scenario-deck primitive from expansion admission.
        // rr:reveal.step.4: "If a player is instructed by card text to reveal an encounter card
        // from the encounter deck or any other game area, this same resolution procedure applies."
        Card? source = null;
        Card? encounter = null;
        Card? unrelated = null;
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$"""
            {"seq":[
                {"initializeAuxiliaryDeck":{"deck":"{{{{{deck}}}}}","cards":{"cardsIn":{"area":"scenarioSetAside","title":"Bomb Scare"}}}},
                {"revealAuxiliaryTop":"{{{{{deck}}}}}"}
            ]}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            encounter = board.CreateCard(AuthoredCards.BombScare, board.AreaOf(DeckType.AsideDeck));
            unrelated = board.CreateCard(AuthoredCards.HydraBomber, board.AreaOf(DeckType.AsideDeck));
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Empty(Assert.Single(world.Areas, area => area.Type == expected).Cards);
        Assert.Equal(DeckType.AsideDeck, unrelated!.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, encounter!.Area.Type);
        Assert.True(encounter.FaceUp);
        // Bomb Scare starts with 2 and reveals with an additional 1 per hero.
        Assert.Equal(3, encounter.Tokens.GetValueOrDefault("k_threat"));
    }
}
