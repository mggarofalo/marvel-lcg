using Marvel.Protocol;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Marvel.Architecture.Tests;

internal static class GeneratedPartialTypePolicy
{
    internal static bool IsRequired(
        string path, CompilationUnitSyntax root, TypeDeclarationSyntax declaration)
    {
        // These declarations participate in Godot, JSON or regex source generation.
        // Exact paths and sole declarations prevent an exemption from admitting split logic.
        var required = path switch
        {
            // Native row input and drop callbacks require Godot source generation.
            "src/Marvel.Godot/DeckCardTable.cs" => (Namespace: "Marvel.Godot", Type: "DeckCardTable"),
            "src/Marvel.Godot/DeckCardRow.cs" => (Namespace: "Marvel.Godot", Type: "DeckCardRow"),
            "src/Marvel.Godot/Main.cs" => (Namespace: "Marvel.Godot", Type: "Main"),
            "src/Marvel.Godot/CardControl.cs" => (Namespace: "Marvel.Godot", Type: "CardControl"),
            "src/Marvel.Godot/DecisionPanel.cs" => (Namespace: "Marvel.Godot", Type: "DecisionPanel"),
            "src/Marvel.Godot/AbilityWindowLayoutSample.cs" => (Namespace: "Marvel.Godot", Type: "AbilityWindowLayoutSample"),
            "src/Marvel.Godot/HistoryOccurrenceList.cs" => (Namespace: "Marvel.Godot", Type: "HistoryOccurrenceList"),
            "src/Marvel.Godot/HistoryOccurrenceRow.cs" => (Namespace: "Marvel.Godot", Type: "HistoryOccurrenceRow"),
            "src/Marvel.Godot/CardInspectorTransition.cs" => (Namespace: "Marvel.Godot", Type: "CardInspectorTransition"),
            "src/Marvel.Godot/CardInspectorLayout.cs" => (Namespace: "Marvel.Godot", Type: "CardInspectorLayout"),
            "src/Marvel.Godot/EventActivityPresenter.cs" => (Namespace: "Marvel.Godot", Type: "EventActivityPresenter"),
            "src/Marvel.Godot/EventActivityPulse.cs" => (Namespace: "Marvel.Godot", Type: "EventActivityPulse"),
            "src/Marvel.Godot/SourceGridGesture.cs" => (Namespace: "Marvel.Godot", Type: "SourceGridGesture"),
            "src/Marvel.Godot/SmoothRelationshipLine.cs" => (Namespace: "Marvel.Godot", Type: "SmoothRelationshipLine"),
            "src/Marvel.Godot/StatusCardBadge.cs" => (Namespace: "Marvel.Godot", Type: "StatusCardBadge"),
            // Global transform notifications track actual themed table placement.
            "src/Marvel.Godot/TableCanvasSurface.cs" => (Namespace: "Marvel.Godot", Type: "TableCanvasSurface"),
            "src/Marvel.Godot/smoke/ResourceFinalizerProbe.cs" => (Namespace: "Marvel.Godot", Type: "ResourceFinalizerProbe"),
            "src/Marvel.Godot/smoke/CardFaceReuseNativeProbe.cs" => (Namespace: "Marvel.Godot", Type: "CardFaceReuseNativeProbe"),
            "src/Marvel.Godot/smoke/CampaignTrackerNativeProbe.cs" => (Namespace: "Marvel.Godot", Type: "CampaignTrackerNativeProbe"),
            "src/Marvel.Godot/smoke/GroupedDecisionNativeProbe.cs" => (Namespace: "Marvel.Godot", Type: "GroupedDecisionNativeProbe"),
            "src/Marvel.Godot/smoke/DecisionDialogNativeProbe.cs" => (Namespace: "Marvel.Godot", Type: "DecisionDialogNativeProbe"),
            "src/Marvel.Godot/smoke/NativeGameWitness.cs" => (Namespace: "Marvel.Godot", Type: "NativeGameWitness"),
            "src/Marvel.Godot/smoke/TooltipPolicyProbe.cs" => (Namespace: "Marvel.Godot", Type: "TooltipPolicyProbe"),
            "src/Marvel.Protocol/EngineJsonContext.cs" => (Namespace: "Marvel.Protocol", Type: "EngineJsonContext"),
            "tools/Marvel.Cards.Extract/Keywords.cs" => (Namespace: "Marvel.Cards.Extract", Type: "Keywords"),
            "tools/Marvel.Cards.Extract/Printed.cs" => (Namespace: "Marvel.Cards.Extract", Type: "Printed"),
            "tools/Marvel.MarvelCdb.Harvest/CanonicalJson.cs" => (Namespace: "Marvel.MarvelCdb.Harvest", Type: "CanonicalJson"),
            "tools/Marvel.MarvelCdb.Harvest/Harvest.cs" => (Namespace: "Marvel.MarvelCdb.Harvest", Type: "Harvest"),
            "tools/Marvel.Release/ReleaseVersion.cs" => (Namespace: "Marvel.Release", Type: "ReleaseVersion"),
            "tools/Marvel.Rules.Harvest/Markdown.cs" => (Namespace: "Marvel.Rules.Harvest", Type: "Markdown"),
            "tools/Marvel.Rules.Packs.Harvest/Emit.cs" => (Namespace: "Marvel.Rules.Packs.Harvest", Type: "Emit"),
            "tools/Marvel.Rules.Packs.Harvest/Harvest.cs" => (Namespace: "Marvel.Rules.Packs.Harvest", Type: "Harvest"),
            "tools/Marvel.Rulings.Harvest/Harvest.cs" => (Namespace: "Marvel.Rulings.Harvest", Type: "Harvest"),
            "tools/Marvel.Rulings.Harvest/Html.cs" => (Namespace: "Marvel.Rulings.Harvest", Type: "Html"),
            _ => default,
        };
        return required.Type is not null
            && declaration.Identifier.ValueText == required.Type
            && root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Count(type => type.Modifiers.Any(SyntaxKind.PartialKeyword)) == 1
            && root.Members.Count == 1
            && root.Members[0] is FileScopedNamespaceDeclarationSyntax space
            && space.Name.ToString() == required.Namespace
            && declaration.Parent == space;
    }
}
