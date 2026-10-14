using Godot;

namespace Marvel.Godot;

/// <summary>Keeps current decision prose on the ability sheet while retaining the table's causal cards.</summary>
internal static class AbilitySituationOwnership
{
    private const string Owners = "ability_situation_owners";

    internal static void Change(Main main, int delta)
    {
        main.SetMeta(Owners, Math.Max(0, main.GetMeta(Owners, 0).AsInt32() + delta));
        foreach (Control situation in main.FindChildren("CausalSituationCopy", "Control", true, false))
            Apply(main, situation);
    }

    internal static void Apply(Main main, Control situation) =>
        situation.Visible = main.GetMeta(Owners, 0).AsInt32() == 0
            && !main.GetMeta("response_staged", false).AsBool();
}
