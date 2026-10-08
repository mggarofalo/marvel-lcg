using Marvel.Server;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardFactClientValidationTests : LocalGameClientTestBase
{
    [Theory]
    [InlineData("missing-collection")]
    [InlineData("missing-value")]
    [InlineData("missing-numeral")]
    [InlineData("empty-attribute")]
    [InlineData("negative-consequence")]
    public async Task MalformedPrintedFactsAreRejectedBeforeRendering(string problem)
    {
        var valid = new CardPrintedValue("3", true, false, 1);
        IReadOnlyDictionary<string, CardPrintedValue>? values = problem switch
        {
            "missing-collection" => null,
            "missing-value" => new Dictionary<string, CardPrintedValue> { ["ATK"] = null! },
            "missing-numeral" => new Dictionary<string, CardPrintedValue> { ["ATK"] = valid with { Value = null! } },
            "empty-attribute" => new Dictionary<string, CardPrintedValue> { [" "] = valid },
            "negative-consequence" => new Dictionary<string, CardPrintedValue>
                { ["ATK"] = valid with { ConsequentialDamage = -1 } },
            _ => throw new ArgumentOutOfRangeException(nameof(problem)),
        };

        ClientStartupResult result = await OpenWithPrintedFacts(values!);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_response", result.Error?.Code);
    }

    [Theory]
    [InlineData("0", false, false, 0)]
    [InlineData("X", true, false, 0)]
    [InlineData("—", false, false, 0)]
    [InlineData("★", false, false, 0)]
    [InlineData("★", true, false, 0)]
    [InlineData("3", true, false, 3)]
    [InlineData("12", true, true, 0)]
    public async Task IndependentPrintedMarksSurviveClientAdmission(
        string numeral, bool specialStar, bool perPlayer, int consequences)
    {
        // Synthetic wire shapes exercise admission; they do not enable new game content.
        var value = new CardPrintedValue(numeral, specialStar, perPlayer, consequences);

        ClientStartupResult result = await OpenWithPrintedFacts(
            new Dictionary<string, CardPrintedValue> { ["ATK"] = value });

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
    }

    private static async Task<ClientStartupResult> OpenWithPrintedFacts(
        IReadOnlyDictionary<string, CardPrintedValue> values) =>
        await OpenWithFace(face => face with { PrintedValues = values });

    [Theory]
    [InlineData("missing-collection")]
    [InlineData("missing-value")]
    [InlineData("missing-calculation")]
    [InlineData("missing-step")]
    [InlineData("missing-operation")]
    [InlineData("missing-source-text")]
    [InlineData("historical-live-link")]
    public async Task MalformedEffectiveFactsAreRejectedBeforeRendering(string problem)
    {
        var source = new CardValueSourceDescriptor("01091", "Combat Training", 7, false);
        var step = new CardValueCalculation("Add", 1, source, null);
        var value = new CardEffectiveValue(2, 3, "Printed", true, [step]);
        IReadOnlyDictionary<string, CardEffectiveValue>? values = problem switch
        {
            "missing-collection" => null,
            "missing-value" => new Dictionary<string, CardEffectiveValue> { ["ATK"] = null! },
            "missing-calculation" => Facts(value with { Calculation = null! }),
            "missing-step" => Facts(value with { Calculation = [null!] }),
            "missing-operation" => Facts(value with { Calculation = [step with { Operation = null! }] }),
            "missing-source-text" => Facts(value with
                { Calculation = [step with { Source = source with { RulesText = null! } }] }),
            "historical-live-link" => Facts(value with
                { Calculation = [step with { Source = source with { Historical = true } }] }),
            _ => throw new ArgumentOutOfRangeException(nameof(problem)),
        };

        ClientStartupResult result = await OpenWithFace(face => face with { EffectiveValues = values! });

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_response", result.Error?.Code);
    }

    [Fact]
    public async Task AuthorizedSourcesNeedNotSumToTheEffectiveQuantity()
    {
        // Synthetic restricted visibility: undisclosed sources are absent, not placeholders.
        var source = new CardValueSourceDescriptor("01091", "Combat Training", null, true);
        var value = new CardEffectiveValue(2, 5, "Printed", true,
            [new CardValueCalculation("Add", 1, source, new CardValueDuration("round", null, null, false))]);

        ClientStartupResult result = await OpenWithFace(face => face with { EffectiveValues = Facts(value) });

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
    }

    private static Dictionary<string, CardEffectiveValue> Facts(CardEffectiveValue value) =>
        new Dictionary<string, CardEffectiveValue> { ["ATK"] = value };

    private static async Task<ClientStartupResult> OpenWithFace(
        Func<CardFaceDescriptor, CardFaceDescriptor> change) =>
        await OpenWithCard(card => card with { Face = change(card.Face!) });

    [Theory]
    [InlineData("source")]
    [InlineData("historical-source")]
    [InlineData("relation")]
    [InlineData("contributions")]
    [InlineData("contribution")]
    [InlineData("abilities")]
    [InlineData("ability")]
    [InlineData("trigger")]
    [InlineData("costs")]
    [InlineData("cost")]
    [InlineData("effects")]
    [InlineData("effect")]
    [InlineData("threshold")]
    public async Task IncompletePersistentFactsCannotEnterClientState(string problem)
    {
        CardPersistentDescriptor valid = PersistentFacts();
        CardPersistentAbilityDescriptor ability = valid.Abilities[0];
        CardPersistentDescriptor invalid = new Dictionary<string, CardPersistentDescriptor>
        {
            ["source"] = valid with { Source = null! },
            ["historical-source"] = valid with { Source = valid.Source with { Historical = true } },
            ["relation"] = valid with { Relation = null! },
            ["contributions"] = valid with { Contributions = null! },
            ["contribution"] = valid with { Contributions = [null!] },
            ["abilities"] = valid with { Abilities = null! },
            ["ability"] = valid with { Abilities = [null!] },
            ["trigger"] = valid with { Abilities = [ability with { Trigger = null! }] },
            ["costs"] = valid with { Abilities = [ability with { Costs = null! }] },
            ["cost"] = valid with { Abilities = [ability with { Costs = [null!] }] },
            ["effects"] = valid with { Abilities = [ability with { Effects = null! }] },
            ["effect"] = valid with { Abilities = [ability with { Effects = [null!] }] },
            ["threshold"] = valid with { Abilities = [ability with
                { Effects = [ability.Effects[0] with { Condition = new("Source", null!, 5) }] }] },
        }[problem];

        ClientStartupResult result = await OpenWithCard(card => card with { Persistent = invalid });

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_response", result.Error?.Code);
    }

    [Fact]
    public async Task SignedContributionsAndCompletePersistentFactsAreAdmitted()
    {
        ClientStartupResult result = await OpenWithCard(card => card with { Persistent = PersistentFacts() });

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
    }

    private static CardPersistentDescriptor PersistentFacts() => new(
        new("fixture", "Synthetic source", 7, false), new("Controlled", null, 0),
        [new(1, "ATK", "Add", -1, null)],
        [new(new("Action", null, null, null, null, null, null, false),
            [new("Exhaust", "Source", null, null, null, false)],
            [new("Ready", "ActingIdentity", null, null, null, null, null)])], false);

    private static async Task<ClientStartupResult> OpenWithCard(
        Func<CardDescriptor, CardDescriptor> change)
    {
        EngineResponse opened = Host().Exchange(
            EngineRequest.OpenGame("local-open", LocalGameSession.GameId, Specification()));
        WorldDescriptor world = opened.World!;
        CardDescriptor readable = world.Areas.SelectMany(area => area.Cards)
            .First(card => card.Face is not null);
        CardDescriptor replacement = change(readable);
        WorldDescriptor changed = world with
        {
            Areas = world.Areas.Select(area => area with
            {
                Cards = area.Cards.Select(card => ReferenceEquals(card, readable) ? replacement : card).ToArray(),
            }).ToArray(),
        };
        return await new LocalGameClient(new FixedTransport(opened with { World = changed }))
            .OpenAsync(Specification(), TestContext.Current.CancellationToken);
    }
}
