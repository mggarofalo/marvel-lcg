from . import *

# * Apocalypse

def GetAbilities() -> Sequence['Ability']:

    def apocalypse(effect: 'Effect', message: 'Message.WhenMainSchemeStageCompleted') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        Worlds.SetGameOver(False, effect)

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True
        ),
        AbilityFactory.WhenMainSchemeStageCompleted(
            AbilityType.ForcedInterrupt,
            MainScheme,
            apocalypse
        ),
    ]

