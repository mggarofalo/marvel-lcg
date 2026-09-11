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
    private readonly AbilityBook abilities;

    private DatasetGameFactory(
        SetupCatalog setup,
        CardCatalog cards,
        AbilityBook abilities,
        SessionCompatibility compatibility)
    {
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
            AbilityCatalog.Parse(System.Text.Encoding.UTF8.GetString(abilityBytes)),
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
                    cards),
                cards),
            [.. specification.Heroes.Select(hero => setup.Hero(hero).Name)],
            specification.Seed,
            runner,
            setupEvents,
            campaign.Expert);
        return new OpenedGame(Game.Begin(world, cards, runner), setupEvents);
    }

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
