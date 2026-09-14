using Marvel.Decisions;

namespace Marvel.Godot;

/// <summary>
/// Fences one table interaction source to its prompt render before mutating its draft.
/// </summary>
internal sealed class TableDraftBinding
{
    private readonly TableDraftOperations operations;
    private readonly int generation;
    private readonly long revision;
    private readonly Func<int, long, bool> isCurrent;

    internal TableDraftBinding(
        DecisionComposer composer,
        int generation,
        long revision,
        Func<int, long, bool> isCurrent)
    {
        operations = new TableDraftOperations(composer);
        this.generation = generation;
        this.revision = revision;
        this.isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
    }

    internal bool TrySelectAffordance(int id) =>
        IsCurrent() && operations.TrySelectAffordance(id);

    internal bool TrySelectGroup(IReadOnlyList<int> targets) =>
        IsCurrent() && operations.TrySelectGroup(targets);

    internal bool TryToggleTarget(int id) =>
        IsCurrent() && operations.TryToggleTarget(id);

    internal bool TryAddTarget(int id) =>
        IsCurrent() && operations.TryAddTarget(id);

    internal bool TryRemoveTarget(int id) =>
        IsCurrent() && operations.TryRemoveTarget(id);

    internal bool TrySelectCost(int index) =>
        IsCurrent() && operations.TrySelectCost(index);

    internal bool TryToggleGenerator(int effect) =>
        IsCurrent() && operations.TryToggleGenerator(effect);

    private bool IsCurrent() => isCurrent(generation, revision);
}
