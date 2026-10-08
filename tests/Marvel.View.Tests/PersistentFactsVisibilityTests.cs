using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.View.Tests;

public sealed class PersistentFactsVisibilityTests
{
    [Fact]
    public void ControlledSupportRetainsNamedCountersAndTheirActivationCost()
    {
        World world = PersistentFixture.Board();
        Card team = world.CreateCard("01064", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        team.PlaceTokens("c_snoop", 2);
        CardDescriptor described = PersistentFixture.Card(world, team);
        Assert.Equal(new CardRelationDescriptor("Controlled", null, 0), described.Persistent!.Relation);
        Assert.Equal(2, described.State!.Counters["snoop"]);
        CardPersistentCostDescriptor counter = Assert.Single(Assert.Single(described.Persistent.Abilities).Costs,
            cost => cost.Operation == "RemoveCounters");
        Assert.Equal("Source", counter.Target);
        Assert.Equal("snoop", counter.Counter);
        Assert.Equal(1, counter.Amount);
        Assert.True(team.Ready);
        Assert.Equal(2, team.Tokens["c_snoop"]);
    }

    [Fact]
    public void HiddenSourceHasNoPersistentFaceAndHiddenHostHasNoRelationshipOrContributionLink()
    {
        World world = PersistentFixture.Board();
        Card rhino = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        Card charge = PersistentFixture.Attach(world, "01099", rhino);
        charge.TurnFaceDown();
        CardDescriptor hiddenSource = PersistentFixture.Card(world, charge);
        Assert.Null(hiddenSource.Face);
        Assert.Null(hiddenSource.Persistent);
        charge.TurnFaceUp();
        rhino.TurnFaceDown();
        CardPersistentDescriptor visibleSource = PersistentFixture.Facts(world, charge);
        Assert.Equal("Attached", visibleSource.Relation.Kind);
        Assert.Null(visibleSource.Relation.HostId);
        Assert.Empty(visibleSource.Contributions);
        Assert.NotEmpty(visibleSource.Abilities);
    }

    [Fact]
    public void DiscardedSourcesHaveNoLiveFactsAndHistoricalEffectsNeverRebindToReturnedCopies()
    {
        World world = PersistentFixture.Board();
        Card hero = world.Seats[0].IdentityCard;
        Card armor = world.CreateCard("01036", PersistentFixture.Controlled(world));
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 2, armor.ObjectId, hero.ObjectId,
            Duration.UntilEndOf(TimingPoints.EndOfRound)));
        World.MoveToTop(armor, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        Assert.Null(PersistentFixture.Card(world, armor).Persistent);
        World.MoveToTop(armor, PersistentFixture.Controlled(world));
        CardPersistentDescriptor live = PersistentFixture.Facts(world, armor);
        Assert.Equal("HP", Assert.Single(live.Contributions).Attribute);
        CardValueSourceDescriptor historical = Assert.Single(PersistentFixture.Card(world, hero).Face!
            .EffectiveValues["ATK"].Calculation).Source!;
        Assert.True(historical.Historical);
        Assert.Null(historical.CardId);
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Empty(PersistentFixture.Card(world, hero).Face!.EffectiveValues["ATK"].Calculation);
    }

    [Fact]
    public void UnsupportedPartOrConditionNeverProducesAPartialOrUnconditionalAbility()
    {
        // Synthetic checked DSL isolates safe fallback; this is not extra playable content.
        World world = PersistentFixture.Board();
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01035","name":"Synthetic","abilities":[
              {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
               "cost":{"exhaust":"this"},"effect":{"seq":[{"ready":"you"},{"draw":{"player":"you","count":1}}]}},
              {"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
               "when":{"inForm":{"player":"you","form":"hero"}},"effect":{"ready":"you"}}
            ]}]}
            """));
        Card source = world.CreateCard("01035", PersistentFixture.Controlled(world));
        CardPersistentDescriptor facts = PersistentFixture.Facts(world, source);
        Assert.True(facts.HasUnresolvedAbilities);
        Assert.Empty(facts.Abilities);
        Assert.Equal("Arc Reactor", facts.Source.Title);
        Assert.NotEmpty(facts.Source.RulesText);
    }

    [Fact]
    public void FacedownDroneNeverPublishesTheUnderlyingPlayerCardsAbility()
    {
        World world = PersistentFixture.Board();
        Card drone = world.CreateCard("01035", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        drone.AssignProfile(AbilityLowering.Book(PersistentFixture.Abilities).Profiles["effective-drone"]);
        drone.TurnFaceDown();
        CardDescriptor described = PersistentFixture.Card(world, drone);
        Assert.Equal("Drone", described.Face!.Title);
        Assert.Null(described.Persistent);
        string json = JsonSerializer.Serialize(described);
        Assert.DoesNotContain("Arc Reactor", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ActingIdentity", json, StringComparison.Ordinal);
    }

    [Fact]
    public void AnIntrinsicDefinitionRemainsADefinitionRatherThanAnAdditiveBonus()
    {
        World world = PersistentFixture.Board();
        Card titania = world.CreateCard("01162", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        titania.TakeDamage(2);
        CardContributionDescriptor row = Assert.Single(PersistentFixture.Facts(world, titania).Contributions);
        Assert.Equal("ATK", row.Attribute);
        Assert.Equal("DefineBase", row.Operation);
        Assert.Equal(4, row.Amount);
        Assert.Equal(titania.ObjectId, row.TargetId);
        Assert.False(PersistentFixture.Card(world, titania).Face!.EffectiveValues["ATK"].IsModified);
    }

    [Fact]
    public void SyntheticHeroAttachmentUsesItsCheckedDirective()
    {
        // Synthetic attachment placement exercises the hero role without
        // authoring an additional playable card or parsing its printed text.
        World world = PersistentFixture.Board();
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01035","name":"Synthetic", "attachTo":{"query":"heroes"},"abilities":[]}]}
            """));
        Card source = world.CreateCard("01035", PersistentFixture.Controlled(world));
        Assert.Equal("Attached", PersistentFixture.Facts(world, source).Relation.Kind);
        Assert.Equal(world.Seats[0].IdentityCard.ObjectId, PersistentFixture.Facts(world, source).Relation.HostId);
    }

    [Rule("rr:attach-to.1")]
    [Fact]
    public void DefeatingAnAllyDiscardsItsAttachmentAndRemovesAllLiveContributionFacts()
    {
        // "if the game element an attachment is attached to leaves play, the
        // attachment is discarded." Exercise the engine's defeat finalization.
        World world = PersistentFixture.Board();
        Card ally = world.CreateCard("01084", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Card inspired = world.CreateCard("01074", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0),
            cardOwner: 0, host: ally.ObjectId));
        Assert.Equal(2, PersistentFixture.Facts(world, inspired).Contributions.Count);
        world.CreateCard("01091", world.Seats[0].Deck);
        ally.TakeDamage(3);
        Defeat.FinalizeCharacter(world, PersistentFixture.Cards, ally, "FixtureDefeat", []);
        Assert.Equal(DeckType.DiscardPile, inspired.Area.Type);
        Assert.Null(PersistentFixture.Card(world, inspired).Persistent);
        Assert.Null(PersistentFixture.Card(world, ally).Persistent);
    }

    [Fact]
    public void ProjectionAndBoardPassThroughPreserveSemanticsWithoutChangingStateOrRandomness()
    {
        World world = PersistentFixture.Board();
        Card rhino = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        Card suit = PersistentFixture.Attach(world, "01098", rhino);
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        ContinuousEffect[] registered = world.Effects.Registered.ToArray();
        WorldDescriptor visible = PersistentFixture.Visible(world);
        string serialized = JsonSerializer.Serialize(visible);
        for (int i = 0; i < 3; i++) Assert.Equal(serialized, JsonSerializer.Serialize(PersistentFixture.Visible(world)));
        WorldDescriptor restored = JsonSerializer.Deserialize<WorldDescriptor>(serialized)!;
        CardPersistentDescriptor facts = VisibilityFixture.Card(restored, suit.ObjectId).Persistent!;
        BoardCardPresentation board = Assert.Single(BoardPresentation.From(restored).Areas.SelectMany(area => area.Cards),
            card => card.TargetId == suit.ObjectId);
        Assert.Equal(JsonSerializer.Serialize(facts), JsonSerializer.Serialize(board.Persistent));
        Assert.Equal("RedirectAllDamage", Assert.Single(facts.Abilities).Effects[0].Operation);
        Assert.Equal(5, facts.Abilities[0].Effects[1].Condition!.Minimum);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(registered, world.Effects.Registered);
    }
}
