using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>
/// Which seats took turns, paid and played cards, how often a basic power was divided, and how often
/// an additional attack cost was paid with resources.
/// </summary>
internal sealed class SeatParticipation
{
    internal HashSet<int> Acting { get; } = [];
    internal HashSet<int> Paid { get; } = [];
    internal HashSet<int> Played { get; } = [];
    internal int Divisions { get; private set; }
    internal int ResourceAttackCosts { get; private set; }

    internal void Record(Prompt prompt, Decision answer)
    {
        if (prompt.Asking == Question.TurnOption) Acting.Add(prompt.Player);
        if (answer.Spent.Count > 0) Paid.Add(prompt.Player);
        var chosen = prompt.Affordances.FirstOrDefault(offer => offer.Id == answer.Affordance);
        if (chosen?.PlaysCard == true) Played.Add(prompt.Player);
        if (chosen?.Verb == BasicPowerDivision.Verb) Divisions++;
        if (chosen?.Verb == BasicAttackCosts.PayVerb && chosen.CostOptions.Count > 0) ResourceAttackCosts++;
    }
}
