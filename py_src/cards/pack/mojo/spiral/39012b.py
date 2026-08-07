from . import *

# * Spiral

def GetAbilities() -> Sequence['Ability']:

    def spiral(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        Faces.RemoveCountersOn([this], "All", 'teleport', effect)
        this.card.Flip(effect)

    def spiral_place(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'teleport', effect)

    return [
        AbilityFactory.IfThereAreAtLeastCounterHere(
            "3*",
            'teleport',
            spiral
        ),
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "This",
            spiral_place,
        ),
    ]

