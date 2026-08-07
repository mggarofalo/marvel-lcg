class EngineIntegrityError(Exception):
    """The run has already produced something that must not be trusted.

    The engine deliberately absorbs exceptions during play: a card script that
    raises should not take the whole game down, so `EffectInvoker`,
    `Message2.Send`, the cost and target checkers, and `Engine.EngineRun` all
    catch broadly, report through `Log.OnCrash`, and carry on. `Log.OnCrash`
    re-raises only when `Build.release` is false, and `build.py` hardcodes it
    true -- so in every real run those handlers swallow.

    That is the right behaviour for a bug in one card. It is the wrong
    behaviour for a failure that means the recorded output is already corrupt,
    because carrying on turns a loud failure into a corpus file that looks
    clean. `Log.OnCrash` re-raises anything deriving from this class
    regardless of the build, so a subclass cannot be silently absorbed by a
    handler that was written to protect against something else.

    Subclass this only for failures where continuing would produce a wrong
    artefact rather than a wrong frame. See MARVEL-32.
    """
