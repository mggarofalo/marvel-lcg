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
    /// <summary>Verify the complete v19 trace under v25 without replacing historical outcomes.</summary>
    CoreRorsV19ToV25,
    /// <summary>Verify the complete v20 trace under v25 without replacing historical outcomes.</summary>
    CoreRorsV20ToV25,
    /// <summary>Verify the complete v21 trace under v25 without replacing historical outcomes.</summary>
    CoreRorsV21ToV25,
    /// <summary>Verify the complete v19 trace under v26 without replacing historical outcomes.</summary>
    CoreRorsV19ToV26,
    /// <summary>Verify the complete v20 trace under v26 without replacing historical outcomes.</summary>
    CoreRorsV20ToV26,
    /// <summary>Verify the complete v21 trace under v26 without replacing historical outcomes.</summary>
    CoreRorsV21ToV26,
    /// <summary>Verify the complete v19 trace under v27 without replacing historical outcomes.</summary>
    CoreRorsV19ToV27,
    /// <summary>Verify the complete v20 trace under v27 without replacing historical outcomes.</summary>
    CoreRorsV20ToV27,
    /// <summary>Verify the complete v21 trace under v27 without replacing historical outcomes.</summary>
    CoreRorsV21ToV27,
    /// <summary>Verify the complete v19 trace under v28 without replacing historical outcomes.</summary>
    CoreRorsV19ToV28,
    /// <summary>Verify the complete v20 trace under v28 without replacing historical outcomes.</summary>
    CoreRorsV20ToV28,
    /// <summary>Verify the complete v21 trace under v28 without replacing historical outcomes.</summary>
    CoreRorsV21ToV28,
    /// <summary>Verify the complete v19 trace under v29 without replacing historical outcomes.</summary>
    CoreRorsV19ToV29,
    /// <summary>Verify the complete v20 trace under v29 without replacing historical outcomes.</summary>
    CoreRorsV20ToV29,
    /// <summary>Verify the complete v21 trace under v29 without replacing historical outcomes.</summary>
    CoreRorsV21ToV29,
}
