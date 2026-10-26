using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic selection and composition contracts; no Lightning Strike row is admitted.</summary>
public sealed class LightningRecipientSelectionTests : CoreCardTestBase
{
    private static readonly AbilityCardSelection Selection =
        new AbilityCardSelection.Query(AbilityCardQuery.VillainAndMinionsEngagedWithYou);

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(4, 3)]
    public void QueryNamesOnlyCurrentVillainAndResolvingSeatsEffectiveMinions(int players, int player)
    {
        var world = Hero("01001a", players);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var own = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player)));
        var changed = world.CreateCard("01101", own.Area);
        changed.AssignProfile(new("synthetic-ally", "Synthetic ally", CardKind.Ally, [], ImmutableDictionary<string, long>.Empty));
        foreach (int other in Enumerable.Range(0, players).Where(seat => seat != player))
            world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(other)));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDiscardPile));
        var context = Context(world, player);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        for (int repeat = 0; repeat < 3; repeat++)
        {
            Assert.Equal([villain, own], context.Selectors().Every(Selection));
            Assert.Equal([villain, own], AbilityProjectedStateQueries.ProjectedEvery(Selection, new(context), context));
        }
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
        World.MoveToTop(own, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.Equal([villain], context.Selectors().Every(Selection));
        World.MoveToTop(villain, world.AreaOf(DeckType.VillainDeck));
        Assert.Empty(context.Selectors().Every(Selection));
    }

    [Fact]
    public void ProjectedEngagementAndVillainPromotionReplaceMembership()
    {
        var world = Hero("01001a", 2);
        var first = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var next = world.CreateCard("01095", world.AreaOf(DeckType.VillainDeck));
        var leaving = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var arriving = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var context = Context(world, 1);
        var state = new AreaProjectionState(context) { ActiveVillain = next.ObjectId };
        state.Departed.Add(first.ObjectId);
        state.Entered.Add(next.ObjectId);
        state.EngagedWith[leaving.ObjectId] = 0;
        state.EngagedWith[arriving.ObjectId] = 1;
        Assert.Equal([next, arriving], AbilityProjectedStateQueries.ProjectedEvery(Selection, state, context));
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var trace = new AbilitySelectorTraceContext(next.ObjectId, scope, [first.ObjectId], [], [], state.EngagedWith);
        Assert.Throws<RulesNotImplementedException>(() => AbilitySelectorTrace.MatchingCandidates(Selection, trace));
        Assert.Equal([next, arriving], AbilitySelectorTrace.MatchingCandidates(new AbilityCardSelection.InObjectIdOrder(Selection), trace));
        Assert.Equal([first, leaving], context.Selectors().Every(Selection));
    }

    [Fact]
    public void QueriedExecutionMatchesAnUnprobedControl()
    {
        var probed = ExecutionFixture();
        var control = ExecutionFixture();
        var context = Context(probed.World, 0);
        string digest = probed.World.Digest().Canonical();
        var random = probed.World.Random.Generator.GetState();
        long words = probed.World.Random.Generator.WordsConsumed;
        var pending = probed.World.Agenda.Outstanding.ToArray();
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var trace = new AbilitySelectorTraceContext(probed.World.TheCardIn(DeckType.VillainArea)!.ObjectId,
            scope, [], [], [], []);
        for (int repeat = 0; repeat < 3; repeat++)
        {
            var live = context.Selectors().Every(Selection);
            Assert.Equal(live, AbilityProjectedStateQueries.ProjectedEvery(Selection, new(context), context));
            Assert.Equal(live, AbilitySelectorTrace.MatchingCandidates(Selection, trace));
        }
        Assert.Equal(digest, probed.World.Digest().Canonical());
        Assert.Equal(random.Words, probed.World.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, probed.World.Random.Generator.GetState().Index);
        Assert.Equal(words, probed.World.Random.Generator.WordsConsumed);
        Assert.Equal(pending, probed.World.Agenda.Outstanding.ToArray());
        foreach (var fixture in new[] { probed, control })
        {
            var source = fixture.World.Cards.Single(card => card.FaceId == "01006" && DeckTypes.IsInPlay(card.Area.Type));
            var action = Assert.Single(fixture.Game.Pending!.Affordances, item => item.AnchorId == source.ObjectId);
            fixture.Game.Resolve(Decision.Take(action.Id));
        }
        Assert.Equal(1, probed.World.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(control.World.Digest().Canonical(), probed.World.Digest().Canonical());
        Assert.Equal(control.Game.Pending!.Asking, probed.Game.Pending!.Asking);
        Assert.Equal(control.Game.Pending.Affordances.Select(item => (item.Id, item.Verb, item.AnchorId)),
            probed.Game.Pending.Affordances.Select(item => (item.Id, item.Verb, item.AnchorId)));
        Assert.Equal(control.World.Agenda.Outstanding.ToArray(), probed.World.Agenda.Outstanding.ToArray());
        Assert.Equal(control.World.Random.Generator.GetState().Words, probed.World.Random.Generator.GetState().Words);
        Assert.Equal(control.World.Random.Generator.GetState().Index, probed.World.Random.Generator.GetState().Index);
        Assert.Equal(control.World.Random.Generator.WordsConsumed, probed.World.Random.Generator.WordsConsumed);
    }

    private static (Game Game, World World) ExecutionFixture() => Playing(board =>
    {
        InPlay(board, "01006");
        board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
    }, hero: true, abilities: DamageContinuationRunner("""
        {"dealDamage":{"cards":{"query":"villainAndMinionsEngagedWithYou"},"amount":1}}
        """));

    [Fact]
    public void RecipientOrderIsVillainThenEngagedAreaOrderAcrossQueries()
    {
        var world = Hero("01001a", 2);
        var area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1));
        var older = world.CreateCard("01101", area);
        var newer = world.CreateCard("01101", area);
        World.MoveToTop(older, area);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var context = Context(world, 1);
        Card[] expected = [villain, .. area.Cards];
        Assert.Equal([newer, older], area.Cards);
        Assert.Equal(expected, context.Selectors().Every(Selection));
        Assert.Equal(expected, AbilityProjectedStateQueries.ProjectedEvery(Selection, new(context), context));
        var scope = new AbilityAdmission.AbilityAdmissionScope(context, []);
        var trace = new AbilitySelectorTraceContext(villain.ObjectId, scope, [], [], [], []);
        Assert.Equal(expected, AbilitySelectorTrace.MatchingCandidates(Selection, trace));
    }

    [Fact]
    public void TopologySelectsActiveVillainAndProjectedSuccessorRatherThanEveryVillain()
    {
        var (world, groups, scope) = WreckingProjectionFixture.Create();
        var context = scope.Context;
        Assert.Equal([groups[1].Villain], context.Selectors().Every(Selection));
        var next = groups[2].Stages[1];
        var state = new AreaProjectionState(context) { ActiveVillain = next.ObjectId };
        state.Departed.Add(groups[2].Villain.ObjectId);
        state.Entered.Add(next.ObjectId);
        Assert.Equal([next], AbilityProjectedStateQueries.ProjectedEvery(Selection, state, context));
        var trace = new AbilitySelectorTraceContext(next.ObjectId, scope, [groups[2].Villain.ObjectId], [], [], [])
        {
            EncounterStages = groups.ToDictionary(group => group.Villain.ObjectId,
                group => group == groups[2] ? next.ObjectId : group.Villain.ObjectId),
        };
        Assert.Equal([next], AbilitySelectorTrace.MatchingCandidates(Selection, trace));
    }

    [Fact]
    [Rule("rr:damage.step.5")]
    [Rule("rr:damage.step.6")]
    public void EveryRecipientTakesDamageBeforeFirstDefeatWindow()
    {
        // Step 5: "Placing of damage on the character." Step 6: "Abilities that trigger
        // “when [character] would be defeated...”". A simultaneous effect places all recipients before defeat.
        var runner = DamageContinuationRunner("""
            {"dealDamage":{"cards":{"query":"villainAndMinionsEngagedWithYou"},"amount":1}}
            """);
        Card? source = null;
        Card? first = null;
        Card? later = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "01006");
            InPlay(board, "01092");
            first = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            first.TakeDamage(Damage.Health(board, board.Facts, first) - 1);
            later = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        int hand = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(1, later!.Damage);
        Assert.Equal(hand, world.Seats[0].Hand.Cards.Count);
        game.Resolve(Decision.Decline);
        Assert.Equal(1, later.Damage);
        Assert.Equal(hand + 1, world.Seats[0].Hand.Cards.Count);
    }

    private static AbilityAdmissionContext Context(World world, int player)
    {
        var source = world.Seats[player].IdentityCard;
        var query = new AbilityQueryContext(world, source, new Occurrence(player, []), player,
            source.Incarnation, null, null, null, []);
        return new(AbilityLowering.Book(AbilityCatalog.Parse("{\"cards\":[]}")), world.ResourceAbilities,
            new(query, ImmutableDictionary<string, long>.Empty, [], "", 0, false, null), new(), null);
    }
}
