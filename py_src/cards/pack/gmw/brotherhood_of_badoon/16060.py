from . import *

# * Drang

def GetAbilities() -> Sequence['Ability']:

    def drang_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def process(face: 'CardFace'):
            if Minion.IsType(face):
                player = Worlds.FindPlayer(None, effect, engaged_with_fewest_minions=True)
                if player:
                    face.PutIntoPlay(player, effect)

        Worlds.DiscardEncounterCards("4*", effect, each_time=process)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            drang_revealed
        ),
        ResolveBadoonShipChargeUpabilityAfterThisActive()
    ]

