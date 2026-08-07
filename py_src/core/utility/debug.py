
class Debug:

    @staticmethod
    def IsVSDebug() -> bool:
        # https://stackoverflow.com/questions/38634988/check-if-program-runs-in-debug-mode
        import sys
        return hasattr(sys, 'gettrace') and sys.gettrace() is not None

    @staticmethod
    def DebugBreak(pause_skip: bool = True):
        from engine import Engine
        from build import Build
        if not Build.release:
            if pause_skip:
                if Engine.game.controller_manager.skip.SetIsSkipping(False):
                    if Engine.game.world:
                        Engine.game.world.render.PresentForceNoWait()
            breakpoint()

