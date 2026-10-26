namespace Marvel.Rules.Timing;

/// <summary>Tracks exact ability uses and physical event-play generations within one occurrence.</summary>
internal sealed class TriggerUses
{
    private readonly HashSet<(WindowKind Window, int Card, AbilityType Type, int Ordinal,
        State.Defeated? Defeat, State.Attacked? Attack, NativeAbilityKind Native,
        Occurrence? Threat, int EventPlay)> spent = [];
    private readonly Dictionary<int, int> completedEventPlays = [];

    internal int EventPlay(int card) => completedEventPlays.GetValueOrDefault(card);

    internal void CompleteEventPlay(int card) => completedEventPlays[card] = checked(EventPlay(card) + 1);

    // rr:triggering-condition.1-.1.1: each interrupt and response may be triggered
    // once per occurrence; multiple copies may each trigger. Distinct recipient
    // facts and successive plays of one physical event retain independent uses.
    internal bool MayTrigger(WindowKind window, PendingAbility ability) =>
        !spent.Contains((window, ability.Card, ability.Type, ability.Ordinal, ability.TriggeringDefeat,
            ability.TriggeringAttack, ability.Native, ability.TriggeringThreat, ability.EventPlay));

    internal bool Trigger(WindowKind window, PendingAbility ability) =>
        spent.Add((window, ability.Card, ability.Type, ability.Ordinal, ability.TriggeringDefeat,
            ability.TriggeringAttack, ability.Native, ability.TriggeringThreat, ability.EventPlay));
}
