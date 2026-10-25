using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingThwartPermissionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cannot")]
    public void PermissionDistinguishesThwartingFromOtherThreatRemoval(bool thwart)
    {
        // Held Hostage: "Threat cannot be removed from attached scheme by thwarting."
        // rr:cannot: "The word 'cannot' is absolute"; the printed qualifier still determines its scope.
        Card? scheme = null;
        var runner = RunnerFor("""{"removeThreat":{"scheme":{"titled":"Breakin' & Takin'"},"amount":3}}""");
        var (_, world) = Playing(board => scheme = ProtectedScheme(board), abilities: runner);
        Assert.True(runner.CanRemoveThreat(world, scheme!));
        Assert.False(runner.CanThwartScheme(world, scheme!));
        Assert.DoesNotContain(scheme!, BasicPowers.Thwartable(world, world.Facts, 0));
        var outcome = Threat.Remove(world, world.Facts, runner,
            new(scheme!, 3, "test", "Remove_Threat") { ByThwarting = thwart }, []);
        Assert.Equal(thwart ? 0 : 3, outcome);
        Assert.Equal(thwart ? 5 : 2, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AbilityAdmissionUsesItsThwartLabelAndRetainsNonThwartRemoval(bool thwart)
    {
        // Synthetic player action isolates a labeled thwart from identically sized non-thwart removal.
        const string removal = """{"removeThreat":{"scheme":{"titled":"Breakin' & Takin'"},"amount":3}}""";
        var effect = thwart ? """{"thwart":{"target":{"titled":"Breakin' & Takin'"},"effect":EFFECT}}"""
            .Replace("EFFECT", removal, StringComparison.Ordinal) : removal;
        var runner = RunnerFor(effect);
        Card? source = null;
        Card? scheme = null;
        var (game, world) = Playing(board =>
        {
            scheme = ProtectedScheme(board);
            source = InPlay(board, "01007");
        }, abilities: runner);
        var actions = game.Pending!.Affordances.Where(offer => offer.AnchorId == source!.ObjectId).ToArray();
        Assert.Equal(thwart ? 0 : 1, actions.Length);
        if (thwart) return;
        game.Resolve(Decision.Take(actions[0].Id));
        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void RemovingOneOfTwoCopiesDoesNotRemoveTheOtherProhibition()
    {
        var runner = RunnerFor("""{"draw":{"player":"you","count":1}}""");
        Card? scheme = null;
        var (_, world) = Playing(board => scheme = ProtectedScheme(board), abilities: runner);
        var first = world.Cards.Single(card => card.FaceId == "07005");
        var second = world.CreateCard("07005", first.Area);
        Assert.False(runner.CanThwartScheme(world, scheme!));
        World.MoveToTop(first, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.False(runner.CanThwartScheme(world, scheme!));
        World.MoveToTop(second, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.True(runner.CanThwartScheme(world, scheme!));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void AreaAndPowerProjectionRespectDepartedProhibitions(bool thwart, bool departed)
    {
        var (world, groups, original) = WreckingProjectionFixture.Create();
        var scheme = groups[0].Scheme;
        scheme.PlaceTokens("k_threat", 5);
        var attachment = world.CreateCard("07005", world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Villains, scheme.ObjectId));
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"07005","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"preventThwarting":"attachedTo"}}]}]}
            """);
        var context = original.Context with { Program = AbilityLowering.Book(book),
            Power = thwart ? BasicPowers.ThwartVerb : null };
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var power = AbilityPowerStateFactory.Initial(scope);
        var area = new AreaProjectionState(context);
        if (departed)
        {
            power.Discarded.Add(attachment.ObjectId);
            area.Departed.Add(attachment.ObjectId);
        }
        var removal = new AbilityEffect.RemoveThreat(new AbilityCardSelection.Titled("Day of Reckoning"),
            new AbilityNumber.Constant(3), false, null);
        var changed = AbilityPowerStateMutation.ApplyPowerLeafState(removal, scope, false, power);
        var projection = new AbilityAreaProjection([removal], null, new HashSet<DeckType>(), context, 1);
        var changedArea = Assert.Single(projection.Trace(removal, [area]));
        long expected = thwart && !departed ? 5 : 2;
        Assert.Equal(expected, changed.SchemeThreat.GetValueOrDefault(scheme.ObjectId, 5));
        Assert.Equal(expected, changedArea.ThreatOf(scheme));
        Assert.Equal(5, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    public void AreaProjectionUsesChangedAttachmentHost()
    {
        var (world, groups, original) = WreckingProjectionFixture.Create();
        foreach (var group in groups) group.Scheme.PlaceTokens("k_threat", 5);
        var attachment = world.CreateCard("07005", world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Villains, groups[0].Scheme.ObjectId));
        var context = original.Context.WithPower(BasicPowers.ThwartVerb);
        var area = new AreaProjectionState(context);
        area.Hosts[attachment.ObjectId] = groups[1].Scheme.ObjectId;
        var removal = new AbilityEffect.RemoveThreat(new AbilityCardSelection.Query(AbilityCardQuery.SideSchemes),
            new AbilityNumber.Constant(3), false, null);
        var projection = new AbilityAreaProjection([removal], null, new HashSet<DeckType>(), context, 1);
        var changed = Assert.Single(projection.Trace(removal, [area]));
        Assert.Equal(2, changed.ThreatOf(groups[0].Scheme));
        Assert.Equal(5, changed.ThreatOf(groups[1].Scheme));
        Assert.Equal(groups[0].Scheme.ObjectId, attachment.Area.Host);
    }

    private static Card ProtectedScheme(World world)
    {
        var scheme = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 5);
        world.CreateCard("07005", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, scheme.ObjectId));
        return scheme;
    }

    private static AbilityRunner RunnerFor(string effect) => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"07005","abilities":[{
          "trigger":{"timing":"Constant","subject":"this"},"effect":{"preventThwarting":"attachedTo"}}]},
          {"card":"01007","abilities":[{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
            "effect":EFFECT}]}]}
        """.Replace("EFFECT", effect, StringComparison.Ordinal)));
}
