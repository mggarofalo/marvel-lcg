using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttachedDefeatProjectionReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData("ForcedInterrupt", "", true, false)]
    [InlineData("ForcedInterrupt", "\"when\":{\"inForm\":{\"player\":\"you\",\"form\":\"hero\"}},", true, true)]
    [InlineData("ForcedInterrupt", "\"cost\":{\"exhaust\":\"this\"},", true, true)]
    [InlineData("Interrupt", "", true, true)]
    [InlineData("ForcedInterrupt", "", false, true)]
    public void OpaqueObserverAdmissionPreservesItsNarrowBoundary(
        string timing, string extra, bool attached, bool refused)
    {
        // Synthetic variants distinguish the engine's chosen conservative
        // projection boundary; they do not change Spider-Tracer's printed text.
        var local = AbilityCatalog.Parse($$$$"""
            {"cards":[
              {"card":"01006","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
                "cost":{"exhaust":"this"},
                "effect":{"attack":{"target":{"titled":"Goblin Thrall"},"effect":{"seq":[
                  {"dealAttackDamage":{"cards":{"titled":"Goblin Thrall"},"amount":100}},
                  {"draw":{"player":"you","count":1}}
                ]}}}
              }]},
              {"card":"01007","abilities":[{
                "trigger":{"event":"WhenCardDefeated","timing":"{{{{timing}}}}","subject":"game"},
                {{{{extra}}}}
                "effect":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}
              }]}
            ]}
            """);
        var runner = new AbilityRunner(AuthoredCards.Book with
        {
            Abilities = [.. AuthoredCards.Book.Abilities.Where(row => row.Card is not ("01006" or "01007")), .. local.Abilities],
        });
        Card? source = null;
        Card? minion = null;
        Card? observer = null;
        void Prepare(World world)
        {
            source = InPlay(world, "01006");
            minion = world.CreateCard("02024", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            observer = world.CreateCard("01007", attached
                ? world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId, 0)
                : world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 4);
        }
        if (refused)
        {
            var failure = Assert.Throws<RulesNotImplementedException>(() => Playing(Prepare, hero: true, abilities: runner));
            Assert.Contains("defeat-triggered ability", failure.Message);
        }
        else
        {
            var (game, _) = Playing(Prepare, hero: true, abilities: runner);
            Assert.Contains(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        }
        Assert.True(source!.Ready);
        Assert.True(observer!.Ready);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
    }
}
