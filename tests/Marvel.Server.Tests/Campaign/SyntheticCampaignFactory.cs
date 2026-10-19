using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Session;
namespace Marvel.Server.Tests;

// This synthetic rules fixture exercises storage and host boundaries, not RoRS content admission.
internal sealed class SyntheticCampaignFactory : ICampaignGameFactory
{
    private readonly DatasetGameFactory core = DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root);
    public CampaignDefinition CampaignDefinition { get; } = new("synthetic",
        [.. Enumerable.Range(0, 5).Select(_ => new CampaignScenario("rhino", "rhino"))], [], [],
        ["o1", "o2", "o3", "o4"], [], []);
    public SessionCompatibility Compatibility => core.Compatibility;
    public OpenedGame Create(GameSpecification specification) => core.Create(specification with { Campaign = null, PlayerDecks = null });
    public CampaignGameProgress InspectCampaign(Game game) => new([], [], null);
    public DeckAssessment AssessCampaignDeck(string hero, IReadOnlyList<string> cards, IReadOnlyList<string> bannedAllies) => new(cards.Count, []);
    public int CampaignBaseHitPoints(string hero) => 10;
}
