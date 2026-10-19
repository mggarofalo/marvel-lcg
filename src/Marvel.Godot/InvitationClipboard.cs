namespace Marvel.Godot;

/// <summary>Retires a one-time invitation only after the clipboard confirms the copy.</summary>
internal static class InvitationClipboard
{
    internal static bool TryCopy(ref string? invitation, Action<string> write, Func<string> read)
    {
        if (invitation is null) return false;
        write(invitation);
        if (!string.Equals(read(), invitation, StringComparison.Ordinal)) return false;
        invitation = null;
        return true;
    }
}
