using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Whether a thwart group's performer may thwart before the group is offered.</summary>
/// <remarks>
/// A thwart group schedules thwarts by its performer, as a "Hero Action (thwart)"
/// does. rr:cannot: "The word 'cannot' is absolute and cannot be countermanded by other
/// abilities or effects."
/// A performer that cannot thwart makes the group unplayable, so the prohibition
/// is checked before any cost is paid.
/// </remarks>
internal static class AbilityThwartGroupAdmission
{
    internal static bool PerformerCanThwart(AbilityAdmission.AbilityAdmissionScope cast) =>
        ThwartPermissions.CanThwart(cast.World, LabeledAbilities.Performer(cast.World, cast.World.Facts,
            AbilityCardQueries.Resolver(cast.Context.Query), cast.Source));
}
