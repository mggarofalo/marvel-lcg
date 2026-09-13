namespace Marvel.Rules.Play;

/// <summary>Icons from one generator assigned to one simultaneous resource cost.</summary>
/// <param name="Source">The <c>ResourceSource.Effect</c> that generated them.</param>
/// <param name="Cost">Zero-based component in <c>CostOption.ResourceCosts</c>.</param>
/// <param name="PaidAs">
/// One letter per paid icon. A wild carries the type the player declared for
/// it; generated excess is omitted because it was not paid for the cost.
/// </param>
public readonly record struct ResourceAllocation(int Source, int Cost, string PaidAs);
