namespace Marvel.Client;

/// <summary>How an operation affects the sole draft bound to the displayed prompt.</summary>
public enum ClientDraftDisposition
{
    /// <summary>Retain the draft and its current submission lock.</summary>
    Preserve,
    /// <summary>The mutation was not sent; retain the draft and allow an explicit retry.</summary>
    Retry,
    /// <summary>Replace the draft from the newly authoritative prompt, even at the same revision.</summary>
    Replace,
    /// <summary>The session is unavailable; discard its draft.</summary>
    Clear,
}
