from . import *

# Hydra Soldier

def GetAbilities() -> Sequence['Ability']:

    def hydra_soldier(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        player = this.GetEngagedPlayer()
        player.DealEncounterCards(1, effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            hydra_soldier
        )
    ]

