namespace Marvel.View;

/// <summary>Authorized participants of an attack still resolving, including its response windows.</summary>
public sealed record ActiveAttackDescriptor(ActivitySubject Source, ActivitySubject Target);
