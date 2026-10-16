using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Describes the engine-selected candidates for a revealed attachment.</summary>
internal static class AttachmentTargetPrompt
{
    internal static Prompt Describe(
        World world, ICardFacts facts, PhaseStep step)
    {
        var card = world.Cards[step.Subject];
        var candidates = step.Require<AttachmentTargetOperation>().Candidates ?? [];
        return new Prompt(
            world.FirstPlayer,
            Question.Element,
            TimingPriority.Untimed,
            Steps.ChooseAttachmentTarget,
            $"{world.Seats[world.FirstPlayer].Name} chooses where {card.FaceId} attaches",
            false,
            [.. candidates.Select(id => new Affordance(
                id,
                "Attach",
                id,
                world.Cards[id].Area.PlayArea.IsPlayers
                    ? world.Cards[id].Area.PlayArea.Player
                    : -1,
                facts.Title(world.Cards[id].FaceId)))]);
    }
}
