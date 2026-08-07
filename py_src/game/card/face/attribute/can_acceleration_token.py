from . import *

class CanAccelerationToken(CanPlaceToken):

    @override
    def OnAfterCardLeavePlay(self, message: 'Message.AfterCardLeavePlay') -> None:
        self.RemoveTokensInternal("All", 'acceleration_token', message.by_effect)
        return super().OnAfterCardLeavePlay(message)

    ################################################################################
    #
    @final
    @property
    def acceleration_token(self) -> int:
        return self.GetTokens('acceleration_token')

    ################################################################################
    #
    @final
    def PlaceAccelerationToken(self, num: int, by_effect: 'Effect') -> int|None:
        return self.PlaceTokensInternal(num, 'acceleration_token', by_effect)

