from . import *

@final
class Effects:

    @staticmethod
    def UnRegister(*effects: 'Effect'|List['Effect']|None) -> None:
        def unregister(effects: 'Effect'|List['Effect']|None):
            if effects == None:
                pass
            elif isinstance(effects, list):
                for effect in effects:
                    effect.UnRegisterSelf()
            else:
                effects.UnRegisterSelf()

        for effect in effects:
            unregister(effect)

