using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Xunit;

namespace Marvel.Content.Tests.Cards;

public sealed class AbilityProgramTests
{
    [Fact]
    public void AttachmentBindingsUseTheSameSemanticVocabularyAsEffects()
    {
        var source = AbilityCatalog.Parse("""
            {"cards":[{"card":"01005","attachTo":"you","abilities":[]}]}
            """);
        var program = AbilityLowering.Book(source);
        Assert.Equal(new AbilityCardSelection.Bound(AbilityCardBinding.You), program.AttachTo["01005"]);

        var invalid = AbilityCatalog.Parse("""
            {"cards":[{"card":"01005","attachTo":{"query":"yourIdentity"},"abilities":[]}]}
            """);
        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(invalid));
        Assert.Contains("attachTo/query", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCompleteCoreBookLowersWithEveryFaceAndAbility()
    {
        var source = AuthoredCards.Book;
        var program = AbilityLowering.Book(source);

        Assert.Equal(209, program.Authored.Count);
        Assert.True(program.Authored.SetEquals(source.Authored));
        Assert.Equal(source.Abilities.Count, program.Abilities.Length);
        Assert.Equal(source.Abilities.Select(ability => (ability.Card, ability.Name)),
            program.Abilities.Select(ability => (ability.Card, ability.Name)));
        Assert.Equal(source.AttachTo!.Count, program.AttachTo.Count);
        Assert.Equal(source.CounterPools!.Count, program.CounterPools.Count);
        Assert.All(program.Abilities, ability => Assert.Same(ability.Effect, program.Effects[ability.Address]));
        Assert.True(program.Effects.Count > program.Abilities.Length);
        Assert.Empty(program.On("unpublished-face"));
    }

    [Fact]
    public void NestedAddressesUseOnlyExplicitStructureAndAuthoredOrder()
    {
        var first = AbilityLowering.Book(Parse(
            """{"if":{"test":{"finalStep":"true"},"then":{"seq":[{"ready":"you"},{"exhaust":"you"}]},"else":{"ready":"this"}}}"""));
        var reorderedFields = AbilityLowering.Book(Parse(
            """{"if":{"else":{"ready":"this"},"then":{"seq":[{"ready":"you"},{"exhaust":"you"}]},"test":{"finalStep":"true"}}}"""));

        var expected = new[] { "effect", "effect/if/else", "effect/if/then", "effect/if/then/seq/0", "effect/if/then/seq/1" };
        Assert.Equal(expected, first.Effects.Keys.Select(address => address.Path).Order(StringComparer.Ordinal));
        Assert.Equal(expected, reorderedFields.Effects.Keys.Select(address => address.Path).Order(StringComparer.Ordinal));
        var last = new AbilityEffectAddress("01005", 0, "effect/if/then/seq/1");
        Assert.Equal(new AbilityEffect.CardAction(AbilityCardInstruction.Exhaust,
            new AbilityCardSelection.Bound(AbilityCardBinding.You)), first.Effects[last]);
    }

    [Fact]
    public void CompilationSnapshotsMutableAuthoredMetadata()
    {
        var parsed = Parse("""{"ready":"you"}""");
        var authored = new HashSet<string>(parsed.Authored, StringComparer.Ordinal);
        var labels = new List<string> { "attack" };
        var abilities = new List<CardAbility> { parsed.Abilities[0] with { Labels = labels } };
        var program = AbilityLowering.Book(new AbilityBook(abilities, authored));

        labels.Clear();
        abilities.Clear();
        authored.Clear();

        Assert.Equal("attack", Assert.Single(Assert.Single(program.Abilities).Labels));
        Assert.Contains("01005", program.Authored);
        Assert.Single(program.On("01005"));
    }

    [Fact]
    public void TheBookBoundaryRejectsAnUnknownPropertyInAnUnchosenBranch()
    {
        var source = Parse(
            """{"if":{"test":{"finalStep":"true"},"then":{"ready":"you"},"else":{"draw":{"player":"you","count":1,"ignored":1}}}}""");

        var failure = Assert.Throws<AbilityException>(() => AbilityLowering.Book(source));

        Assert.Contains("'01005' ability 0 at 'effect/if/else/draw/ignored'", failure.Message, StringComparison.Ordinal);

        var runnerFailure = Assert.Throws<AbilityException>(() => new AbilityRunner(source));
        Assert.Equal(failure.Message, runnerFailure.Message);
    }

    private static AbilityBook Parse(string effect) => AbilityCatalog.Parse($$$"""
        {"cards":[{"card":"01005","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
          "effect":{{{effect}}}
        }]}]}
        """);
}
