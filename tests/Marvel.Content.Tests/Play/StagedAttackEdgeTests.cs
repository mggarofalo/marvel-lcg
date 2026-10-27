using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic Melee body tests engine timing without expansion admission.
public sealed class StagedAttackEdgeTests
{
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:guard.1")]
    [Fact]
    public void FirstDefeatChoiceSettlesBeforeGuardRemovalAndSecondEnemyOffer()
    {
        // "resolves before the defeated card is placed in its owner's discard pile";
        // Guard: "The engaged player cannot attack any villain."
        var root = JsonNode.Parse("""
            {"cards":[{"card":"01101","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect":{"choose":{"options":[
                {"placeCounters":{"card":"defeater","counter":"settled","count":1}},
                {"placeCounters":{"card":"defeater","counter":"settled","count":2}}
              ]}}
            }]}]}
            """)!;
        root["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"05030","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
              "effect":BODY}]}
            """.Replace("BODY", StagedAttackSafetyTests.Body, StringComparison.Ordinal)));
        Card? source = null;
        Card? guard = null;
        var (game, world) = Playing(board =>
        {
            source = StagedAttackSafetyTests.Payable(board);
            guard = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString())));
        StagedAttackSafetyTests.Pay(game, world, source!);
        game.Resolve(Decision.Take(guard!.ObjectId));
        Assert.Equal(DeckType.EngagedEnemiesArea, guard.Area.Type);
        Assert.NotEqual("Choose another enemy", game.Pending!.DisplayQuestion);
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_settled"));
        game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_settled"]);
        Assert.Equal(DeckType.EncounterDiscardPile, guard.Area.Type);
        Assert.Equal("Choose another enemy", game.Pending!.DisplayQuestion);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal(villain.ObjectId, Assert.Single(game.Pending.Affordances).AnchorId);
        game.Resolve(Decision.Take(villain.ObjectId));
        Assert.Equal(3, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:guard.1")]
    [Fact]
    public void SurvivingGuardStillBlocksVillainAtSecondChoice()
    {
        // "The engaged player cannot attack any villain."
        Card? source = null;
        Card? guard = null;
        Card? other = null;
        var (game, world) = Playing(board =>
        {
            source = StagedAttackSafetyTests.Payable(board);
            var area = board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
            guard = board.CreateCard("01101", area);
            Statuses.Give(board, guard, Statuses.Tough);
            other = board.CreateCard("01130", area);
        }, hero: true, abilities: StagedAttackSafetyTests.Runner());
        StagedAttackSafetyTests.Pay(game, world, source!);
        game.Resolve(Decision.Take(guard!.ObjectId));
        Assert.Equal(0, guard.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, guard.Area.Type);
        Assert.False(Statuses.Has(world, guard, Statuses.Tough));
        Assert.Equal(other!.ObjectId, Assert.Single(game.Pending!.Affordances).AnchorId);
        game.Resolve(Decision.Take(other.ObjectId));
        Assert.Equal(3, other.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:ability.4")]
    [Fact]
    public void LoneEnemyReceivesOnlyFirstSentenceAndEventCompletes()
    {
        // "resolve the ability one sentence at a time"; no other enemy exists.
        Card? source = null;
        var (game, world) = Playing(board => source = StagedAttackSafetyTests.Payable(board),
            hero: true, abilities: StagedAttackSafetyTests.Runner());
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        StagedAttackSafetyTests.Pay(game, world, source!);
        game.Resolve(Decision.Take(villain.ObjectId));
        Assert.Equal(3, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:stun-stunned.5")]
    [Rule("rr:stun-stunned.7")]
    [Fact]
    public void StunnedActorPaysThenCancelsBothSentencesWithoutAttackEndResponse()
    {
        // "Costs associated with the attack attempt ... must still be paid";
        // "that character is not considered to have attacked."
        var runner = ObserverRunner();
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(board =>
        {
            source = StagedAttackSafetyTests.Payable(board);
            minion = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
            InPlay(board, "01092");
        }, hero: true, abilities: runner);
        StagedAttackSafetyTests.Pay(game, world, source!);
        Assert.True(game.IsRootPrompt);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.DoesNotContain(world.Seats[0].Hand.Cards, card => card.FaceId is "01088" or "01089");
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_whole"));
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:retaliate-x.1")]
    [Rule("rr:tough.1")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToughOnEitherRecipientPreventsItsDamageAndBothRetaliateAfterSecondChoice(bool toughFirst)
    {
        // Retaliate: "After this character is attacked, deal X damage to the attacker."
        // Tough: remove tough "instead" of taking damage.
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(board =>
        {
            source = StagedAttackSafetyTests.Payable(board);
            minion = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            var villain = board.TheCardIn(DeckType.VillainArea)!;
            Statuses.Give(board, toughFirst ? villain : minion, Statuses.Tough);
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "retaliate", 1,
                Affects: villain.ObjectId));
            board.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "retaliate", 1,
                Affects: minion.ObjectId));
        }, hero: true, abilities: StagedAttackSafetyTests.Runner());
        var first = world.TheCardIn(DeckType.VillainArea)!;
        var hero = world.Seats[0].IdentityCard;
        long before = hero.Damage;
        StagedAttackSafetyTests.Pay(game, world, source!);
        game.Resolve(Decision.Take(first.ObjectId));
        Assert.Equal(before, hero.Damage);
        Assert.Equal("Choose another enemy", game.Pending!.DisplayQuestion);
        game.Resolve(Decision.Take(minion!.ObjectId));
        while (game.Pending!.Asking == Question.Order)
            game.Resolve(Decision.Take(game.Pending.Affordances[0].Id));
        Assert.Equal(toughFirst ? 0 : 3, first.Damage);
        Assert.Equal(toughFirst ? 3 : 0, minion.Damage);
        Assert.Equal(before + 2, hero.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void BothEnemyChoicesBelongToTheNonzeroActingSeat()
    {
        Card? source = null;
        Card? first = null;
        Card? second = null;
        Card[] payments = [];
        var (game, world) = Playing(board =>
        {
            board.FirstPlayer = 1;
            board.Seats[1].IdentityCard.TurnTo("01010a");
            source = board.CreateCard("05030", board.Seats[1].Hand);
            payments = [board.CreateCard("01088", board.Seats[1].Hand),
                board.CreateCard("01089", board.Seats[1].Hand)];
            var area = board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1));
            first = board.CreateCard("01130", area);
            second = board.CreateCard("01130", area);
        }, heroes: ["spider_man", "captain_marvel"], abilities: StagedAttackSafetyTests.Runner());
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId), [.. payments.Select(card => card.ObjectId)]));
        Assert.Equal(1, game.Pending!.Player);
        game.Resolve(Decision.Take(first!.ObjectId));
        Assert.Equal(1, game.Pending!.Player);
        Assert.Equal("Choose another enemy", game.Pending.DisplayQuestion);
        Assert.Contains(source!.ObjectId, game.Pending.ContextCardIds!);
        game.Resolve(Decision.Take(second!.ObjectId));
        Assert.Equal(3, first.Damage);
        Assert.Equal(3, second.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(PlayArea.Of(1), source.Area.PlayArea);
        Assert.False(world.Agenda.IsBusy);
    }

    private static AbilityRunner ObserverRunner() => new(AbilityCatalog.Parse("""
        {"cards":[{"card":"05030","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":BODY}]},{"card":"01092","abilities":[{
          "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","subject":"game"},
          "effect":{"placeCounters":{"card":"you","counter":"whole","count":1}}
        }]}]}
        """.Replace("BODY", StagedAttackSafetyTests.Body, StringComparison.Ordinal)));
}
