using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>One generated icon explicitly assigned by the local player.</summary>
public readonly record struct ResourceIconAssignment(
    int Source,
    int Icon,
    int Cost,
    char PaidAs);
