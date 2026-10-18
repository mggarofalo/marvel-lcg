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

namespace Marvel.Server;

/// <summary>Loads the repository's canonical datasets and deals games from them.</summary>
public sealed class DatasetGameFactory : IDurableGameFactory, ISetupDiscovery
{
    private readonly SetupCatalog setup;
    private readonly CardCatalog cards;
    private readonly AbilityProgram abilities;

    private DatasetGameFactory(
        SetupCatalog setup,
        CardCatalog cards,
        AbilityProgram abilities,
        SessionCompatibility compatibility)
    {
        SetupCatalogAdmission.Validate(setup, cards, abilities.Authored);
        this.setup = setup;
        this.cards = cards;
        this.abilities = abilities;
        Compatibility = compatibility;
    }

    /// <inheritdoc />
    public SessionCompatibility Compatibility { get; }

    /// <summary>Loads the three datasets beneath <paramref name="dataRoot"/>.</summary>
    public static DatasetGameFactory Load(string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        string root = Path.GetFullPath(dataRoot);
        byte[] setupBytes = ReadBytes(root, "setup", "setup.json");
        byte[] cardBytes = ReadBytes(root, "cards", "cards.json");
        byte[] abilityBytes = ReadBytes(root, "abilities", "abilities.json");
        return new DatasetGameFactory(
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
                Hash(abilityBytes)));
    }

    /// <inheritdoc />
    public OpenedGame Create(GameSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        PlayerCountRules.Validate(specification.Heroes.Count);
        if (specification.PlayerDecks is not null)
        {
            if (specification.PlayerDecks.Count != specification.Heroes.Count)
                throw new ArgumentException("an explicit deck is required for every seat", nameof(specification));
            for (int seat = 0; seat < specification.Heroes.Count; seat++)
            {
                var assessment = AssessDeck(specification.Heroes[seat], specification.PlayerDecks[seat].Cards);
                if (!assessment.IsValid)
                    throw new ArgumentException(string.Join("; ", assessment.Diagnostics.Select(item => item.Message)), nameof(specification));
            }
        }
        var runner = new AbilityRunner(abilities);
        var setupEvents = new List<GameEvent>();
        var campaign = setup.Campaign(specification.Scenario);
        var world = WorldSetup.Deal(
            cards,
            Blueprints.From(
                Dealer.DealOrder(
                    setup,
                    specification.Scenario,
                    specification.Heroes,
                    specification.ModularSets,
                    cards, playerDecks: specification.PlayerDecks),
                cards),
            [.. specification.Heroes.Select(hero => setup.Hero(hero).Name)],
            specification.Seed,
            runner,
            setupEvents,
            campaign.Expert);
        return new OpenedGame(Game.Begin(world, cards, runner), setupEvents);
    }

    /// <summary>Previews a draft against the same content and construction checks used at game creation.</summary>
    public DeckAssessment AssessDeck(string hero, IReadOnlyList<string> draft) =>
        CoreDeckAssessment.Assess(draft, setup.Hero(hero), cards, abilities.Authored);

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
                return new ScenarioSetupChoice(
                    key, campaign.Name, campaign.Expert, [.. campaign.ModularSets]);
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
