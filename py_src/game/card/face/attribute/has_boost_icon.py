from . import *

class HasBoostIcon(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_boost = 0

        super().__init__(paper)

        self.RegisterAttribute("Boost", "printed_boost")
        self.RegisterInfoDict('boost_const')

    ################################################################################
    #
    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainKeywordBoostInternal(self.printed_boost, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def GainKeywordBoostInternal(self, diff: int, by_effect: 'Effect'):
        self.GainKeyword(diff, 'Boost', by_effect)

    @final
    @property
    def boost_const(self) -> int:
        return self.GetKeyword('Boost')

    @final
    def CountBoostIconsInternal(self) -> int:
        from game.message import Message

        if self.card.area.flags.is_place_card_area:
            return self.printed_boost

        message = Message.WhenBoostIconsWouldBeCount(self)
        message.Send()
        if message.is_be_instead:
            return message.set_boost_icons
        else:
            if self.IsInDeck() or self.card.area.flags.is_attach_boost_area:
                # For "03027"
                value = self.printed_boost
            else:
                # Some boost card will move self after gain amplify, e.g. "45140"
                value = self.GetKeyword('Boost')
            # Fix "15009", cannot decrease lower than 0
            return max(0, value +  message.update_boost_icons)

    @final
    @property
    def boost_star(self) -> int:
        value = 0
        # if self.FindEffect(when=Send.WhenCardBecomeBoost):
        if self.effect.Find(type=AbilityType.Boost):
            value += 1
        return value

    ################################################################################
    #
    @final
    def GainBoostIcons(self, value: int, by_effect: 'Effect'):
        from game.message import Message
        Message.WhenCardWouldGainBoostIcons_Text(self, value, by_effect)
        self.GainKeywordBoostInternal(value, by_effect)
        Message.WhenCardGainBoostIcons_Text(self, value, by_effect)

    @final
    def SetBoostIcon(self, value: int, by_effect: 'Effect'):
        from game.message import Message
        Message.WhenCardWouldGainBoostIcons_Text(self, value, by_effect)
        self.SetKeywords(value, 'Boost', by_effect)
        Message.WhenCardGainBoostIcons_Text(self, value, by_effect)
