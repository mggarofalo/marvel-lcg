namespace Marvel.Rules.Prompts;

/// <summary>
/// What an affordance costs against one target, and what can generate it.
/// </summary>
/// <param name="Target">
/// The object this price applies to, or <c>0</c> when the cost does not vary by
/// target.
/// </param>
/// <param name="Cost">The cost as printed, e.g. <c>"3"</c>.</param>
/// <param name="Rule">
/// The cost's requirements when it is not just a number — specific resource
/// types, in the quantities required.
/// </param>
/// <param name="OrCost">
/// An alternative reading of the same cost, e.g. "a mental resource <i>or</i>
/// two of any type". Empty when there is only one reading.
/// </param>
/// <param name="OrRule">The alternative's requirements.</param>
/// <param name="Sources">
/// Every source that can generate resources toward this cost.
/// </param>
/// <param name="Variables">
/// Values the player must define before paying this cost. The request is part
/// of the affordance because a cost of X cannot infer the player's choice from
/// whichever generators they later use.
/// </param>
/// <param name="Components">
/// The simultaneous resource costs represented by this one payment. Empty for
/// an ordinary single cost.
/// </param>
/// <remarks>
/// <para>
/// <b>Generation and payment are two things, and this record only describes the
/// first.</b> A player does not simply spend a cost; they generate resources —
/// incrementally, by discarding cards and using abilities — and then those
/// resources are consumed once to pay. <paramref name="Sources"/> is the menu of
/// generators. Which subset is used, and whether the total satisfies
/// <paramref name="Cost"/>, is the payment, and it is the player's decision
/// rather than something the engine can collapse in advance.
/// </para>
/// <para>
/// That distinction is not academic. Measured over the sampled options,
/// <b>22.1% of priced affordances offer five ways to pay and 21.6% offer six</b>;
/// only 7.3% offer exactly one. An interface that picked for the player would be
/// wrong most of the time, and one that modelled payment as a single number
/// could not express the choice at all. See the original investigation.
/// </para>
/// <para>
/// <paramref name="OrCost"/> is additive on purpose: <paramref name="Cost"/> and
/// <paramref name="Rule"/> keep describing the primary reading, so a reader that
/// ignores the alternative is merely conservative rather than wrong. A reader
/// planning a payment needs both — flattening an alternative cost to a bare
/// number is what the original investigation found breaking games, because the payer met the
/// number with resources of the wrong type and the ability failed
/// mid-resolution.
/// </para>
/// </remarks>
public sealed record CostOption(
    int Target,
    string Cost,
    IReadOnlyList<string>? Rule = null,
    string OrCost = "",
    IReadOnlyList<string>? OrRule = null,
    IReadOnlyList<ResourceSource>? Sources = null,
    IReadOnlyList<VariableRequest>? Variables = null,
    IReadOnlyList<ResourceCost>? Components = null)
{
    /// <summary>Whether the cost has a second legal reading.</summary>
    public bool HasAlternative => OrCost.Length > 0;

    /// <summary>The generators, or an empty list when there are none.</summary>
    public IReadOnlyList<ResourceSource> Generators => Sources ?? [];

    /// <summary>The variable values this cost asks the player to define.</summary>
    public IReadOnlyList<VariableRequest> VariableRequests => Variables ?? [];

    /// <summary>The resource-cost components sharing this payment.</summary>
    public IReadOnlyList<ResourceCost> ResourceCosts => Components
        ?? [new ResourceCost(Cost, Rule)];
}

/// <summary>One component of a simultaneous resource payment.</summary>
public sealed record ResourceCost(
    string Cost,
    IReadOnlyList<string>? Rule = null,
    bool Printed = false);

/// <summary>A numerical value the player must define while initiating a cost.</summary>
/// <param name="Name">The printed variable, such as <c>X</c>.</param>
/// <param name="Min">The smallest legal definition.</param>
/// <param name="Max">The largest legal definition on the current board.</param>
public readonly record struct VariableRequest(string Name, long Min, long Max)
{
    /// <summary>Whether a proposed definition is inside the offered range.</summary>
    public bool Allows(long value) => value >= Min && value <= Max;
}

/// <summary>Something that can generate resources toward a cost.</summary>
/// <param name="Effect">
/// The effect that generates. Handed back when the player chooses to use it.
/// </param>
/// <param name="Generates">
/// What it produces, as resource-type letters — one per resource, so a card
/// generating two of a type appears as two letters.
/// </param>
/// <remarks>
/// A generator is itself an effect, not a passive property of a card: using it
/// is a thing the player does, which is why it carries an id that can be handed
/// back in rather than a card reference.
/// </remarks>
public readonly record struct ResourceSource(int Effect, string Generates);
