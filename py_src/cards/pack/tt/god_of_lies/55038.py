from . import *

# * Grendell

def GetAbilities() -> Sequence['Ability']:

    def grendell(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = Worlds.DiscardEncounterCards(3, effect)
        faces = Filter.ByType(faces, Treachery)
        this.GainForThisActive(
            effect,
            message,
            attack=len(faces)
        )


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.ForcedInterrupt,
            "This",
            grendell
        ),
        WhenDefeatedPlaceShatterCountersOnTheAvatarOfLokivillain(3)
    ]

