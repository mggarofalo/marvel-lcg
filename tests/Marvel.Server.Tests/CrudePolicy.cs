using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Server.Tests;

/// <summary>A crude, deterministic policy that keeps a hosted game moving.</summary>
internal static class CrudePolicy
{
    /// <summary>Candidate answers in the order the policy tries them.</summary>
    internal static IEnumerable<Decision> Candidates(Prompt prompt)
    {
        foreach (Affordance card in prompt.Affordances.Where(offer =>
                     offer.Verb == CardPlay.Verb && offer.CostOptions.Count > 0))
        {
            if (Payment(card) is { } paying
                && ResourcePayment.Allocate(card.CostOptions[0], paying) is { } allocation)
            {
                yield return Decision.Take(card.Id,
                    card.Targets is { Legal.Count: > 0 } targets ? [targets.Legal[0]] : [],
                    paying, new Dictionary<string, long>(), allocation);
            }
        }

        if (prompt.Asking == Question.Defender)
            yield return Decision.Take(prompt.Affordances[^1].Id);
        foreach (string verb in new[] { BasicPowers.ThwartVerb, BasicPowers.AttackVerb, Game.ChangeForm, Game.EndPhaseVerb })
        {
            foreach (Affordance offer in prompt.Affordances.Where(offer => offer.Verb == verb))
                yield return Answer(prompt, offer);
        }

        if (prompt.Cancellable)
            yield return Decision.Decline;
        foreach (Affordance offer in prompt.Affordances.Where(offer => offer.Verb != CardPlay.Verb))
            yield return Answer(prompt, offer);
    }

    private static Decision Answer(Prompt prompt, Affordance offer)
    {
        if (offer.Targets is not { } targets)
            return Decision.Take(offer.Id);
        if (prompt.Asking == Question.Order)
            return new Decision(offer.Id, [.. targets.Legal]);
        int count = offer.Verb == Game.EndPhaseVerb
            ? targets.Min
            : Math.Max(targets.Min, Math.Min(1, targets.Legal.Count));
        return new Decision(offer.Id, [.. targets.Legal.Take(count)]);
    }

    private static int[]? Payment(Affordance offer)
    {
        if (!long.TryParse(offer.CostOptions[0].Cost,
                System.Globalization.CultureInfo.InvariantCulture, out long cost))
            return null;
        var spent = new List<int>();
        long generated = 0;
        foreach (var source in offer.CostOptions[0].Generators)
        {
            if (generated >= cost)
                break;
            spent.Add(source.Effect);
            generated += source.Generates.Length;
        }

        return generated >= cost ? [.. spent] : null;
    }
}
