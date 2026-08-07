from . import *

# Nebula's Ship

def GetAbilities() -> Sequence['Ability']:

    def nebulas_ship(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        value = effect.GetPaidResources().val
        Faces.RemoveCountersOn([this], value, 'evasion', effect)

    def place_evasion(effect: 'Effect', message: 'Message.WhenPhaseBegin') -> None:
        this = effect.this.CastTo(Environment)
        Faces.PlaceCountersOn([this], 1, 'evasion', effect)

    return [
        AbilityFactory.WhenVillainPhaseBegin(
            AbilityType.ForcedInterrupt,
            place_evasion
        ),
        ExhaustTheMilano(
            None,
            nebulas_ship
        ).SetName("Shoot the Thrusters!")
        .SetCost(Cost("2", up_to=True)),
    ]

