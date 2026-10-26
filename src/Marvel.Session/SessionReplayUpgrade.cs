namespace Marvel.Session;

/// <summary>Release-owned replay transitions available only after named source admission.</summary>
public enum SessionReplayUpgrade
{
    /// <summary>The replay contract is unchanged.</summary>
    Unchanged,
    /// <summary>Verify the complete v19 trace under v20 without changing any expected gameplay fact.</summary>
    CoreRorsV19ToV20,
    /// <summary>Verify the complete v19 trace under v21 without replacing historical outcomes.</summary>
    CoreRorsV19ToV21,
    /// <summary>Verify the complete v20 trace under v21 without replacing historical outcomes.</summary>
    CoreRorsV20ToV21,
    /// <summary>Verify the complete v19 trace under v22 without replacing historical outcomes.</summary>
    CoreRorsV19ToV22,
    /// <summary>Verify the complete v20 trace under v22 without replacing historical outcomes.</summary>
    CoreRorsV20ToV22,
    /// <summary>Verify the complete v21 trace under v22 without replacing historical outcomes.</summary>
    CoreRorsV21ToV22,
    /// <summary>Verify the complete v19 trace under v23 without replacing historical outcomes.</summary>
    CoreRorsV19ToV23,
    /// <summary>Verify the complete v20 trace under v23 without replacing historical outcomes.</summary>
    CoreRorsV20ToV23,
    /// <summary>Verify the complete v21 trace under v23 without replacing historical outcomes.</summary>
    CoreRorsV21ToV23,
    /// <summary>Verify the complete v19 trace under v24 without replacing historical outcomes.</summary>
    CoreRorsV19ToV24,
    /// <summary>Verify the complete v20 trace under v24 without replacing historical outcomes.</summary>
    CoreRorsV20ToV24,
    /// <summary>Verify the complete v21 trace under v24 without replacing historical outcomes.</summary>
    CoreRorsV21ToV24,
}
