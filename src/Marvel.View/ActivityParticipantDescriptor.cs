namespace Marvel.View;

/// <summary>Authorized seat, actual quantity and visible subjects of an activity.</summary>
public sealed record ActivityParticipantDescriptor(
    int Player, string Name, int Count, IReadOnlyList<ActivitySubject> Subjects);
