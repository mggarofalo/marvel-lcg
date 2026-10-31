using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Locates the live host whose attachments may carry to a projected stage.</summary>
internal static class AbilityProjectedAttachmentHosts
{
    internal static int BoardVillain(Card stage, AbilityAdmissionScope cast)
    {
        if (cast.World.EncounterTopology is not { } topology)
            return EncounterRouting.Villain(cast.World, cast.Source)?.ObjectId ?? -1;
        return topology.Groups.SingleOrDefault(group => group.Stages.Contains(stage))
            ?.CurrentVillain?.ObjectId ?? -1;
    }
}
