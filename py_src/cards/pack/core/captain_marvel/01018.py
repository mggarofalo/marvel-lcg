from . import *

# Energy Channel

def GetAbilities() -> Sequence['Ability']:

    def energy_channel(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        value = 2 * this.GetCounters('energy')
        if value > 10:
            value = 10

        this.DealDamage(effect.targets, value, effect)

    def energy_channel_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        value = effect.GetPaidResources().val
        Faces.PlaceCountersOn([this], value, 'energy', effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            energy_channel_action
        ).SetCost(Cost("Y"))
        ,
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            energy_channel
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Enemy)
    ]

