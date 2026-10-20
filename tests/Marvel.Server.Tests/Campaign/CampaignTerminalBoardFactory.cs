using Marvel.Protocol;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Session;
namespace Marvel.Server.Tests;

/// <summary>Arranges canonical terminal boards reproducibly; not a full-play or reachability claim.</summary>
internal sealed class CampaignTerminalBoardFactory(DatasetGameFactory actual) : ICampaignGameFactory
{
    public CampaignDefinition? CampaignDefinition => actual.CampaignDefinition;
    public SessionCompatibility Compatibility => actual.Compatibility;
    internal string Failure { get; private set; } = "";
    public OpenedGame Create(GameSpecification specification)
    {
        try { return Prepare(specification); }
        catch (Exception error) { Failure = error.ToString(); throw; }
    }
    private OpenedGame Prepare(GameSpecification specification)
    {
        var opened = actual.Create(specification);
        var setupEvents = opened.SetupEvents.ToList();
        int setupDecisions = 0;
        while (opened.Game.Phase == GamePhase.ScenarioSetup)
        {
            if (++setupDecisions > 20) throw new InvalidOperationException("Setup did not complete");
            setupEvents.AddRange(opened.Game.Resolve(CampaignSetupAnswers.Next(opened.Game.Pending!)).Events);
        }
        var world = opened.Game.State;
        // Test preparation occurs identically on every replay. The final attack,
        // victory handling, inspection, rewards and durable host commit remain real.
        var active = world.TheCardIn(DeckType.VillainArea)!;
        var final = world.AreaOf(DeckType.VillainDeck).Cards.Single();
        var leaving = world.Cards.Where(card => card == active || world.Facts.Kind(card.FaceId) == CardKind.Minion
            && DeckTypes.IsInPlay(card.Area.Type)).ToArray();
        var hosts = leaving.Select(card => card.ObjectId).ToHashSet();
        foreach (var attachment in world.Cards.Where(card => hosts.Contains(card.Area.Host)
            && DeckTypes.IsInPlay(card.Area.Type)).ToArray())
            World.MoveToTop(attachment, world.AreaOf(DeckType.EncounterDiscardPile));
        foreach (var card in leaving) World.MoveToTop(card, world.AreaOf(DeckType.EncounterDiscardPile));
        // Red House prevents all villain damage; the arranged terminal board has already cleared it.
        foreach (var scheme in world.Cards.Where(card => card.FaceId == "04139"
            && DeckTypes.IsInPlay(card.Area.Type)).ToArray())
            World.MoveToTop(scheme, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(final, world.AreaOf(DeckType.VillainArea));
        final.TakeDamage(DamagePlacement.Health(world, world.Facts, final) - 1);
        return opened with { SetupEvents = setupEvents };
    }
    public CampaignGameProgress InspectCampaign(Game game) => actual.InspectCampaign(game);
    public DeckAssessment AssessCampaignDeck(string hero, IReadOnlyList<string> cards, IReadOnlyList<string> bannedAllies) =>
        actual.AssessCampaignDeck(hero, cards, bannedAllies);
    public IReadOnlyDictionary<string, int[]> CampaignDeckQuantities(string hero, IReadOnlyList<string> draft, IReadOnlyList<string> bannedAllies) =>
        actual.CampaignDeckQuantities(hero, draft, bannedAllies);
    public int CampaignBaseHitPoints(string hero) => actual.CampaignBaseHitPoints(hero);
}
