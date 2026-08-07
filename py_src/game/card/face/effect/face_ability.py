from core import *
from game.card.face import *
from game.ability import *
from game.message import *

class FaceAbility:

    def __init__(self, this: 'CardFace') -> None:
        self.face = this
        self.abilities: List[Ability] = []

    ################################################################################
    # Ability
    @final
    def Add(self, *abilities: 'Ability'):
        for ability in abilities:
            # assert ability.paper == None or ability.paper == self.face.paper
            ability.paper = self.face.paper
            self.abilities.append(ability)

    @final
    def Find(self,
            *,
            func_name: "Ability.FUNCTION_NAME|None"=None,
            name: str|None=None,
            type: 'AbilityType|None'=None,
            label: 'Ability.LABEL|None'=None) -> List['Ability']:
        abilities: List['Ability'] = []
        for ability in self.abilities:
            if func_name != None and not ability.IsFunction(func_name):
                continue
            if name != None and ability.name != name:
                continue
            if type != None and not ability.flags.IsType(type):
                continue
            if label != None and not ability.IsLabel(label):
                continue
            abilities.append(ability)
        return abilities

