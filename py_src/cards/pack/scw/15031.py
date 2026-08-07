from . import *

# Recuperation

def GetAbilities() -> Sequence['Ability']:

    def recuperation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.GetAlterEgo().recover
        this.HealthUnits(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            recuperation
        ).SetPlay().SetLabel()
        .SetTarget("YourAlterEgo", canbe_heal=True),
    ]

