from . import *

class Component:

    def __init__(self, card: 'Card') -> None:
        self.parent = card
        return

    # TP = TypeVar("TP", bound='Component')
    # def GetComponent(self, component_type: Type[TP]) -> TP:
    #     return self.parent.GetComponent(component_type)

    def OnParentReset(self):
        pass
    def OnParentLeavePlay(self, by_effect: 'Effect'):
        pass
    # def OnWouldEnterPlay(self):
    #     pass
    # def OnWhenEnterPlay(self):
    #     pass
    def OnBeforeParentFlip(self, by_effect: 'Effect', no_detach: bool=False):
        pass
    # def OnFlip(self, by_effect: 'Effect', origin_face: 'CardFace|None'):
    #     pass
    def OnAfterParentFlip(self, by_effect: 'Effect'):
        pass
    def OnRemove(self, by_effect: 'Effect'):
        pass
    # return can be discard in ordeer cards
    def OnDiscard(self, by_effect: 'Effect') -> List['CardFace']:
        return []
    # def OnDefeating(self):
    #     pass

