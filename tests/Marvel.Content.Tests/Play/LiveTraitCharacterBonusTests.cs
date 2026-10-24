using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class LiveTraitCharacterBonusTests
{
    [Theory]
    [InlineData("attack")]
    [InlineData("thwart")]
    [Rule("rr:lasting-effects.3")]
    [Rule("rr:lasting-effects.4")]
    [Rule("rr:lasting-effects.5")]
    public void BonusFollowsEffectiveTraitAcrossSeatsEntrantsDepartureAndExpiry(string field)
    {
        // Lasting effects "update whenever the game state updates" and include later entrants.
        var runner = Runner();
        Card? source = null;
        Card? avenger = null;
        Card? foreign = null;
        Card? ordinary = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("01007", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            avenger = Ally(board, "03014", 0);
            foreign = Ally(board, "03013", 1);
            ordinary = Ally(board, "01002", 0);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        long ownBefore = Value(world, avenger!, field);
        long foreignBefore = Value(world, foreign!, field);
        long plainBefore = Value(world, ordinary!, field);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(ownBefore + 1, Value(world, avenger!, field));
        Assert.Equal(foreignBefore + 1, Value(world, foreign!, field));
        Assert.Equal(plainBefore, Value(world, ordinary!, field));
        var later = Ally(world, "03013", 0);
        Assert.Equal(foreignBefore + 1, Value(world, later, field));
        using (world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Traits.Granted + "AVENGER", Affects: ordinary!.ObjectId)))
            Assert.Equal(plainBefore + 1, Value(world, ordinary, field));
        Assert.Equal(plainBefore, Value(world, ordinary, field));
        using (world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Characteristics.Lost + Traits.Granted + "AVENGER", Affects: avenger!.ObjectId)))
            Assert.Equal(ownBefore, Value(world, avenger, field));
        Assert.Equal(ownBefore + 1, Value(world, avenger, field));
        World.MoveToTop(later, world.Seats[0].Hand);
        Assert.Equal(foreignBefore, Value(world, later, field));
        using (world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Traits.Granted + "AVENGER", Affects: source!.ObjectId)))
            Assert.Equal(0, Value(world, source, field));
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Equal(ownBefore, Value(world, avenger, field));
        Assert.Equal(foreignBefore, Value(world, foreign!, field));
    }

    [Theory]
    [InlineData("health")]
    [InlineData("trait:AVENGER")]
    [InlineData("thwart")]
    public void UnsupportedLiveGroupFieldsFailClosed(string field) =>
        Assert.Throws<AbilityException>(() => Runner(field));

    private static Card Ally(World world, string face, int player) =>
        world.CreateCard(face, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));

    private static long Value(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, world.Facts, world.Players);

    private static AbilityRunner Runner(string fields = "attack") => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"01007","abilities":[{
          "trigger":{"timing":"Action","event":"WhenActionTriggered","subject":"this"},
          "effect":{"grantCharactersWithTrait":{"trait":"AVENGER","fields":["FIELD","thwart"],"amount":1,"until":"EndOfPhase"}}
        }]}]}
        """.Replace("FIELD", fields, StringComparison.Ordinal)));
}
