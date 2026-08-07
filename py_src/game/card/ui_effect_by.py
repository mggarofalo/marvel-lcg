from core import *

from game.card.face import *
from game.card import *
from game.effect import *

class UIEffectBy:

    class UITypeSimple:
        def __init__(self, card: 'Card') -> None:
            self.card = card
            self.unavailable_by_faces: List['Card'] = []

        def Get(self) -> List['Card']:
            return self.unavailable_by_faces

        def Add(self, *effect: 'Effect'):
            self.unavailable_by_faces += [x.this.card for x in effect]

        def Append(self, card: 'Card'):
            self.unavailable_by_faces.append(card)

        def Remove(self, card: 'Card'):
            self.unavailable_by_faces.remove(card)

        def Reset(self):
            self.unavailable_by_faces = []

    class UIType:
        def __init__(self, card: 'Card') -> None:
            self.card = card
            self.effected_faces: Dict['Effect', List['Card']] = {}

        def Get(self) -> List['Card']:
            card_faces: List['Card'] = []
            for cards in [self.effected_faces[x] for x in self.effected_faces]:
                if cards:
                    card_faces += cards
            return card_faces

        def Add(self, effect: 'Effect', face: 'CardFace'):
            if self.card != face.card:
                if effect in self.effected_faces:
                    self.effected_faces[effect].append(face.card)
                else:
                    self.effected_faces[effect] = [face.card]

        def Reset(self, effect: 'Effect|Literal["All"]'):
            if effect == "All":
                self.effected_faces = {}
            else:
                self.effected_faces[effect] = []

    def __init__(self, card: 'Card') -> None:
        self.card = card
        self.temp = UIEffectBy.UIType(card)
        self.cost = UIEffectBy.UIType(card)

        self.effected_to = UIEffectBy.UITypeSimple(card)
        self.effected_by = UIEffectBy.UITypeSimple(card)

        # `WhenCheckCanBeThwartBy` and `WhenCheckCanBeAttackBy`
        # Only for effects, sender
        self.unavailable = UIEffectBy.UITypeSimple(card)

        self.ResetEffectedBy(False)
        pass

    ################################################################################
    #
    @final
    def GetEffectedByFaces(self) -> List['CardFace']:
        return [x.face for x in self.effected_by.Get()] + \
                [x.face for x in self.unavailable.Get()] + \
                [x.face for x in self.temp.Get()] + \
                [x.face for x in self.cost.Get()]

    @final
    def GetEffectToFaces(self) -> List['CardFace']:
        return [x.face for x in self.effected_to.Get()]

    ################################################################################
    # Guard, Patrol, Trait ...
    # You need to call this function to clear the effect
    @final
    def SetEffectedBy(self, diff: int, effect: 'Effect'):
        if self.card.face != effect.this:
            assert diff != 0
            if diff > 0:
                self.effected_by.Append(effect.this.card)
                effect.this.card.ui.effected_to.Append(self.card)
            else:
                # Fix "04116" as boost cause "04113" effect then select "04116"
                if effect.this.card in self.effected_by.Get():
                    self.effected_by.Remove(effect.this.card)
                if self.card in effect.this.card.ui.effected_to.Get():
                    effect.this.card.ui.effected_to.Remove(self.card)

    @final
    def ResetEffectedBy(self, remove_relate: bool=True):
        self.ResetTempEffectBy("All")
        self.cost.Reset("All")

        if remove_relate:
            for card in self.effected_by.Get():
                card.ui.effected_to.Remove(self.card)

            for card in self.effected_to.Get():
                card.ui.effected_by.Remove(self.card)

        self.effected_to.Reset()
        self.effected_by.Reset()

    ################################################################################
    # Pay Cost, or `WhileValid`
    # System will call `ResetEffectInclude` automatically
    @final
    def SetTempEffectBy(self, effect: 'Effect', face: 'CardFace'):
        self.temp.Add(effect, face)
    @final
    def ResetTempEffectBy(self, effect: 'Effect|Literal["All"]'):
        self.temp.Reset(effect)

