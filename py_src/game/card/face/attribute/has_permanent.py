from . import *

class HasPermanent(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_permanent = False

        super().__init__(paper)

        self.RegisterAttribute("Permanent", "printed_permanent", bool)
        self.RegisterInfoDict('permanent')

    ################################################################################
    #
    @override
    def OnWhenCardLeavePlay(self, message: 'Message.WhenCardLeavePlay') -> bool:
        from game.player import Player
        if self.permanent:
            # Hack
            by_effect = message.by_effect
            if self.paper.set_name in ["Flight", "Super Strength", "Telepathy"]:
                if by_effect.this.paper.card_id == "40139a":
                    pass
            elif self.paper.set_name == "Weather":
                if by_effect.this.paper.set_name == "Storm":
                    pass
            elif self.name == "Power Stone":
                controller = self.GetBindFace().GetControlBy()
                if Player.IsType(controller) and controller.is_eliminated:
                    pass
            elif self.name == "Milano":
                if by_effect.this.name == "The Missing Milano":
                    pass
            elif self.paper.set_name != by_effect.this.paper.set_name:
                return False
        return super().OnWhenCardLeavePlay(message)

    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainPermanent(self.printed_permanent, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def GainPermanent(self, diff: int, by_effect: 'Effect'):
        self.GainKeyword(diff, 'Permanent', by_effect)

    @final
    @property
    def permanent(self) -> bool:
        return self.GetKeyword('Permanent') > 0

