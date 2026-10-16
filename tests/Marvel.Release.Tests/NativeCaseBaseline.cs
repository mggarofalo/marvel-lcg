namespace Marvel.Release.Tests;

/// <summary>The reviewed union of native obligations preceding the shared runner.</summary>
internal static class NativeCaseBaseline
{
    internal static IEnumerable<string> Ids()
    {
        foreach (string id in Expand("card-face-sample card-live-state card-source card-inspection card-visual-sample", "motion", 50, 80, 100, 150)) yield return id;
        foreach (string id in Expand("source-tableau source-vacancy source-drag-hit", "motion", 80, 100, 150)) yield return id;
        foreach (string id in Expand("source-tableau", "narrow", 80, 100, 150)) yield return id;
        yield return "source-weighted-gesture-100-motion";
        yield return "source-weighted-gesture-100-reduced";
        yield return "legal-iron-man-tableau-150-reduced";
        foreach (string id in Expand("""
            setup-readiness close-resolution shared-control-finish cursor-theme control-material
            theme-palette card-face-reconciliation next-decision-access causal-activity causal-arrival
            causal-departure causal-table-transition weighted-gesture loop-drag tooltip-action-polish
            source-preparation shell-finish ultron-inspection boost-choice decision-catalogue
            """, "motion", 100)) yield return id;
        foreach (string id in Expand("""
            minion-order ability-choice deferred-event repeated-commit hand-intent discard-preview
            recipient-commitment layout-continuity resource-finalizer result-playback event-activity
            motion-finish timeline-navigation
            """, "motion", 100)) yield return id;
        foreach (string id in Expand("""
            inspector-bounded ultron-inspection boost-choice decision-catalogue minion-order ability-choice
            deferred-event repeated-commit weighted-gesture loop-drag tooltip-action-polish hand-intent
            discard-preview recipient-commitment layout-continuity result-playback event-activity
            motion-finish timeline-navigation
            """, "reduced", 150)) yield return id;
        foreach (string id in Expand("ability-window ability-window-pass forced-ability ability-window-long-cause-layout staged-response", "motion", 100, 150)) yield return id;
        foreach (string id in Expand("search-choice shuri-search ancestral-knowledge", "motion", 50, 80, 100, 150)) yield return id;
        yield return "motion-finish-150-motion";
        foreach (string id in Expand("local-game", "motion", 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150)) yield return id;
        yield return "local-game-100-reduced";
        foreach (string id in Expand("local-game", "two-player", 100, 120, 150)) yield return id;
    }

    private static IEnumerable<string> Expand(string names, string suffix, params int[] scales)
    {
        foreach (string name in names.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        foreach (int scale in scales)
            yield return $"{name}-{scale}-{suffix}";
    }
}
