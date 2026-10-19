namespace Marvel.Desktop;

/// <summary>Separates durable storage admission failures from content loading failures.</summary>
internal sealed class DesktopStorageException(Exception inner) : Exception("Embedded save storage is unavailable.", inner);
