from . import *

# * M.O.D.O.K.

def GetAbilities() -> Sequence['Ability']:

    def modok(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Worlds.FindCardOnField(effect, name="Holding Cell")
        if face:
            message.SetBeInstead(effect)
    
            Faces.RemoveCountersOn([face], 2, 'lock', effect)
            this.ResetHealth(effect, 10)
        else:
            Worlds.SetGameOver(True,  effect)


    return [
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedInterrupt,
            "This",
            modok,
        ),
    ]

