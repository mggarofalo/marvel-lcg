from . import *

class HasAccelerationIcon(HasAttribute):

    @override
    def __init__(self, paper: 'Paper') -> None:
        # For each side scheme in play with the acceleration icon,
        # one additional threat is placed on the main scheme
        # during step one of the villain phase
        self.printed_acceleration_icon = 0
        self.ignore_acceleration_icon = False

        super().__init__(paper)

        self.RegisterAttribute("Acceleration", "printed_acceleration_icon")
        self.RegisterInfoDict('acceleration_icon')

    ################################################################################
    #
    @final
    @property
    def acceleration_icon(self) -> int:
        return self.GetKeyword('Acceleration')

    ################################################################################
    #
    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.ignore_acceleration_icon = False
        self.GainAccelerationIcon(self.printed_acceleration_icon, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def GainAccelerationIcon(self, diff: int, by_effect: 'Effect'):
        self.GainKeyword(diff, 'Acceleration', by_effect)

    @final
    def SetIgnoreAccelerationIcon(self, diff: int, by_effect: 'Effect'):
        self.SetIgnoreKeyword(diff, 'Acceleration', by_effect)

