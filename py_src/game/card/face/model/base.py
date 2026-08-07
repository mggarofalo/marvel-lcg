from core import *

class ModelBase:
    def GetThis(self):
        from game.card.face.card_face import CardFace
        return Cast(CardFace, self)

    def OnInitModel(self):
        pass

    def OnResetModel(self):
        pass

