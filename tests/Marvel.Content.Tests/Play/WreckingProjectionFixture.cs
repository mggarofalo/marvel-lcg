using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

internal sealed class WreckingProjectionFixture : CoreCardTestBase
{
    internal static (World, EncounterDeckGroup[], AbilityAdmission.AbilityAdmissionScope) Create(string? firstSuccessor = null)
    {
        var world = Hero("01001a");
        string[][] faces = [["07002", "07003", "07004"], ["07017", "07018", "07019"],
            ["07032", "07033", "07034"], ["07046", "07047", "07048"]];
        if (firstSuccessor is not null) faces[0][1] = firstSuccessor;
        var groups = faces.Select(pair => Group(world, pair)).ToArray();
        world.BindEncounterTopology(new EncounterTopology(world, groups, groups[1].Villain));
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var query = new AbilityQueryContext(world, source, new Occurrence(0, []), 0,
            source.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var scope = new AbilityAdmission.AbilityAdmissionScope(new AbilityAdmissionContext(
            AbilityLowering.Book(AuthoredCards.Book), world.ResourceAbilities, expression, new(), null), []);
        return (world, groups, scope);
    }

    private static EncounterDeckGroup Group(World world, string[] faces)
    {
        var first = world.CreateCard(faces[0], world.AreaOf(DeckType.VillainArea));
        var next = world.CreateCard(faces[1], world.AreaOf(DeckType.VillainDeck));
        return new(first, world.CreateCard(faces[2], world.AreaOf(DeckType.SideSchemesArea)),
            world.CreateArea(DeckType.EncounterDeck), world.CreateArea(DeckType.EncounterDiscardPile),
            [], [first, next]);
    }
}
