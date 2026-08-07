from . import *

# * Machete

def GetAbilities() -> Sequence['Ability']:

    def machete(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.ShuffleAllTo([this], "EncounterDeck", effect)

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            machete
        ),
    ]

