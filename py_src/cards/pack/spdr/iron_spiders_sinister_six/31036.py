from . import *

# * Spot

def GetAbilities() -> Sequence['Ability']:

    def spot(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.ShuffleAllTo([this], "EncounterDeck", effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            spot,
            without_excess_damage=True
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay
        ),
    ]

