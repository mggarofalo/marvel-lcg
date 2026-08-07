from . import *

class CanPlaceToken(CardFace):

    ################################################################################
    # Counter
    @final
    def GetAllTokens(self) -> int:
        return self.components.token.GetAllTokens()
    @final
    def SetTokens(self, counters: int, name: 'CardFace.TOKEN', by_effect: 'Effect'):
        # name = Cast(CardFace.TOKEN, name)
        return self.components.token.SetTokens(counters, name)
    @final
    def GetTokens(self, name: 'CardFace.TOKEN') -> int:
        # name = Cast(CardFace.TOKEN, name)
        return self.components.token.GetTokens(name)

    def PlaceTokensInternal(self, value: INT_TYPE, name: 'CardFace.TOKEN', by_effect: 'Effect') -> int|None:
        from game.message import Message

        from game.operate.worlds import Worlds
        value = Worlds.ConvertPerPlayerIconToInt(value, by_effect)

        would_message = Message.WhenCardWouldBePlacedToken(self, value, name, by_effect)
        would_message.Send()
        if would_message.is_be_instead:
            return None

        value = would_message.num

        self.components.token.PlaceTokens(value, name)

        placed_message = Message.AfterCardPlacedToken(self, value, name, by_effect, would_message)
        placed_message.Send()
        return placed_message.num

    # When only has 1 token, but want to remove 2
    # if force == True
    #   remove all token and return `removed_num`
    # else:
    #   do nothing and return None
    def RemoveTokensInternal(self, value: Literal["All"]|INT_TYPE, name: 'CardFace.TOKEN', by_effect: 'Effect', forced: bool|None=None) -> 'int|None':
        counter_num = self.components.token.GetTokens(name)
        if value != "All":
            from game.operate.worlds import Worlds
            value = Worlds.ConvertPerPlayerIconToInt(value, by_effect)
        if forced == None:
            # assert value == "All" or value == 1
            forced = True

        would_message = Message.WhenCardWouldRemovedToken(self, value, name, by_effect)
        would_message.Send()
        if would_message.is_be_instead:
            return None

        if would_message.remove_all:
            value = "All"
        else:
            assert would_message.would_remove
            value = would_message.would_remove

        if self.components.token.RemoveTokens(value, name, forced):
            diff = counter_num - self.components.token.GetTokens(name)
            after_message = Message.AfterCardRemovedToken(self, diff, would_message)
            after_message.Send()
            return after_message.removed_num
        else:
            return None

    @final
    def RemoveAllTokens(self, by_effect: 'Effect') -> int:
        size = 0
        for name in self.components.token.GetTokenNames():
            num = self.RemoveTokensInternal(Math.INT_INF, name, by_effect, forced=True)
            if num:
                size += num
        return size

    @override
    def GetInfoDict(self) -> Dict[str, int]:
        the_dict: Dict[str, int] = {}
        for key in self.components.token.GetTokenNames():
            token = self.GetTokens(key)
            key_name = 'k_' + key
            the_dict |= {key_name: token}
        return the_dict | super().GetInfoDict()

    # @override
    # def OnAfterCardLeavePlay(self, message: 'Message.AfterCardLeavePlay') -> None:
    #     from game.ability.rule import GameRule
    #     rule = GameRule(self)
    #     self.RemoveAllTokens(rule)
    #     return super().OnAfterCardLeavePlay(message)

