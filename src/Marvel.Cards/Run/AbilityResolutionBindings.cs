using Marvel.Rules.State;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

// Captured references and last-known quantities belong to one resolving copy.
internal sealed class AbilityResolutionBindings(Card source)
{
    private AbilityCardReference? chosenBinding;
    private AbilityCardReference? playerSelectionBinding;
    private int sourceIncarnation = source.Incarnation;
    private CardInstanceState sourceState = source.InstanceState;

    public IReadOnlyDictionary<string, long> CaptureSourceState(IReadOnlyDictionary<string, long> results) =>
        AbilitySourceStateFacts.Capture(results, sourceState);

    public void RestoreSourceState(CardInstanceState? restored)
    {
        if (restored is null) return;
        sourceState = source.Incarnation == sourceIncarnation && DeckTypes.IsInPlay(source.Area.Type)
            ? source.InstanceState : restored;
    }

    /// <summary>The card the player picked, once they have.</summary>
    public Card? Chosen => CurrentCard(chosenBinding, "chosen");

    /// <summary>The outer card selection used by chosen-player references.</summary>
    public Card? PlayerSelection =>
        CurrentCard(playerSelectionBinding, "player selection");

    public int SourceBindingIncarnation => sourceIncarnation;

    public void RestoreSourceIncarnation(int incarnation) =>
        sourceIncarnation = incarnation;

    /// <summary>Records the card a <c>chooseCard</c> was answered with.</summary>
    /// <param name="card">What they picked.</param>
    public void Choose(Card? card)
    {
        chosenBinding = Bind(card);
    }

    /// <summary>Records a player answer in both chosen namespaces.</summary>
    public void ChooseSelection(Card? card)
    {
        var binding = Bind(card);
        chosenBinding = binding;
        playerSelectionBinding = binding;
    }

    public AbilityCardReference? CaptureChosen() => chosenBinding;

    public AbilityCardReference? CapturePlayerSelection() => playerSelectionBinding;

    public AbilityCardReference? CaptureCurrentSelection() =>
        playerSelectionBinding ?? chosenBinding;

    public void RestoreChosen(AbilityCardReference? binding) => chosenBinding = binding;

    public void RestorePlayerSelection(AbilityCardReference? binding) =>
        playerSelectionBinding = binding;

    public void RestorePersistedSelection(
        Card card, int area, int incarnation, bool overwriteChosen)
    {
        var binding = new AbilityCardReference(card, area, incarnation);
        playerSelectionBinding = binding;
        if (overwriteChosen || chosenBinding is null)
        {
            chosenBinding = binding;
        }
    }

    public bool SourceBindingIsCurrent(Card card) =>
        source.ObjectId == card.ObjectId
        && sourceIncarnation == card.Incarnation;

    private static AbilityCardReference? Bind(Card? card) => card is null
        ? null
        : new AbilityCardReference(card, card.Area.Id, card.Incarnation);

    private Card? CurrentCard(AbilityCardReference? binding, string name) => binding?.Resolve(source, name);

    internal AbilityQueryContext QueryContext(AbilityResolutionState resolution) => new(
        resolution.World, source, resolution.Occurrence, resolution.Player, sourceIncarnation,
        chosenBinding, playerSelectionBinding, resolution.Altered, [.. resolution.PowerTargets])
        { SourceState = sourceState };
}
