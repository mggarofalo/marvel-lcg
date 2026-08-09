class Build:
    # Hardcoded on purpose, and the decision is MARVEL-28's.
    #
    # `release` is read at around sixty sites and controls far more than log
    # formatting: `Log.OnCrash` re-raises only when it is false, the editor only
    # initialises when it is false, `Debug`/`Hack`/`DebugSilent` become no-ops,
    # and `Ver.ui_version_str` gains the `r`/`d` suffix every API route's
    # `app_version` cookie is checked against. The headless bot's crash capture
    # (MARVEL-12) exists *because* a release build absorbs -- flipping this
    # would turn every absorbed card bug into a run-ending exception and change
    # what a corpus run means.
    #
    # The line above this one used to read `release = "RELEASE" in os.environ`,
    # which looked like an override and was not: this assignment overwrote it
    # unconditionally. It is gone rather than honoured, so nothing reads a
    # switch that has never worked. Nothing in the replay verification path
    # depends on this flag either way -- see `game/test/verify.py`.
    release = True

    # Version
    MAJOR = 0
    MINOR = 5
    PATCH = 9
    BUILD = 205
