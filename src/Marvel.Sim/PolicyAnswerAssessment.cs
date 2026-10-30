using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;

namespace Marvel.Sim;

/// <summary>Holds a policy's answer to the engine's own assessment before it is resolved.</summary>
/// <remarks>
/// A rejected answer is the policy's failure, not the engine's, so it is raised
/// while the run is still in its policy stage and is recorded as <c>policy_error</c>.
/// </remarks>
internal static class PolicyAnswerAssessment
{
    internal static void Require(Prompt asked, Decision answer)
    {
        int actor = DurableDecision.SimulationActor(asked, answer);
        DecisionAssessmentResult result = DecisionAssessment.Assess(asked, answer, actor);
        if (!result.IsAccepted)
        {
            throw new SimulationRunException(
                $"policy answer rejected: {result.Reason ?? result.Rejection.ToString()}");
        }
    }
}
