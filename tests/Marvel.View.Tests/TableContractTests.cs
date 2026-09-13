using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;

public sealed class TableContractTests
{
    [Fact]
    public void CooperativeTableKeepsDuplicateCardsAndRuntimeAreasDistinctInAllocationOrder()
    {
        World world = Board(out Card duplicateOne, out Card duplicateTwo, out Area future);
        ViewScope scope = new PermissiveVisibilityPolicy().Authorize(null, world.Players);

        WorldDescriptor first = WorldProjection.For(world, null, [], scope, activePlayer: 1).World;
        WorldDescriptor second = WorldProjection.For(world, null, [], scope, activePlayer: 1).World;

        Assert.Equal(first.Areas.Select(area => area.Id), second.Areas.Select(area => area.Id));
        Assert.Equal(future.Id, Assert.Single(first.Areas, area => area.Id == future.Id).Id);
        Assert.Equal(nameof(DeckType.AdditionalDeck), Assert.Single(first.Areas, area => area.Id == future.Id).Zone);
        Assert.Equal(duplicateOne.ObjectId, Card(first, duplicateOne.ObjectId).Id);
        Assert.Equal(duplicateTwo.ObjectId, Card(first, duplicateTwo.ObjectId).Id);
        Assert.Equal("Duplicate", Card(first, duplicateOne.ObjectId).Face?.Title);
        Assert.Equal("Duplicate", Card(first, duplicateTwo.ObjectId).Face?.Title);
        Assert.Equal(1, first.Table?.ActivePlayer);
        Assert.Equal(0, first.Table?.FirstPlayer);
        Assert.Equal(1, first.Table?.PublicFocusSeat);
    }

    [Fact]
    public void RelationshipsAndCompactSummaryCarryOnlyExplicitAuthorizedSubjects()
    {
        World world = Board(out _, out Card remoteDefender, out _);
        Card villain = world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));
        Area engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        Card minion = world.CreateCard("minion", engaged);
        Area attachment = world.CreateArea(
            DeckType.UpgradesArea, cardOwner: 0, playArea: PlayArea.Of(1), host: remoteDefender.ObjectId);
        Card attachmentCard = world.CreateCard("attachment", attachment);
        world.Attack = new EnemyAttack(villain.ObjectId, 0, world.Seats[0].IdentityCard.ObjectId);
        world.Activation = new EnemyActivation(villain.ObjectId, 0, Attacking: true);
        Prompt prompt = Assert.IsType<Prompt>(
            Attack.DeclareDefender(world, world.Facts, new NoCardAbilities())) with
        {
            Affordances = [new Affordance(7, Attack.DefenseVerb, remoteDefender.ObjectId, 1,
                "unknown label", new TargetRequest([villain.ObjectId, minion.ObjectId], 1, 1),
                [new CostOption(remoteDefender.ObjectId, "X", OrCost: "2", Sources:
                    [new ResourceSource(remoteDefender.ObjectId, "W")],
                    Variables: [new VariableRequest("X", 0, 3)],
                    Components: [new ResourceCost("1"), new ResourceCost("1", ["W"])])])],
        };
        ViewScope scope = new PermissiveVisibilityPolicy().Authorize(null, world.Players);
        VisibleResult visible = WorldProjection.For(world, prompt, [], scope, activePlayer: 1);
        WorldDescriptor table = visible.World;
        PromptPresentation presentation = PromptPresentation.From(Assert.IsType<Prompt>(visible.Prompt), table);
        AffordancePresentation affordance = Assert.Single(presentation.Affordances);

        Assert.Contains(table.Relationships, relationship => relationship == new TableRelationshipDescriptor(
            RelationshipKind.Attachment, attachmentCard.ObjectId, remoteDefender.ObjectId));
        Assert.Contains(table.Relationships, relationship => relationship == new TableRelationshipDescriptor(
            RelationshipKind.Engagement, minion.ObjectId, null, 0));
        PlayerSummaryDescriptor second = Assert.Single(table.PlayerSummaries, summary => summary.Seat == 1);
        Assert.Equal([remoteDefender.ObjectId], second.OfferedDefenders);
        Assert.Equal(remoteDefender.ObjectId, affordance.Source?.CardId);
        Assert.Equal(remoteDefender.Area.Id, affordance.Source?.AreaId);
        Assert.Equal([villain.ObjectId, minion.ObjectId], affordance.TargetRequest?.Legal);
        Assert.Equal("X", Assert.Single(affordance.CostOptions).Cost);
        Assert.True(Assert.Single(affordance.CostOptions).HasAlternative);
        Assert.Equal(2, Assert.Single(affordance.CostOptions).ResourceCosts.Count);
        Assert.Equal([villain.ObjectId, minion.ObjectId], affordance.Relationships
            .Where(relationship => relationship.Kind == RelationshipKind.OfferedTarget)
            .Select(relationship => relationship.Related));
    }

    [Fact]
    public void RestrictedSeatSwitchErasesConcealedCardStateAndPromptOwnership()
    {
        World world = Board(out _, out _, out _);
        Card secret = world.CreateCard("secret", world.Seats[1].Hand);
        secret.Exhaust();
        var prompt = new Prompt(1, Question.Element, TimingPriority.Untimed, "search", "secret", false,
            [new Affordance(8, "Choose", secret.ObjectId, 1, "secret")]);

        VisibleResult owner = WorldProjection.For(world, prompt, [],
            new RestrictedVisibilityPolicy(1).Authorize(null, world.Players));
        VisibleResult other = WorldProjection.For(world, prompt, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players));
        CardDescriptor concealed = Assert.Single(Area(other.World, DeckType.HandsArea, 1).Cards);

        Assert.Equal(secret.ObjectId, Card(owner.World, secret.ObjectId).Id);
        Assert.Null(concealed.Id);
        Assert.Null(concealed.Face);
        Assert.Null(concealed.Location);
        Assert.Null(concealed.State);
        Assert.Null(other.Prompt);
        Assert.Null(other.World.Table?.PromptOwner);
        Assert.Equal(0, other.World.Table?.ViewedPrivateSeat);
        Assert.DoesNotContain(other.World.PlayerSummaries,
            summary => summary.EngagedEnemies.Contains(secret.ObjectId)
                || summary.OfferedDefenders.Contains(secret.ObjectId));
    }

    [Fact]
    public void CardControllerComesFromTheCardWhenScenarioCardsSitOnAPlayerSide()
    {
        World world = Board(out Card host, out _, out _);
        Area aside = world.CreateArea(DeckType.AsideDeck, World.Scenario, PlayArea.Of(0));
        Card asideCard = world.CreateCard("villain", aside);
        Card tucked = world.CreateCard("villain", world.AreaOf(DeckType.EncounterDeck));
        Tuck.Card(world, tucked, host, "test", []);

        WorldDescriptor view = WorldProjection.For(world, null, [],
            new PermissiveVisibilityPolicy().Authorize(null, world.Players)).World;

        Assert.Equal(World.Scenario, Card(view, asideCard.ObjectId).Location?.Controller);
        Assert.Equal(World.Scenario, Card(view, tucked.ObjectId).Location?.Controller);
        Assert.Equal(0, Assert.Single(view.Areas, area => area.Id == aside.Id).Owner);
        Assert.Equal(nameof(DeckType.AsideDeck), Card(view, tucked.ObjectId).Location?.Zone);
    }

    [Fact]
    public void DeclaredAnchorKindPreventsCollidingCardAndRuntimeAreaIdsFromChangingSource()
    {
        var card = new CardDescriptor(4, CardBack.Player, true, true, -1,
            new CardFaceDescriptor("card", "Colliding card", string.Empty, CardKind.Ally,
                new Dictionary<string, long>(StringComparer.Ordinal)))
        {
            Location = new CardLocationDescriptor(9, "AlliesArea", 1, -1),
        };
        var world = new WorldDescriptor([new PlayerDescriptor(0, "Zero", false)],
            [new AreaDescriptor(4, "AdditionalDeck", 0, -1, [], []),
                new AreaDescriptor(9, "AlliesArea", 1, -1, [card], [])], [], Outcome.Unfinished);
        var prompt = new Prompt(0, Question.Element, TimingPriority.Untimed, "unknown", "misleading", false,
        [
            new Affordance(1, "Unknown", 4, 0, "unknown")
                { AnchorKind = AffordanceAnchorKind.Area },
            new Affordance(2, "Unknown", 4, 0, "unknown")
                { AnchorKind = AffordanceAnchorKind.Unspecified },
        ]);

        AffordancePresentation[] presented = [.. PromptPresentation.From(prompt, world).Affordances];

        Assert.Equal(AffordanceAnchorKind.Area, presented[0].Source?.AnchorKind);
        Assert.Null(presented[0].Source?.CardId);
        Assert.Equal(4, presented[0].Source?.AreaId);
        Assert.Equal("Additional Deck", presented[0].Anchor);
        Assert.Equal(AffordanceAnchorKind.Unspecified, presented[1].AnchorKind);
        Assert.Null(presented[1].Source);
        Assert.Equal("Object 4", presented[1].Anchor);
    }

    private static World Board(out Card duplicateOne, out Card duplicateTwo, out Area future)
    {
        var world = new World(new Facts(), players: 2, seed: 3);
        Seat zero = world.CreateSeat("Zero");
        Seat one = world.CreateSeat("One");
        Card identityZero = world.CreateCard("hero0", zero.Hero);
        Card identityOne = world.CreateCard("hero1", one.Hero);
        zero.IdentityCard = identityZero;
        one.IdentityCard = identityOne;
        duplicateOne = world.CreateCard("duplicate", world.CreateArea(
            DeckType.AlliesArea, cardOwner: 0, playArea: PlayArea.Of(0)));
        duplicateTwo = world.CreateCard("duplicate", world.CreateArea(
            DeckType.AlliesArea, cardOwner: 1, playArea: PlayArea.Of(1)));
        future = world.CreateArea(DeckType.AdditionalDeck, cardOwner: World.Scenario,
            playArea: PlayArea.Villains);
        world.CreateCard("future", future);
        return world;
    }

    private static AreaDescriptor Area(WorldDescriptor world, DeckType zone, int owner) =>
        Assert.Single(world.Areas, area => area.Zone == zone.ToString() && area.Owner == owner);

    private static CardDescriptor Card(WorldDescriptor world, int id) => Assert.Single(
        world.Areas.SelectMany(area => area.Cards.Concat(area.Removed)), card => card.Id == id);

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero0" or "hero1" => CardKind.Hero,
            "duplicate" => CardKind.Ally,
            "villain" => CardKind.EncounterVillain,
            "minion" => CardKind.Minion,
            "attachment" => CardKind.Attachment,
            _ => CardKind.Event,
        };

        public string Title(string faceId) => faceId switch
        {
            "duplicate" => "Duplicate",
            "hero0" => "Hero Zero",
            "hero1" => "Hero One",
            _ => faceId,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["HP"] = "10",
            };
        public IReadOnlyList<string> Keywords(string faceId) => [];
        public string Text(string faceId) => string.Empty;
        public string FormattedText(string faceId) => string.Empty;
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
                && long.TryParse(value, out long parsed) ? parsed : fallback;
    }
}
