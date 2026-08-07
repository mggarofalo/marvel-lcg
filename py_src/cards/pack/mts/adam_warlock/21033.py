from . import *

# * Soul World

def GetAbilities() -> Sequence['Ability']:

    def soul_world(effect: 'Effect', message: 'Message.AfterDeckRunOut') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'soul', effect)

    def soul_world_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.HealthUnits(effect.targets, "All", effect)

    return [
        AbilityFactory.AfterDeckRunOut(
            AbilityType.Response,
            lambda effect:
                effect.GetInitiator().player_deck,
            soul_world
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            soul_world_action
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'soul'))
        .SetTarget("YourIdentity"),
    ]

