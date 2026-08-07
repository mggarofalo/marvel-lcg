from typing import Final
from core import *
from game.player import *
from game.card.face import *

class PlayerFinder:
    def __init__(self,\
                is_unit: 'CardFinder|None'=None,
                *,
                name: str|None=None,
                non_name: str|None=None,
                trait: "CardFace.TRAITS|None"=None,
                card_type: 'Type[Hero|AlterEgo]|None'=None,
                engaged_with: 'CardFinder|None'=None,
                controls: 'CardFinder|None'=None,
                with_attach: 'CardFinder|None'=None,
    ):
        self.name           : Final = name
        self.non_name       : Final = non_name
        self.trait          : Final = trait
        self.card_type      : Final = card_type
        self.engaged_with   : Final = engaged_with
        self.controls       : Final = controls
        self.with_attach    : Final = with_attach
        self.is_unit        : Final = is_unit

    def Check(self, player: 'Player') -> bool:
        from game.card.card_finder import CardFinder

        if self.name != None and not player.IsName(self.name):
            return False
        if self.non_name != None and player.IsName(self.non_name):
            return False
        if self.trait != None and not player.GetIdentity().HasTrait(self.trait):
            return False
        if self.card_type != None and not self.card_type.IsType(player.GetIdentity()):
            return False
        if self.engaged_with != None and not player.engaged_minions.FindCardSizeOld(self.engaged_with):
            return False
        if self.controls != None and not player.IsControl(self.controls):
            return False
        # TODO: Optimize
        if self.with_attach != None and not CardFinder(with_attach=self.with_attach).Check(player.GetIdentity()):
            return False

        if self.is_unit:
            return self.is_unit.Check(player.GetIdentity())
        return True

