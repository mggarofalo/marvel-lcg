from core import *
from game.card.face import *
from game.card.face.model.base import ModelBase

class ModelConsider(ModelBase):

    class ConsiderAs:

        def __init__(self) -> None:
            self.names: Dict[str, List['CardFace']] = {}
            self.traits: Dict['CardFace.TRAITS', List['CardFace']] = {}
            self.card_types: Dict[Type['CardFace'], List['CardFace']] = {}
            # card_classes: Dict['PlayerCard.CardClass', List['CardFace']] = {}

        def __repr__(self) -> str:
            texts = [*[x for x in self.names],
                        *[x for x in self.traits],
                    #  *[x for x in self.card_classes],
                        *[str(x) for x in self.card_types]]
            return ", ".join(texts)

    def OnInitModel(self):
        self.ResetConsiderAs()
        return super().OnInitModel()

    def OnResetModel(self):
        self.ResetConsiderAs()
        return super().OnResetModel()

    @final
    def ResetConsiderAs(self):
        self.consider_as = ModelConsider.ConsiderAs()

    @final
    def IsConsiderAs(self,
                     name: str|None=None,
                     trait: "CardFace.TRAITS|None"=None,
                    #  card_class: "PlayerCard.CardClass|None"=None,
                     card_type: 'Type[TC]|None'=None) -> bool:
        if name != None:
            found = False
            for consider_name in self.consider_as.names:
                if self.GetThis().IsName(consider_name, False):
                    found = True
                    break
            if not found:
                return False
        if trait != None:
            if trait not in self.consider_as.traits:
                return False
        # if card_class != None:
        #     if card_class not in self.consider_as.card_classes:
        #         return False
        if card_type != None:
            found = False
            for consider_type in self.consider_as.card_types:
                if issubclass(consider_type, card_type):
                    found = True
                    break
            if not found:
                return False
        return True

    @final
    def SetConsiderAs(self, face: 'CardFace', diff: int,
                    name: str|None=None,
                    trait: "CardFace.TRAITS|None"=None,
                    # card_class: "PlayerCard.CardClass|None"=None,
                    card_type: 'Type[TC]|None'=None):
        if diff == 1:
            if name:
                if name in self.consider_as.names:
                    assert face not in self.consider_as.names[name]
                    self.consider_as.names[name].append(face)
                else:
                    self.consider_as.names[name] = [face]

            if trait:
                if trait in self.consider_as.traits:
                    assert face not in self.consider_as.traits[trait]
                    self.consider_as.traits[trait].append(face)
                else:
                    self.consider_as.traits[trait] = [face]

            # We disable this
            # card_class = None
            # if card_class:
            #     if card_class in self.card_classes:
            #         assert face not in self.card_classes[card_class]
            #         self.card_classes[card_class].append(face)
            #     else:
            #         self.card_classes[card_class] = [face]

            if card_type:
                if card_type in self.consider_as.card_types:
                    assert face not in self.consider_as.card_types[card_type]
                    self.consider_as.card_types[card_type].append(face)
                else:
                    self.consider_as.card_types[card_type] = [face]

        elif diff == -1:
            if name:
                self.consider_as.names[name].remove(face)
                if self.consider_as.names[name] == []:
                    del self.consider_as.names[name]
            if trait:
                self.consider_as.traits[trait].remove(face)
                if self.consider_as.traits[trait] == []:
                    del self.consider_as.traits[trait]
            # if card_class:
            #     self.consider_as.card_classes[card_class].remove(face)
            #     if self.consider_as.card_classes[card_class] == []:
            #         del self.consider_as.card_classes[card_class]
            if card_type:
                self.consider_as.card_types[card_type].remove(face)
                if self.consider_as.card_types[card_type] == []:
                    del self.consider_as.card_types[card_type]
        else:
            assert False, f"{diff=}"