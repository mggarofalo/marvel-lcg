using System.Collections.Immutable;
using Marvel.Protocol;
using Marvel.Rules.Setup;
using System.Security.Cryptography;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Rules.Campaign;

namespace Marvel.Server;

/// <summary>Loads the repository's canonical datasets and deals games from them.</summary>
public sealed class DatasetGameFactory : ICampaignGameFactory, ISetupDiscovery, IDeckDiscovery, IContentUpgradeFactory
{
    private readonly SetupCatalog setup;
    private readonly CardCatalog cards;
    private readonly AbilityProgram abilities;
    private readonly ImmutableHashSet<string> playable;
    private ContentUpgradeAdmission? upgrades;
    private IReadOnlyDictionary<string, Dictionary<string, string>> search = new Dictionary<string, Dictionary<string, string>>();

    private DatasetGameFactory(
        SetupCatalog setup,
        CardCatalog cards,
        AbilityProgram abilities,
        SessionCompatibility compatibility)
    {
        CampaignDefinition = RiseOfRedSkullAdmission.Discover(setup, cards, abilities.Authored);
        playable = abilities.Authored.Where(face => CampaignDefinition is not null || cards.Pack(face) != "trors")
            .ToImmutableHashSet(StringComparer.Ordinal);
        if (CampaignDefinition is null) setup = setup.WithoutPack(cards, "trors");
        SetupCatalogAdmission.Validate(setup, cards, playable);
        this.setup = setup;
        this.cards = cards;
        this.abilities = abilities;
        Compatibility = compatibility;
    }

    /// <inheritdoc />
    public SessionCompatibility Compatibility { get; }

    /// <inheritdoc />
    public SessionContentUpgrade? ContentUpgrade(SessionCompatibility source) => upgrades?.For(source);

    /// <inheritdoc />
    public CampaignDefinition? CampaignDefinition { get; }

    /// <inheritdoc />
    public DeckAssessment AssessCampaignDeck(string hero, IReadOnlyList<string> cards, IReadOnlyList<string> bannedAllies) =>
        CampaignDecks.Assess(cards, setup.Hero(hero), this.cards, playable, bannedAllies);

    /// <summary>Offers campaign deck quantities through the same construction rules as creation.</summary>
    public IReadOnlyDictionary<string, int[]> CampaignDeckQuantities(
        string hero, IReadOnlyList<string> draft, IReadOnlyList<string> bannedAllies) =>
        CampaignDecks.Quantities(draft, setup.Hero(hero), cards, playable, bannedAllies);

    /// <inheritdoc />
    public int CampaignBaseHitPoints(string hero) => checked((int)cards.PrintedValue(
        setup.Hero(hero).Hero.Single().Split(',')[0], "HP", 1));

    /// <inheritdoc />
    public CampaignGameProgress InspectCampaign(Game game) => CampaignInspection.Read(game,
        CampaignDefinition ?? throw new InvalidOperationException("the complete campaign is not admitted"));

    /// <summary>Loads the three datasets beneath <paramref name="dataRoot"/>.</summary>
    public static DatasetGameFactory Load(string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        string root = Path.GetFullPath(dataRoot);
        byte[] setupBytes = ReadBytes(root, "setup", "setup.json");
        byte[] cardBytes = ReadBytes(root, "cards", "cards.json");
        byte[] abilityBytes = ReadBytes(root, "abilities", "abilities.json");
        var factory = new DatasetGameFactory(
            SetupCatalog.Parse(System.Text.Encoding.UTF8.GetString(setupBytes)),
            CardCatalog.Parse(System.Text.Encoding.UTF8.GetString(cardBytes)),
            AbilityLowering.Book(AbilityCatalog.Parse(System.Text.Encoding.UTF8.GetString(abilityBytes))),
            new SessionCompatibility(
                EngineBuildIdentity.ProductVersion,
                EngineBuildIdentity.ReplayContract,
                EngineBuildIdentity.RngContract,
                EngineBuildIdentity.StateDigest,
                Hash(cardBytes),
                Hash(setupBytes),
                Hash(abilityBytes)))
        {
            search = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
                ReadBytes(root, "cards", "search.json"))!,
        };
        factory.upgrades = new ContentUpgradeAdmission(factory.Compatibility, cardBytes, setupBytes, abilityBytes);
        return factory;
    }

    /// <inheritdoc />
    public OpenedGame Create(GameSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        PlayerCountRules.Validate(specification.Heroes.Count);
        CampaignSetupAdmission.Validate(specification, CampaignDefinition, cards, playable);
        if (specification.PlayerDecks is not null)
        {
            if (specification.PlayerDecks.Count != specification.Heroes.Count)
                throw new ArgumentException("an explicit deck is required for every seat", nameof(specification));
            for (int seat = 0; seat < specification.Heroes.Count; seat++)
            {
                var assessment = specification.Campaign is { } campaignSetup
                    ? AssessCampaignDeck(specification.Heroes[seat], specification.PlayerDecks[seat].Cards, campaignSetup.BannedAllyFaces)
                    : AssessDeck(specification.Heroes[seat], specification.PlayerDecks[seat].Cards);
                if (!assessment.IsValid)
                    throw new ArgumentException(string.Join("; ", assessment.Diagnostics.Select(item => item.Message)), nameof(specification));
            }
        }
        var runner = new AbilityRunner(abilities);
        var setupEvents = new List<GameEvent>();
        var campaign = setup.Campaign(specification.Scenario);
        var ordinary = Blueprints.From(Dealer.Deal(setup,
            new DealRequest(specification.Scenario, specification.Heroes, specification.ModularSets,
                PlayerDecks: specification.PlayerDecks)
            { CampaignExcludedAllies = specification.Campaign?.BannedAllyFaces ?? [] }, cards), cards);
        var plan = specification.Campaign is { } input
            ? CampaignBlueprints.From(ordinary, input, CampaignDefinition!) : new CampaignDealPlan(ordinary, [], []);
        var world = WorldSetup.DealConfigured(
            cards, plan.Blueprints,
            [.. specification.Heroes.Select(hero => setup.Hero(hero).Name)],
            specification.Seed,
            runner,
            new WorldSetupOptions(campaign.Expert, specification.Campaign, plan.Bindings, plan.ExperimentalWeapons),
            setupEvents);
        return new OpenedGame(Game.Begin(world, cards, runner), setupEvents);
    }

    /// <summary>Previews a draft against the same content and construction checks used at game creation.</summary>
    public DeckAssessment AssessDeck(string hero, IReadOnlyList<string> draft) =>
        CoreDeckAssessment.Assess(draft, setup.Hero(hero), cards, playable);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, int[]> DeckQuantities(string hero, IReadOnlyList<string> draft) =>
        CoreDeckQuantities.Assess(draft, setup.Hero(hero), cards, playable);

    /// <inheritdoc />
    public DeckCatalog DiscoverDecks() => new(
        setup.HeroNames.Select(key =>
        {
            var hero = setup.Hero(key);
            return new DeckHero(key, hero.Name, hero.Hero.Single().Split(','), hero.HeroDeck,
                [.. hero.HeroDeck, .. hero.PlayerDeck])
            {
                Previews = hero.Hero.Single().Split(',').Select(face => Marvel.View.CatalogCardProjection.From(cards, face)).ToArray(),
            };
        }).ToArray(),
        playable.Order(StringComparer.Ordinal).Where(face => cards.Kind(face) is
            CardKind.Ally or CardKind.Event or CardKind.Resource or CardKind.Support or CardKind.Upgrade)
            .Select(PresentCard).ToArray());

    /// <inheritdoc />
    public DeckCardPage BrowseCards(int page)
    {
        const int pageSize = 200;
        if (page is < 0 or > 1000) throw new ArgumentOutOfRangeException(nameof(page));
        var faces = cards.Faces;
        int offset = page * pageSize;
        return new(faces.Skip(offset).Take(pageSize).Select(PresentCard).ToArray(),
            offset + pageSize < faces.Count ? page + 1 : null);
    }

    private DeckCard PresentCard(string face) => new(face, cards.Title(face), cards.Kind(face).ToString(),
        cards.Attributes(face).GetValueOrDefault("Class", ""), cards.PrintedTraits(face), cards.Text(face),
        cards.Attributes(face).GetValueOrDefault("Cost"))
    {
        SearchFields = (search.GetValueOrDefault(face) ?? new Dictionary<string, string>()).ToImmutableDictionary(StringComparer.Ordinal),
        RuntimeSupported = playable.Contains(face) && cards.Kind(face) is
            CardKind.Ally or CardKind.Event or CardKind.Resource or CardKind.Support or CardKind.Upgrade,
        Subtitle = cards.Subtitle(face), Preview = Marvel.View.CatalogCardProjection.From(cards, face),
    };

    /// <inheritdoc />
    public SetupChoices DiscoverSetup() => new(
        Heroes:
        [
            .. setup.HeroNames.Select(key =>
                new HeroSetupChoice(key, setup.Hero(key).Name)),
        ],
        Scenarios:
        [
            .. setup.CampaignNames.Select(key =>
            {
                CampaignSetup campaign = setup.Campaign(key);
                return ScenarioSetupProjection.Describe(key, campaign, cards);
            }),
        ],
        ModularSets:
        [
            .. setup.EncounterSetNames
                .Where(key => ModularEncounterSets.IsModular(setup, cards, key))
                .Select(key => new ModularSetupChoice(
                    key, setup.EncounterSetDisplayName(key))),
        ],
        Runtime: new RuntimeIdentity(
            EngineBuildIdentity.ProductVersion,
            EngineBuildIdentity.Commit,
            Compatibility.ReplayContract,
            Compatibility.RngContract,
            Compatibility.StateDigest,
            EngineProtocol.Version,
            SessionSave.CurrentSchema,
            Compatibility.CardsSha256,
            Compatibility.SetupSha256,
            Compatibility.AbilitiesSha256));

    private static byte[] ReadBytes(string root, string dataset, string file) =>
        File.ReadAllBytes(Path.Combine(root, "datasets", dataset, file));

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}

