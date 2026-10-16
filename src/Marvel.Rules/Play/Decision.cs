namespace Marvel.Rules.Play;

/// <summary>
/// One answer to one prompt. The engine's input.
/// </summary>
/// <param name="Affordance">
/// Which of the prompt's affordances is being taken, by
/// <c>Affordance.Id</c>, or <c>-1</c> to decline.
/// </param>
/// <param name="Targets">
/// The objects chosen for it, in the order they were chosen. Empty when the
/// affordance takes no targets, and empty on a decline.
/// </param>
/// <param name="Resources">
/// The generators spent to pay for it, by <c>ResourceSource.Effect</c>. Empty
/// when the affordance is free, and empty on a decline.
/// </param>
/// <param name="Values">
/// Numerical variables defined while initiating the affordance, keyed by the
/// printed variable name. Empty when the affordance asks for none.
/// </param>
/// <param name="Allocations">
/// Individual generated icons assigned to resource-cost components. Empty
/// when no rule can distinguish the allocation.
/// </param>
/// <remarks>
/// Handles are valid only for their issuing prompt. Targets retain player order;
/// payment names generators and the allocation of their icons to cost components.
/// The engine chooses a negative handle to represent decline. A decline carries
/// no selections or payment and is accepted only by a cancellable prompt.
/// </remarks>
public sealed record Decision(
    int Affordance,
    IReadOnlyList<int> Targets,
    IReadOnlyList<int>? Resources = null,
    IReadOnlyDictionary<string, long>? Values = null,
    IReadOnlyList<ResourceAllocation>? Allocations = null)
{
    /// <summary>Take nothing. The engine's empty command.</summary>
    public static Decision Decline => new(-1, []);

    /// <summary>Whether this answer takes no affordance.</summary>
    public bool IsDecline => Affordance < 0;

    /// <summary>The generators spent, never null.</summary>
    public IReadOnlyList<int> Spent => Resources ?? [];

    /// <summary>The numerical variables explicitly defined by this answer.</summary>
    public IReadOnlyDictionary<string, long> DefinedValues => Values
        ?? EmptyValues;

    /// <summary>The explicit paid-icon allocation, never null.</summary>
    public IReadOnlyList<ResourceAllocation> Allocated => Allocations ?? [];

    private static IReadOnlyDictionary<string, long> EmptyValues { get; } =
        System.Collections.Immutable.ImmutableDictionary<string, long>.Empty;

    /// <summary>Takes an affordance with no targets.</summary>
    /// <param name="affordance">The affordance's id, from the prompt that offered it.</param>
    public static Decision Take(int affordance) => new(affordance, []);

    /// <summary>Takes an affordance, paying for it with these generators.</summary>
    /// <param name="affordance">The affordance's id, from the prompt that offered it.</param>
    /// <param name="targets">The objects chosen for it, in order.</param>
    /// <param name="paying">
    /// The generators spent, by <c>ResourceSource.Effect</c>. Order is not kept:
    /// <c>rr:modifiers.2</c> treats a cost's modifiers as simultaneous, and
    /// nothing in <c>rr:initiating-abilities</c> step 5 makes the order of
    /// payment observable.
    /// </param>
    public static Decision Take(
        int affordance, IReadOnlyList<int> targets, IReadOnlyList<int> paying) =>
        new(affordance, targets, paying);

    /// <summary>Takes an affordance with explicit numerical variable values.</summary>
    public static Decision Take(
        int affordance, IReadOnlyList<int> targets, IReadOnlyList<int> paying,
        IReadOnlyDictionary<string, long> values) =>
        new(affordance, targets, paying, values);

    /// <summary>Takes an affordance with variable values and paid-icon allocation.</summary>
    public static Decision Take(
        int affordance, IReadOnlyList<int> targets, IReadOnlyList<int> paying,
        IReadOnlyDictionary<string, long> values,
        IReadOnlyList<ResourceAllocation> allocations) =>
        new(affordance, targets, paying, values, allocations);
}
