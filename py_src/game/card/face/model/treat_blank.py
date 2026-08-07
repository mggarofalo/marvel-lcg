from core import *
from game.card.face import *
from game.effect import *
from game.message import *
from game.card.face.model.base import ModelBase
from game.buff import *

class ModelTreatBlank(ModelBase):
    def OnResetModel(self):
        self.ResetTreatBlank()
        return super().OnResetModel()

    @final
    def ResetTreatBlank(self):
        this = self.GetThis()
        this.GetBuff(BuffIsTreatAsIfBlank).Reset()

    ################################################################################
    #
    def OnNotTreatAsIfBlank(self, message: 'Message.WhenCardTreatAsIfBlank') -> None:
        message.Send()

    def OnBeTreatAsIfBlank(self, message: 'Message.WhenCardTreatAsIfBlank') -> None:
        message.Send()

    @final
    def TreatAsIfBlankInternal(self, diff: int, by_effect: 'Effect'):
        from game.message import Message
        this = self.GetThis()

        would_message = Message.WhenCardWouldTreatAsIfBlank(this, by_effect)
        would_message.Send()
        if would_message.is_be_instead:
            return

        if diff == 1:
            this.GetBuff(BuffIsTreatAsIfBlank).OnGain(by_effect)
        else:
            this.GetBuff(BuffIsTreatAsIfBlank).OnLost(by_effect)
        this.card.ui.SetEffectedBy(diff, by_effect)

        message = Message.WhenCardTreatAsIfBlank(this, by_effect)

        if not this.is_treat_as_if_blank:
            this.OnNotTreatAsIfBlank(message)
        else:
            this.OnBeTreatAsIfBlank(message)

    @property
    @final
    def is_treat_as_if_blank(self) -> bool:
        this = self.GetThis()
        return bool(this.GetBuff(BuffIsTreatAsIfBlank))

