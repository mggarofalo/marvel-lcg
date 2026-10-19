namespace Marvel.Desktop;

/// <summary>Chooses the explicit embedded save root or the normal per-user location.</summary>
internal static class DesktopSaveDirectory
{
    internal static string Resolve(string? configured = null)
    {
        configured ??= Environment.GetEnvironmentVariable("MARVEL_SAVE_DIRECTORY");
        try
        {
            return Path.GetFullPath(string.IsNullOrEmpty(configured)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarvelLCG", "Saves")
                : configured);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new DesktopStorageException(error);
        }
    }
}
