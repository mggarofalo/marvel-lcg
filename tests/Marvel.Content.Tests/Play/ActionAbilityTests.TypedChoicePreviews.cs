using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ChoicePreviewDistinguishesAttackContextFromOrdinaryDamage(bool attack)
    {
        const string damage = """{"dealDamage":{"cards":"chosen","amount":2}}""";
        string effect = attack ? $$$$$$"""{"attack":{"target":"chosen","effect":{{{{{{damage}}}}}}}}""" : damage;
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"chooseCard":{"from":{"query":"villain"},"effect":{{{{{{effect}}}}}}}}
            """);
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
        }, hero: true, abilities: runner);

        var prompt = Assert.IsType<Prompt>(runner.Choosing(world, source!, 0, 0, null, false));
        var choice = Assert.Single(prompt.Affordances);
        Assert.Equal(attack, choice.Description!.Contains("Stunned cancels this attack; no damage will be dealt", StringComparison.Ordinal));
        Assert.Equal(!attack, choice.Description.Contains("HP", StringComparison.Ordinal));
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ChoicePreviewKeepsItsCompiledOverkillFlag(bool overkill)
    {
        // The engine's preview and execution share the compiled keyword,
        // independently of subsequent changes to the supplied syntax map.
        string marker = overkill ? ",\"overkill\":1" : string.Empty;
        var (runner, fields) = MutableNumericRunner("dealAttackDamage",
            $$"""{"cards":"chosen","amount":5{{marker}}}""", false, "minionsEngagedWithYou");
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard(AuthoredCards.Shocker,
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        long excess = 5 - Damage.Health(world, world.Facts, minion!);
        Assert.True(excess > 0);
        if (overkill) fields.Remove("overkill");
        else fields["overkill"] = new AbilityValue.Number(1);

        game.Resolve(Decision.Take(action.Id));

        var choice = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(minion!.ObjectId, choice.Id);
        Assert.Equal(overkill, choice.Description!.Contains($"Overkill carries {excess} excess damage", StringComparison.Ordinal));
        game.Resolve(Decision.Take(choice.Id));

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(overkill ? excess : 0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("leading")]
    [InlineData("later")]
    [InlineData("empty-leading")]
    [InlineData("then")]
    [InlineData("else")]
    [InlineData("absent")]
    public void ChoicePreviewDescribesOnlyTheCurrentlyKnowableDamage(string structure)
    {
        // Preview scope is an engine choice. It follows the active branch,
        // but does not predict damage after an earlier state-changing effect.
        const string damage = """{"dealDamage":{"cards":"chosen","amount":2}}""";
        const string draw = """{"draw":{"player":"you","count":1}}""";
        const string test = """{"inForm":{"player":"you","form":"hero"}}""";
        string effect = structure switch
        {
            "leading" => $$$$$$"""{"seq":[{{{{{{damage}}}}}},{{{{{{draw}}}}}}]}""",
            "later" => $$$$$$"""{"seq":[{{{{{{draw}}}}}},{{{{{{damage}}}}}}]}""",
            "empty-leading" => $$$$$$"""{"seq":[{"seq":[]},{{{{{{damage}}}}}}]}""",
            "then" => $$$$$$"""{"if":{"test":{{{{{{test}}}}}},"then":{{{{{{damage}}}}}},"else":{{{{{{draw}}}}}}}}""",
            "else" => $$$$$$"""{"if":{"test":{"not":{{{{{{test}}}}}}},"then":{{{{{{draw}}}}}},"else":{{{{{{damage}}}}}}}}""",
            _ => $$$$$$"""{"if":{"test":{"not":{{{{{{test}}}}}}},"then":{{{{{{damage}}}}}}}}""",
        };
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"chooseCard":{"from":{"query":"villain"},"effect":{{{{{{effect}}}}}}}}
            """);
        Card? source = null;
        var (_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        // Reading the engine-owned choice directly also exercises the absent
        // branch, which is not offered as a state-changing action on its own.
        var prompt = Assert.IsType<Prompt>(runner.Choosing(world, source!, 0, 0, null, false));
        var choice = Assert.Single(prompt.Affordances);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        string title = world.Facts.Title(villain.FaceId);
        long health = Damage.Health(world, world.Facts, villain);
        Assert.Equal(structure is "leading" or "then" or "else"
            ? $"{title} · {health}/{health} → {health - 2}/{health} HP" : title, choice.Description);
        Assert.Equal(0, villain.Damage);
        Assert.False(world.Agenda.IsBusy);
    }
}
