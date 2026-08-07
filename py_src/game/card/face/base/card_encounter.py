from game.card.face import *
from game.player import *

class EncounterCard(CanSurge, CanAccelerationToken, CardFace):
    pass

class EncounterNonVillainCard(HasBoostIcon, CanIncite, HasVictory, HasPeril, EncounterCard):

    def IsNemesis(self, player: 'Player') -> bool:
        if not self.paper.set_name.endswith(" Nemesis"):
            return False
        identity = player.GetIdentity()
        from game.card.face.card_type import Minion
        if Minion.IsType(self):
            if not self.nemesis:
                return False
        # We cannot use this, see "01167" and "27058"
        #     return player.IsName(self.nemesis)
        if identity.paper.set_name == "Spider-Man - Miles Morales":
            return self.paper.set_name[:-8] == "Spider-Man - Morales"
        return identity.paper.set_name == self.paper.set_name[:-8]


