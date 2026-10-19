using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RedHouseTests
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    [Rule("rr:interrupt.1")]
    public void HeroMayChooseTheStatAfterPayingForTheThwart(bool accept, int removed)
    {
        // Printed 04139: "they may use their ATK instead of their THW."
        var runner = RedHouse();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var scheme = Scheme(world);
        BasicThwartPowers.BasicThwart(world, world.Facts, 0, scheme, []);
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Answer(world, runner, accept);
        Assert.Equal(10 - removed, scheme.Tokens["k_threat"]);
        world.Seats[0].IdentityCard.Refresh();
        BasicThwartPowers.BasicThwart(world, world.Facts, 0, scheme, []);
        Answer(world, runner, false);
        Assert.Equal(9 - removed, scheme.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [Rule("rr:consequential-damage.1")]
    public void BlackCatUsesTheChosenStatsConsequentialIcons(bool accept, int damage)
    {
        // Ruling 77b5611365165488: Black Cat using ATK for Red House takes no damage.
        var runner = RedHouse();
        var (_, world) = Playing(_ => { }, abilities: runner);
        var scheme = Scheme(world);
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        AllyBasicPowers.AllyPower(world, world.Facts, ally, scheme, BasicPowers.ThwartVerb, []);
        Answer(world, runner, accept);
        Assert.Equal(damage, ally.Damage);
        Assert.Equal(9, scheme.Tokens["k_threat"]);
    }

    [Fact]
    [Rule("rr:basic-power.1")]
    public void DashThwartCannotInitiateToReachTheInterrupt()
    {
        // Rulings 3307f61b7b6d0690 and 812281818ff0af16: Hulk cannot initiate.
        var runner = RedHouse();
        var (_, world) = Playing(_ => { }, abilities: runner);
        var scheme = Scheme(world);
        var hulk = world.CreateCard("01050", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Assert.Throws<RulesNotImplementedException>(() =>
            AllyBasicPowers.AllyPower(world, world.Facts, hulk, scheme, BasicPowers.ThwartVerb, []));
        Assert.True(hulk.Ready);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:confuse-confused.1")]
    public void ConfusionCancelsBeforeTheStatChoice()
    {
        var runner = RedHouse();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var scheme = Scheme(world);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        BasicThwartPowers.BasicThwart(world, world.Facts, 0, scheme, []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(10, scheme.Tokens["k_threat"]);
        Assert.False(world.Seats[0].IdentityCard.Ready);
    }

    [Fact]
    [Rule("rr:interrupt.1")]
    public void OtherSchemesDoNotOfferRedHousesInterrupt()
    {
        var runner = RedHouse();
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        Scheme(world);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 10);
        BasicThwartPowers.BasicThwart(world, world.Facts, 0, main, []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(9, main.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 2)]
    [InlineData(true, true, 4)]
    [Rule("rr:interrupt.1")]
    public void CardThwartsSubstituteOnlyTheActorsThwExpression(bool accept, bool fixedAmount, int removed)
    {
        // Synthetic card ability probes the generic card-power contract; Red House is printed 04139.
        string amount = fixedAmount ? "4" : "{\"modified\":{\"card\":\"you\",\"field\":\"thwart\"}}";
        var runner = new AbilityRunner(AbilityCatalog.Parse(RedHouseJson.Replace("EVENT", """
            ,{"card":"01007","name":"Synthetic THW action","abilities":[{
              "trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"this"},
              "effect":{"seq":[{"chooseCard":{"from":{"query":"schemes"},"effect":{
                "thwart":{"target":"chosen","effect":{"removeThreat":{"scheme":"chosen","amount":AMOUNT}}}
              }}},{"placeCounters":{"card":"this","counter":"energy",
                "count":{"modified":{"card":"you","field":"thwart"}}}}]}
            }]}
            """.Replace("AMOUNT", amount))));
        Card? eventCard = null;
        Card? scheme = null;
        var (game, world) = Playing(board =>
        {
            scheme = Scheme(board);
            eventCard = board.CreateCard("01007", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == eventCard!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var target = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == scheme!.ObjectId);
        game.Resolve(Decision.Take(target.Id));
        var interrupt = Assert.Single(game.Pending!.Affordances);
        game.Resolve(accept ? Decision.Take(interrupt.Id) : Decision.Decline);
        Assert.Equal(10 - removed, scheme!.Tokens["k_threat"]);
        Assert.Equal(1, eventCard!.Tokens["c_energy"]);
    }

    [Fact]
    [Rule("rr:you-your.7")]
    public void TheThwartingPlayersChoiceIsNotOfferedToTheFirstPlayer()
    {
        var runner = RedHouse();
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var scheme = Scheme(world);
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        AllyBasicPowers.AllyPower(world, world.Facts, ally, scheme, BasicPowers.ThwartVerb, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(1, prompt.Player);
        Assert.Contains("ATK", Assert.Single(prompt.Affordances).Description);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances[0].Id), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(0, ally.Damage);
    }

    private static Card Scheme(World world)
    {
        var card = world.CreateCard("04139", world.AreaOf(DeckType.SideSchemesArea));
        card.PlaceTokens("k_threat", 10);
        return card;
    }

    private static AbilityRunner RedHouse() => new(AbilityCatalog.Parse(RedHouseJson.Replace("EVENT", "")));

    private const string RedHouseJson = """
        { "cards": [{ "card":"04139", "name":"The Red House", "abilities":[{
          "trigger":{"timing":"Interrupt","event":"WhenCharacterThwarts","subject":"this","player":"trigger.player"},
          "effect":{"useAttackForThwart":"trigger"}
        }]}EVENT]}
        """;

    private static void Answer(World world, AbilityRunner runner, bool accept)
    {
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.NotNull(prompt);
        Assert.True(prompt.Cancellable);
        var offer = Assert.Single(prompt.Affordances);
        Sequence.Answer(world, world.Facts, runner, prompt,
            accept ? Decision.Take(offer.Id) : Decision.Decline, []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
    }
}
