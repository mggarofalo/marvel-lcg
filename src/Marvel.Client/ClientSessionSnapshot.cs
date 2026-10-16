using Marvel.Protocol;

namespace Marvel.Client;

/// <summary>One atomically remembered revision and its corresponding history offers.</summary>
internal sealed record ClientSessionSnapshot(long Revision, HistoryDescriptor? History);
