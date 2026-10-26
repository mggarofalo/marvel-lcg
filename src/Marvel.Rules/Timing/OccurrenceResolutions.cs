namespace Marvel.Rules.Timing;

/// <summary>Owns ability resolution lifecycles and their containing card's aggregate status.</summary>
public sealed class OccurrenceResolutions
{
    private readonly List<ResolutionEntry> resolutions;

    internal OccurrenceResolutions(IReadOnlyList<ResolutionEntry>? entries) => resolutions = [.. entries ?? []];

    /// <summary>Ordered, immutable entries describing each tracked resolution.</summary>
    public IReadOnlyList<ResolutionEntry> Entries => resolutions;

    /// <summary>Begin resolving one exact triggered ability.</summary>
    public void Begin(PendingAbility ability)
    {
        int found = ResolutionIndex(ability);
        if (found < 0)
        {
            resolutions.Add(new ResolutionEntry(
                ability.Card, ability.Type, ability.Ordinal,
                ability.Type == AbilityType.Constant
                    ? ResolutionStatus.Unresolved
                    : ResolutionStatus.Pending) { TriggeringDefeat = ability.TriggeringDefeat, TriggeringAttack = ability.TriggeringAttack, TriggeringThreatId = ability.TriggeringThreat?.Id, Native = ability.Native, EventPlay = ability.EventPlay });
        }
    }

    /// <summary>Begin resolving an event or treachery and all of its abilities.</summary>
    public void BeginCard(int card, IReadOnlyList<PendingAbility> abilities)
    {
        int eventPlay = abilities.Count > 0 ? abilities[0].EventPlay : 0;
        if (resolutions.All(entry => entry.Card != card || entry.Ability is not null || entry.EventPlay != eventPlay))
        {
            resolutions.Add(new ResolutionEntry(
                card, null, -1, ResolutionStatus.Pending) { EventPlay = eventPlay });
        }
        foreach (var ability in abilities)
        {
            Begin(ability);
        }
        RefreshCard(card, eventPlay);
    }

    /// <summary>Record that at least one effect applied while the ability remains in progress.</summary>
    public void Resolve(PendingAbility ability)
    {
        int found = ResolutionIndex(ability);
        if (found < 0)
        {
            Begin(ability);
            found = ResolutionIndex(ability);
        }
        if (ability.Type != AbilityType.Constant)
        {
            resolutions[found] = resolutions[found] with
            {
                Applied = true,
                Status = resolutions[found].Status == ResolutionStatus.Unresolved
                    ? ResolutionStatus.Resolved
                    : resolutions[found].Status,
            };
            RefreshCard(ability.Card, ability.EventPlay);
        }
    }

    /// <summary>Finish an ability whose effects did or did not apply.</summary>
    public void Complete(PendingAbility ability)
    {
        int found = ResolutionIndex(ability);
        if (found < 0)
        {
            Begin(ability);
            found = ResolutionIndex(ability);
        }
        if (resolutions[found].Status == ResolutionStatus.Pending)
        {
            resolutions[found] = resolutions[found] with
            {
                Status = resolutions[found].Applied
                    ? ResolutionStatus.Resolved
                    : ResolutionStatus.Unresolved,
            };
        }
        RefreshCard(ability.Card, ability.EventPlay);
    }

    /// <summary>Cancel an ability before any of its effects resolve.</summary>
    public void Cancel(PendingAbility ability)
    {
        Set(ability, ResolutionStatus.Unresolved, applied: false);
        RefreshCard(ability.Card, ability.EventPlay);
    }

    /// <summary>The current status of an exact ability; absent abilities are unresolved.</summary>
    public ResolutionStatus StatusOf(PendingAbility ability)
    {
        int found = ResolutionIndex(ability);
        return found < 0 ? ResolutionStatus.Unresolved : resolutions[found].Status;
    }

    /// <summary>The current status of a card; absent cards are unresolved.</summary>
    public ResolutionStatus CardStatus(int card) => resolutions.LastOrDefault(entry =>
        entry.Card == card && entry.Ability is null)?.Status
        ?? ResolutionStatus.Unresolved;

    private int ResolutionIndex(PendingAbility ability) => resolutions.FindIndex(entry =>
        entry.Card == ability.Card
        && entry.Ability == ability.Type
        && entry.Ordinal == ability.Ordinal
        && entry.EventPlay == ability.EventPlay
        && entry.Native == ability.Native
        && entry.TriggeringDefeat == ability.TriggeringDefeat
        && entry.TriggeringAttack == ability.TriggeringAttack
        && entry.TriggeringThreatId == ability.TriggeringThreat?.Id);

    private void Set(
        PendingAbility ability, ResolutionStatus status, bool applied = false)
    {
        if (ability.Type == AbilityType.Constant)
        {
            status = ResolutionStatus.Unresolved;
        }
        int found = ResolutionIndex(ability);
        if (found < 0)
        {
            resolutions.Add(new ResolutionEntry(
                ability.Card, ability.Type, ability.Ordinal, status, applied)
                { TriggeringDefeat = ability.TriggeringDefeat, TriggeringAttack = ability.TriggeringAttack, TriggeringThreatId = ability.TriggeringThreat?.Id, Native = ability.Native, EventPlay = ability.EventPlay });
        }
        else
        {
            resolutions[found] = resolutions[found] with
            {
                Status = status,
                Applied = applied,
            };
        }
    }

    private void RefreshCard(int card, int eventPlay)
    {
        int cardEntry = resolutions.FindIndex(entry =>
            entry.Card == card && entry.Ability is null && entry.EventPlay == eventPlay);
        if (cardEntry < 0)
        {
            return;
        }

        var abilities = resolutions.Where(entry =>
            entry.Card == card && entry.Ability is not null && entry.EventPlay == eventPlay).ToList();
        var status = abilities.Any(entry => entry.Status == ResolutionStatus.Resolved)
            ? ResolutionStatus.Resolved
            : abilities.Any(entry => entry.Status == ResolutionStatus.Pending)
                ? ResolutionStatus.Pending
                : ResolutionStatus.Unresolved;
        resolutions[cardEntry] = resolutions[cardEntry] with { Status = status };
    }

}
