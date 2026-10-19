using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
namespace Marvel.Server.Tests;

/// <summary>Chooses an offered setup card and declines optional recovery for controlled campaign fixtures.</summary>
internal static class CampaignSetupAnswers
{
    internal static Decision Next(Prompt prompt)
    {
        if (prompt.Trigger == "CampaignRecovery") return Decision.Decline;
        var offered = prompt.Affordances.First(option => option.IsLegal);
        int[] targets = offered.Targets is { } request ? [.. request.Legal.Take(request.Min)] : [];
        return new(offered.Id, targets);
    }
}
