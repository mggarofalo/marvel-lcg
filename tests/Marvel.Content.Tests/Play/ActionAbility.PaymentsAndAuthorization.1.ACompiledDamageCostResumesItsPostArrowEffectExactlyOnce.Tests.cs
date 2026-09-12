using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class ActionAbilityPaymentsAndAuthorizationACompiledDamageCostResumesItsPostArrowEffectExactlyOnceTests
{
    [Rule("rr:initiating-abilities.step.5")]
    [Theory]
    [InlineData("dealDamage")]
    [InlineData("takeDamage")]
    public void ACompiledDamageCostResumesItsPostArrowEffectExactlyOnce(string damageCost)
    {
        // rr:initiating-abilities.step.5: "Pay the cost(s)." An interrupt during the
        // damage cost must finish before the post-arrow draw, without paying
        // the cost again when the ability resumes.
        var runner = new Marvel.Cards.Run.AbilityRunner(AbilityCatalog.Parse($$$"""
            {"cards":[
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"seq":[{"exhaust":"this"},{"{{{damageCost}}}":{"cards":"this","amount":2}}]},
                "effect":{"draw":{"player":"you","count":1}}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            source.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.False(source!.Ready);
        Assert.Equal(4, source.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        game.Resolve(Decision.Decline);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ADamageCostThenChoiceResumesItsFollowingEffectExactlyOnce()
    {
        // rr:initiating-abilities.step.5: "Pay the cost(s)." The completed
        // damage cost is not part of either continuation: first the defeat
        // interrupt resumes the post-arrow effect, then the answer resumes the
        // structural suffix without paying that cost again.
        var runner = new Marvel.Cards.Run.AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                "cost":{"seq":[{"exhaust":"this"},{"dealDamage":{"cards":"this","amount":2}}]},
                "effect":{"seq":[
                  {"choose":{"options":[
                    {"draw":{"player":"you","count":1}},
                    {"draw":{"player":"you","count":2}}
                  ]}},
                  {"draw":{"player":"you","count":4}}
                ]}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            source.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.False(source!.Ready);
        Assert.Equal(4, source.Damage);
        game.Resolve(Decision.Decline);
        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        // The engine chooses this persisted key as a one-shot transition
        // marker. Consuming it before the next suspension prevents a later
        // ResumeAbility continuation from restarting the paid-cost boundary.
        Assert.False(world.Agenda.Current?.AbilityResults?.ContainsKey("costProcedurePending") ?? false);
        game.Resolve(Decision.Take(0));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(held + 5, world.Seats[0].Hand.Cards.Count);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void PaymentChoicesAndValidationUseTheCompiledCostSnapshot()
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """);
        var fields = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["from"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { ["query"] = new AbilityValue.Word("alliesYouControl"), }),
            ["count"] = new AbilityValue.Number(1),
        };
        var ability = parsed.Abilities[0] with
        {
            Cost = new AbilityNode("exhaustChosen", new AbilityValue.Map(fields)),
        };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([ability], parsed.Authored));
        Card? source = null;
        Card? ally = null;
        var(_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            ally = board.CreateCard(AuthoredCards.BlackCat, board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        }, abilities: runner);
        // The executable program is a snapshot. Changing the caller-owned
        // syntax cannot change either the offered count or the accepted answer.
        fields["count"] = new AbilityValue.Number(99);
        var pending = Assert.Single(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        var target = Assert.IsType<TargetRequest>(runner.Describe(world, pending).Targets);
        Assert.Equal(1, target.Min);
        Assert.Equal(1, target.Max);
        Assert.Equal([ally!.ObjectId], target.Legal);
        runner.Act(world, pending, [], [ally.ObjectId]);
        Assert.False(ally.Ready);
    }

    [Fact]
    public void PaymentExecutionUsesTheCompiledCostSnapshot()
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01030","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """);
        var fields = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["card"] = new AbilityValue.Word("this"),
            ["amount"] = new AbilityValue.Number(1),
        };
        var ability = parsed.Abilities[0] with
        {
            Cost = new AbilityNode("heal", new AbilityValue.Map(fields)),
        };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([ability], parsed.Authored));
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            source.TakeDamage(2);
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        // Engine choice: compilation snapshots authored syntax. Payment must
        // use that program even if its caller later edits the input dictionary.
        fields["amount"] = new AbilityValue.Number(2);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(1, source!.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AbilityGuardUsesTheCompiledNumericSnapshot()
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """);
        var fields = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["value"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { ["add"] = new AbilityValue.List([new AbilityValue.Number(1), new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { ["damageOn"] = new AbilityValue.Word("you"), }), ]), }),
            ["count"] = new AbilityValue.Number(2),
        };
        var ability = parsed.Abilities[0] with
        {
            When = new AbilityNode("atLeast", new AbilityValue.Map(fields))
        };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([ability], parsed.Authored));
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(1);
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        // Engine choice: guard evaluation reads the compiled program, not a
        // caller-owned dictionary that can change between offering and acting.
        fields["count"] = new AbilityValue.Number(99);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void CompiledGuardRefusesAnAmbiguousSingleCardSearch(int copies)
    {
        var runner = new Marvel.Cards.Run.AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "when":{"atLeast":{"value":{"damageOn":{"cardsIn":{
                "area":"encounterDiscardPile","title":"Hawkeye"
              }}},"count":0}},
              "cost":{"exhaust":"this"},
              "effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """));
        Card? source = null;
        (Game Game, World World) Start() => Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            for (int index = 0; index < copies; index++)
                board.CreateCard("01066", board.AreaOf(DeckType.EncounterDiscardPile));
        }, abilities: runner);
        if (copies == 1)
        {
            var(game, _) = Start();
            Assert.Contains(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        }
        else
        {
            var refused = Assert.Throws<RulesNotImplementedException>(() => Start());
            Assert.Contains("2 matching cards", refused.Message, StringComparison.Ordinal);
        }

        Assert.True(source!.Ready);
    }

    [Fact]
    public void RuntimeCardMetadataUsesTheCompiledSnapshot()
    {
        var authored = new HashSet<string>(StringComparer.Ordinal)
        {
            "01080"
        };
        var firstPlayer = new HashSet<string>(StringComparer.Ordinal)
        {
            "01080"
        };
        var placementOnly = new HashSet<string>(StringComparer.Ordinal);
        var pools = new Dictionary<string, CardCounterPool>(StringComparer.Ordinal)
        {
            ["01080"] = new("medical", 3, Uses: true),
        };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([], authored, ControlledByFirstPlayer: firstPlayer, PlacementOnly: placementOnly, CounterPools: pools));
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, "01080"));
        // Engine choice: caller-owned syntax cannot alter a compiled program.
        authored.Clear();
        firstPlayer.Clear();
        placementOnly.Add("01080");
        pools["01080"] = new("medical", 9, Uses: false);
        Assert.Contains("01080", runner.Authored);
        Assert.Equal(world.FirstPlayer, runner.SetupController(world, source!));
        Assert.Equal(new CardCounterPool("medical", 3, Uses: true), runner.CounterPool(world, source!));
        Assert.Empty(runner.WhenRevealed(world, source!, 0));
        runner.ValidateForPlay(world);
    }
}
