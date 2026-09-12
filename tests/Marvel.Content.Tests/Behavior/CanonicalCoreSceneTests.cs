using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;
public abstract class CanonicalCoreSceneTestBase
{
    protected const string PlayerDeckAuthority = "behavior:rr:player-deck.2:published-result";
    protected static readonly SetupCatalog Setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    protected static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    protected static CanonicalCoreScene OneCardDeck() => Deal(PlayerDeckAuthority, "rhino", ["spider_man"]).Apply(new StackPlayerDeck(Seat: 0, TopFirst: [new SceneCard("01006")], PlayerDeckRemainder.Discard));
    protected static CanonicalCoreScene Deal(string authority, string campaign, IReadOnlyList<string> heroes, IReadOnlyList<string>? modularSets = null) => CanonicalCoreScene.Deal(new CoreSceneRequest(authority, campaign, heroes, Seed: 302, ModularSets: modularSets), Setup, Cards, AuthoredCards.Runner());
}
