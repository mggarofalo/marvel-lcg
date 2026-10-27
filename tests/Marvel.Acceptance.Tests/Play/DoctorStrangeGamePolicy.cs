using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>Prioritizes Doctor Strange's offered abilities, with shared deterministic tactics.</summary>
internal sealed class DoctorStrangeGamePolicy(ICardFacts facts)
{
    private readonly CoreGamePolicy fallback = new(facts);
    internal int Answered { get; private set; }

    internal Decision Answer(Game game)
    {
        Answered++;
        var prompt = game.Pending!;
        var offers = prompt.Affordances.Where(option => option.IsLegal
            && option.AnchorId >= 0 && option.AnchorId < game.State.Cards.Count
            && GoblinPolicyPayment.Payment(option, game) is not null);
        var chosen = offers.FirstOrDefault(option => option.Timing is not null
            && game.State.Cards[option.AnchorId].FaceId.StartsWith("090", StringComparison.Ordinal));
        if (chosen is null) return fallback.Answer(game);
        var payment = GoblinPolicyPayment.Payment(chosen, game)!;
        var values = GoblinPolicyPayment.Values(chosen);
        return Decision.Take(chosen.Id, PolicyTargetSelection.Select(game.State, chosen), payment, values,
            GoblinPolicyPayment.Allocations(chosen, payment, values));
    }
}
