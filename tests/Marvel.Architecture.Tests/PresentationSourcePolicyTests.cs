using System.Reflection;
using Marvel.Rules.State;
using Marvel.Testing;
using Marvel.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class PresentationSourcePolicyTests
{
    private static readonly string[] ImplicitUsings =
    [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks",
    ];

    [Fact]
    public void ViewUsesOnlyReviewedEngineMembers()
    {
        CSharpCompilation compilation = CompileView();
        string[] actual = ExternalMarvelMembers(compilation);

        string[] reviewed =
        [
            "Marvel.Rules.Events.AreaRef.Host",
            "Marvel.Rules.Events.AreaRef.Owner",
            "Marvel.Rules.Events.AreaRef.Zone",
            "Marvel.Rules.Events.AreaReordered.Area",
            "Marvel.Rules.Events.AreaReordered.Order",
            "Marvel.Rules.Events.CardAttached.Card",
            "Marvel.Rules.Events.CardAttached.Host",
            "Marvel.Rules.Events.CardDetached.Card",
            "Marvel.Rules.Events.CardDetached.Host",
            "Marvel.Rules.Events.CardFormChanged.Card",
            "Marvel.Rules.Events.CardsCreated.Area",
            "Marvel.Rules.Events.CardsCreated.Cards",
            "Marvel.Rules.Events.CardsFlipped.Cards",
            "Marvel.Rules.Events.CardsFlipped.FaceUp",
            "Marvel.Rules.Events.CardsMoved.Cards",
            "Marvel.Rules.Events.CardsMoved.From",
            "Marvel.Rules.Events.CardsMoved.To",
            "Marvel.Rules.Events.ControlChanged.Card",
            "Marvel.Rules.Events.ControlChanged.From",
            "Marvel.Rules.Events.ControlChanged.To",
            "Marvel.Rules.Events.CreatedCard.Id",
            "Marvel.Rules.Events.FieldSet.Card",
            "Marvel.Rules.Events.FieldSet.Field",
            "Marvel.Rules.Events.FieldSet.From",
            "Marvel.Rules.Events.FieldSet.To",
            "Marvel.Rules.Events.GameEvent.Trigger",
            "Marvel.Rules.Events.GameEvent.Verb",
            "Marvel.Rules.Events.Landing.Card",
            "Marvel.Rules.Events.PlayAreaDetached.GameArea",
            "Marvel.Rules.Events.PlayAreaDetached.PlayArea",
            "Marvel.Rules.Events.PlayAreaJoined.GameArea",
            "Marvel.Rules.Events.PlayAreaJoined.PlayArea",
            "Marvel.Rules.Play.EncounterDeck.AccelerationToken",
            "Marvel.Rules.Play.Game.ChangeForm",
            "Marvel.Rules.Play.Game.EndPhaseVerb",
            "Marvel.Rules.Play.Outcome.PlayersLose",
            "Marvel.Rules.Play.Outcome.PlayersWin",
            "Marvel.Rules.Play.Outcome.Unfinished",
            "Marvel.Rules.Play.Outcome.VillainWins",
            "Marvel.Rules.Play.Outcome.operator !=(Marvel.Rules.Play.Outcome, Marvel.Rules.Play.Outcome)",
            "Marvel.Rules.Play.Outcome.operator ==(Marvel.Rules.Play.Outcome, Marvel.Rules.Play.Outcome)",
            "Marvel.Rules.Prompts.Affordance.AnchorId",
            "Marvel.Rules.Prompts.Affordance.AnchorPlayer",
            "Marvel.Rules.Prompts.Affordance.CostOptions",
            "Marvel.Rules.Prompts.Affordance.Description",
            "Marvel.Rules.Prompts.Affordance.Id",
            "Marvel.Rules.Prompts.Affordance.Illegal",
            "Marvel.Rules.Prompts.Affordance.Label",
            "Marvel.Rules.Prompts.Affordance.Targets",
            "Marvel.Rules.Prompts.Affordance.Verb",
            "Marvel.Rules.Prompts.CostOption.Cost",
            "Marvel.Rules.Prompts.CostOption.Generators",
            "Marvel.Rules.Prompts.CostOption.HasAlternative",
            "Marvel.Rules.Prompts.CostOption.OrCost",
            "Marvel.Rules.Prompts.CostOption.OrRule",
            "Marvel.Rules.Prompts.CostOption.Rule",
            "Marvel.Rules.Prompts.Prompt.Affordances",
            "Marvel.Rules.Prompts.Prompt.Asking",
            "Marvel.Rules.Prompts.Prompt.Cancellable",
            "Marvel.Rules.Prompts.Prompt.Description",
            "Marvel.Rules.Prompts.Prompt.ExposesConcealedCandidates",
            "Marvel.Rules.Prompts.Prompt.Label",
            "Marvel.Rules.Prompts.Prompt.Player",
            "Marvel.Rules.Prompts.Prompt.Trigger",
            "Marvel.Rules.Prompts.Prompt.When",
            "Marvel.Rules.Prompts.Question.Element",
            "Marvel.Rules.Prompts.Question.Opportunity",
            "Marvel.Rules.Prompts.Question.Option",
            "Marvel.Rules.Prompts.Question.Order",
            "Marvel.Rules.Prompts.Question.TurnOption",
            "Marvel.Rules.Prompts.Question.operator ==(Marvel.Rules.Prompts.Question, Marvel.Rules.Prompts.Question)",
            "Marvel.Rules.Prompts.TargetRequest.AllowRepeated",
            "Marvel.Rules.Prompts.TargetRequest.Groups",
            "Marvel.Rules.Prompts.TargetRequest.IsGrouped",
            "Marvel.Rules.Prompts.TargetRequest.IsSearch",
            "Marvel.Rules.Prompts.TargetRequest.Legal",
            "Marvel.Rules.Prompts.TargetRequest.Max",
            "Marvel.Rules.Prompts.TargetRequest.Min",
            "Marvel.Rules.State.Area.Cards",
            "Marvel.Rules.State.Area.Host",
            "Marvel.Rules.State.Area.Id",
            "Marvel.Rules.State.Area.PlayArea",
            "Marvel.Rules.State.Area.Removed",
            "Marvel.Rules.State.Area.Type",
            "Marvel.Rules.State.Card.Area",
            "Marvel.Rules.State.Card.Damage",
            "Marvel.Rules.State.Card.FaceId",
            "Marvel.Rules.State.Card.FaceUp",
            "Marvel.Rules.State.Card.HasRegisteredTokens",
            "Marvel.Rules.State.Card.ObjectId",
            "Marvel.Rules.State.Card.Owner",
            "Marvel.Rules.State.Card.Ready",
            "Marvel.Rules.State.Card.Tokens",
            "Marvel.Rules.State.CardKind.Ally",
            "Marvel.Rules.State.CardKind.AlterEgo",
            "Marvel.Rules.State.CardKind.EncounterSideScheme",
            "Marvel.Rules.State.CardKind.EncounterVillain",
            "Marvel.Rules.State.CardKind.Event",
            "Marvel.Rules.State.CardKind.Hero",
            "Marvel.Rules.State.CardKind.Insert",
            "Marvel.Rules.State.CardKind.MainScheme",
            "Marvel.Rules.State.CardKind.Resource",
            "Marvel.Rules.State.CardKind.Support",
            "Marvel.Rules.State.CardKind.Upgrade",
            "Marvel.Rules.State.CardKind.operator ==(Marvel.Rules.State.CardKind, Marvel.Rules.State.CardKind)",
            "Marvel.Rules.State.DeckType.AsideDeck",
            "Marvel.Rules.State.DeckType.HandsArea",
            "Marvel.Rules.State.DeckType.HeroArea",
            "Marvel.Rules.State.DeckType.operator ==(Marvel.Rules.State.DeckType, Marvel.Rules.State.DeckType)",
            "Marvel.Rules.State.DeckTypes.FaceDownOnEntry(Marvel.Rules.State.DeckType)",
            "Marvel.Rules.State.DeckTypes.IsInPlay(Marvel.Rules.State.DeckType)",
            "Marvel.Rules.State.FacedownDrones.Kind(Marvel.Rules.State.Card, Marvel.Rules.State.ICardFacts)",
            "Marvel.Rules.State.GameArea.Id",
            "Marvel.Rules.State.GameArea.PlayAreas",
            "Marvel.Rules.State.ICardFacts.Attributes(string)",
            "Marvel.Rules.State.ICardFacts.Keywords(string)",
            "Marvel.Rules.State.ICardFacts.Kind(string)",
            "Marvel.Rules.State.ICardFacts.PrintedTraits(string)",
            "Marvel.Rules.State.ICardFacts.PrintedValue(string, string, int, long)",
            "Marvel.Rules.State.ICardFacts.Subtitle(string)",
            "Marvel.Rules.State.ICardFacts.Text(string)",
            "Marvel.Rules.State.ICardFacts.Title(string)",
            "Marvel.Rules.State.ICardFacts.Traits(string)",
            "Marvel.Rules.State.PlayArea.IsPlayers",
            "Marvel.Rules.State.PlayArea.Player",
            "Marvel.Rules.State.Seat.Eliminated",
            "Marvel.Rules.State.Seat.Index",
            "Marvel.Rules.State.Seat.Name",
            "Marvel.Rules.State.StateFields.For(Marvel.Rules.State.Card, Marvel.Rules.State.ICardFacts, int, bool, bool, bool, Marvel.Rules.State.World?)",
            "Marvel.Rules.State.Traits.Of(Marvel.Rules.State.World, Marvel.Rules.State.Card, Marvel.Rules.State.ICardFacts)",
            "Marvel.Rules.State.World.Areas",
            "Marvel.Rules.State.World.Cards",
            "Marvel.Rules.State.World.Facts",
            "Marvel.Rules.State.World.FirstPlayer",
            "Marvel.Rules.State.World.GameAreas",
            "Marvel.Rules.State.World.Players",
            "Marvel.Rules.State.World.Result",
            "Marvel.Rules.State.World.Seats",
            "Marvel.Rules.Timing.TimingPriority.Interrupt",
            "Marvel.Rules.Timing.TimingPriority.Response",
            "Marvel.Rules.Timing.TimingPriority.operator ==(Marvel.Rules.Timing.TimingPriority, Marvel.Rules.Timing.TimingPriority)",
        ];
        Assert.Equal(reviewed, actual);
    }

    [Fact]
    public void SourcePolicySeesConstantsAndMutators()
    {
        const string source = """
            using Marvel.Rules.Play;
            using Marvel.Rules.State;

            internal static class BoundaryViolation
            {
                public static void Mutate(Card card)
                {
                    _ = EncounterDeck.AccelerationToken;
                    card.Exhaust();
                }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "BoundaryViolation",
            [CSharpSyntaxTree.ParseText(
                source,
                cancellationToken: TestContext.Current.CancellationToken)],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Assert.Equal(
            [
                "Marvel.Rules.Play.EncounterDeck.AccelerationToken",
                "Marvel.Rules.State.Card.Exhaust()",
            ],
            ExternalMarvelMembers(compilation));
    }

    private static CSharpCompilation CompileView()
    {
        string sourceDirectory = Path.Combine(RepositoryPaths.Root, "src", "Marvel.View");
        SyntaxTree implicitUsings = CSharpSyntaxTree.ParseText(
            string.Join("\n", ImplicitUsings.Select(value => $"global using {value};")),
            new CSharpParseOptions(LanguageVersion.Latest),
            "ImplicitUsings.g.cs");
        SyntaxTree[] source = Directory.GetFiles(sourceDirectory, "*.cs")
            .Order(StringComparer.Ordinal)
            .Select(path => CSharpSyntaxTree.ParseText(
                File.ReadAllText(path),
                new CSharpParseOptions(LanguageVersion.Latest),
                path))
            .Prepend(implicitUsings)
            .ToArray();
        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Marvel.View.SourcePolicy",
            source,
            References(),
            options);
        Diagnostic[] errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.True(errors.Length == 0,
            "The View source policy could not compile its subject:\n"
                + string.Join("\n", errors.Select(error => error.ToString())));
        return compilation;
    }

    private static MetadataReference[] References()
    {
        string[] platformAssemblies = ((string?)AppContext.GetData(
                "TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return platformAssemblies
            .Append(typeof(World).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }

    private static string[] ExternalMarvelMembers(CSharpCompilation compilation)
    {
        var members = new HashSet<string>(StringComparer.Ordinal);
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
            {
                ISymbol? symbol = model.GetSymbolInfo(node).Symbol;
                if (symbol is not (IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol)
                    || symbol.ContainingAssembly?.Name is not { } assembly
                    || !assembly.StartsWith("Marvel.", StringComparison.Ordinal)
                    || assembly == compilation.AssemblyName)
                {
                    continue;
                }

                members.Add(symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            }
        }

        return members.Order(StringComparer.Ordinal).ToArray();
    }
}
