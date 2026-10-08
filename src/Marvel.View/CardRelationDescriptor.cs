namespace Marvel.View;

/// <summary>Explicit Attached, Controlled, or Shared relationship. An unreadable host has no link.</summary>
public sealed record CardRelationDescriptor(string Kind, int? HostId, int? Controller);
