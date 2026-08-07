from . import *

class CanPlaceCounter(CardFace):

    # @override
    # def OnAfterCardLeavePlay(self, message: 'Message.AfterCardLeavePlay') -> None:
    #     # Fix "01018"
    #     # self.components.counter.counters = {}
    #     return super().OnAfterCardLeavePlay(message)

    ################################################################################
    # Counter
    @final
    def GetAllCounters(self) -> int:
        return self.components.counter.GetAllCounters()
    @final
    def SetCounters(self, counters: int, name: 'CardFace.COUNTER', by_effect: 'Effect'):
        # name = Cast(CardFace.COUNTER, name)
        return self.components.counter.SetCounters(counters, name)
    @final
    def GetCounters(self, name: 'CardFace.COUNTER|Literal["all-purpose"]') -> int:
        # name = Cast(CardFace.COUNTER, name)
        if name == 'all-purpose':
            return self.GetAllCounters()
        else:
            return self.components.counter.GetCounters(name)

    ################################################################################
    #
    def ConvertAllPurpose(self) -> 'CardFace.COUNTER':
        names = self.components.counter.GetCounterNames()
        if names:
            return names[0]
        else:
            return 'counter'

    def PlaceCountersInternal(self, num: int|Literal["1*", "2*", "3*", "6*"], name: 'CardFace.COUNTER|Literal["all-purpose"]', by_effect: 'Effect', *, maximum: int|None=None) -> int:
        from game.message import Message
        from game.operate.worlds import Worlds

        if name == "all-purpose":
            name = self.ConvertAllPurpose()

        num = Worlds.ConvertPerPlayerIconToInt(num, by_effect)

        would_message = Message.WhenCardWouldBePlacedCounter(self, num, name, by_effect)
        would_message.Send()
        if would_message.is_be_instead:
            return False
        num = would_message.counters

        maximum = would_message.maximum or maximum
        if maximum != None:
            if self.GetCounters(name) + num > maximum:
                num = maximum - self.GetCounters(name)

        self.components.counter.PlaceCounters(num, name)

        after_message = Message.AfterCardPlacedCounter(self, num, name, by_effect, would_message)
        after_message.Send()
        return num

    # When only has 1 counter, but want to remove 2
    # if force == True
    #   remove all counter and return `removed_num`
    # else:
    #   do nothing and return None
    def RemoveCountersInternal(self, counters: Literal["All"]|int, name: 'CardFace.COUNTER|Literal["all-purpose"]', by_effect: 'Effect', *, forced: bool|None=None) -> 'Message.AfterCardRemovedCounter|None':
        from game.message import Message
        if name == "all-purpose":
            name = self.ConvertAllPurpose()

        counter_num = self.components.counter.GetCounters(name)
        if forced == None:
            # assert counters == "All" or counters == 1
            forced = True

        would_message = Message.WhenCardWouldRemovedCounter(self, counters, name, by_effect)
        would_message.Send()
        if would_message.is_be_instead:
            return None

        if would_message.remove_all:
            counters = "All"
        else:
            assert would_message.counters != None and \
                would_message.counters >= 0 # Fix "16093"
            counters = would_message.counters

        if self.components.counter.RemoveCounters(counters, name, forced):
            diff = counter_num - self.components.counter.GetCounters(name)
            after_message = Message.AfterCardRemovedCounter(self, diff, would_message)
            after_message.Send()
            return after_message
        else:
            return None

    @final
    def RemoveAllCounters(self, by_effect: 'Effect'):
        for name in self.components.counter.GetCounterNames():
            self.RemoveCountersInternal(Math.INT_INF, name, by_effect, forced=True)

    @override
    def GetInfoDict(self) -> Dict[str, int]:
        the_dict: Dict[str, int] = {}
        for key in self.components.counter.GetCounterNames():
            counter = self.GetCounters(key)
            key_name = 'c_' + key
            the_dict |= {key_name: counter}
        return the_dict | super().GetInfoDict()

    # @override
    # def OnAfterCardLeavePlay(self, message: 'Message.AfterCardLeavePlay') -> None:
    #     from game.ability.rule import GameRule
    #     rule = GameRule(self)
    #     self.RemoveAllCounters(rule)
    #     return super().OnAfterCardLeavePlay(message)

