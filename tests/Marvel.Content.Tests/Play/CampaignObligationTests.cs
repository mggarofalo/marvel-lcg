using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Prompts;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CampaignObligationTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [Rule("rr:player-phase")]
    public void MedicalEmergencyResolvesOnlyAtItsOwnersTurnEnd(int owner, bool hero)
    {
        // During the player phase each player takes one turn; 04164 says "at the end of your turn".
        var (game, world) = Playing(board =>
        {
            if (hero) board.Seats[owner].IdentityCard.TurnTo(owner == 0 ? "01001a" : "01010a");
            board.CreateCard("04164", board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(owner)));
        }, heroes: ["spider_man", "captain_marvel"]);
        game.Resolve(Decision.Decline);
        Assert.Equal(1, game.Active);
        Assert.Equal(owner == 0 && hero ? 1 : 0, world.Seats[owner].IdentityCard.Damage);
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(hero ? 1 : 0, world.Seats[owner].IdentityCard.Damage);
    }

    [Fact]
    public void AChoiceAtTurnEndResumesBeforeAdvancingTheActivePlayer()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"04164","abilities":[{
              "trigger":{"timing":"ForcedResponse","event":"WhenPlayerTurnEnds","subject":"you"},
              "effect":{"choose":{"options":[{"draw":{"player":"you","count":1}},{"seq":[]}]}}
            }]}]}
            """));
        var (game, world) = Playing(board => board.CreateCard("04164",
            board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        int before = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Decline);
        Assert.Equal(0, game.Active);
        Assert.Equal(Question.Option, game.Pending!.Asking);
        game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(1, game.Active);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(before + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void MartialLawAndPropagandaApplyOnlyWhileTheirPhysicalObligationsRemain()
    {
        var (game, world) = Playing(_ => { }, hero: true);
        var hero = world.Seats[0].IdentityCard;
        long hand = StateFields.Modified(world, hero, "hand_size", world.Facts, 1);
        long attack = StateFields.Modified(world, hero, "attack", world.Facts, 1);
        var law = world.CreateCard("04165", world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
        var propaganda = world.CreateCard("04166", law.Area);
        Assert.Equal(hand - 1, StateFields.Modified(world, hero, "hand_size", world.Facts, 1));
        Assert.Equal(attack - 1, StateFields.Modified(world, hero, "attack", world.Facts, 1));
        Discard.Card(world, law, "test", []);
        Discard.Card(world, propaganda, "test", []);
        Assert.Equal(hand, StateFields.Modified(world, hero, "hand_size", world.Facts, 1));
        Assert.Equal(attack, StateFields.Modified(world, hero, "attack", world.Facts, 1));
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
    }
}
