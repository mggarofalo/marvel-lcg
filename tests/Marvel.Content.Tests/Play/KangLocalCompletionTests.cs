using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Prepared nonpublic completion boundaries; actual first-split coverage remains separate.
public sealed class KangLocalCompletionTests : CoreCardTestBase
{
    [Theory]
    [InlineData("11009b", false)]
    [InlineData("11010b", false)]
    [InlineData("11011b", false)]
    [InlineData("11012b", false)]
    [InlineData("11009b", true)]
    [InlineData("11010b", true)]
    [InlineData("11011b", true)]
    [InlineData("11012b", true)]
    public void EachLocalCompletionRaisesBeforeCompletionLossOrAdvancement(string face, bool nextStage)
    {
        // 11009b-11012b: "place 1 set-aside Kang's Dominion facedown under stage 4A"
        // followed by phase-end removal and reunion. The unsupported boundary is
        // an engine policy; neither ordinary loss nor sequential advance applies.
        var world = Hero("01001a", 2);
        world.Join(PlayArea.Of(0), world.CreateGameArea(), "prepared local area", []);
        var scheme = world.CreateCard(face, world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(0)));
        Card? next = nextStage
            ? world.CreateCard("01097a,01097b", world.AreaOf(DeckType.MainSchemesDeck)) : null;
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();
        Threat.Schedule(world, new ThreatBatch([scheme], null, 8, ThreatCause.CardAbility, "prepared threat", 0));
        Sequence.Finish(world, Cards, abilities, events);
        Assert.Equal(8, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(Outcome.Unfinished, world.Result);

        Threat.Schedule(world, new ThreatBatch([scheme], null, 1, ThreatCause.CardAbility, "prepared completion", 0));
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Finish(world, Cards, abilities, events));
        Assert.Contains(face, error.Message, StringComparison.Ordinal);
        Assert.Contains("Kang's Dominion", error.Message, StringComparison.Ordinal);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("is_completed"));
        Assert.Equal(DeckType.MainSchemesArea, scheme.Area.Type);
        Assert.DoesNotContain(events.OfType<FieldSet>(), change => change.Field == "is_completed");
        if (next is not null) Assert.Equal(DeckType.MainSchemesDeck, next.Area.Type);
    }

    [Theory]
    [InlineData("01097b")]
    [InlineData("04063b")]
    [InlineData("11013b")]
    [Rule("rr:main-scheme-main-scheme-deck.2.1")]
    public void SupportedFinalSchemeStillLosesWithUnrelatedKangStagesSetAside(string face)
    {
        // "If the villain completes the final stage ... the villain wins the game."
        // Set-aside scenario cards cannot change the completing scheme's policy.
        var world = Hero("01001a");
        var scheme = world.CreateCard(face, world.AreaOf(DeckType.MainSchemesArea));
        foreach (string localFace in new[] { "11009b", "11010b", "11011b", "11012b" })
            world.CreateCard(localFace, world.AreaOf(DeckType.AsideDeck));
        var events = new List<GameEvent>();
        Threat.Schedule(world, new ThreatBatch([scheme], null, Modified(world, scheme, "target_threat"),
            ThreatCause.CardAbility, "prepared completion", 0));

        Sequence.Finish(world, Cards, new NoCardAbilities(), events);

        Assert.Equal(Outcome.VillainWins, world.Result);
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("is_completed"));
        Assert.Single(events.OfType<FieldSet>(), change => change.Field == "is_completed");
    }
}
