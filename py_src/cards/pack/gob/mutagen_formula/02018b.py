from . import *

# Mutagen Cloud - 2B

def GetAbilities() -> Sequence['Ability']:

    def mutagen_cloud(effect: 'Effect') -> int:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        value = 0
        for unit in Worlds.GetOnFieldEnemies(effect):
            if unit.HasTrait('GOBLIN'):
                value += 1
        return value


    return [
        AbilityFactory.WhenCalcThisSchemeEscalation(
            mutagen_cloud
        ),
    ]

