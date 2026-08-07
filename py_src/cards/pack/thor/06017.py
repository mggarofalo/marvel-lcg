from . import *

# * Hall of Heroes

def GetAbilities() -> Sequence['Ability']:

    def hall_of_heroes_put_glory_counter(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Support)
        Faces.PlaceCountersOn([this], 1, "glory", effect)

    def hall_of_heroes(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(3, effect)

    return [
        AbilityFactory.AfterUnitDefeatedUnit(
            AbilityType.Response,
            "You",
            Minion,
            hall_of_heroes_put_glory_counter,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            hall_of_heroes
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 3, 'glory'))
        .SetTarget("Initiator")
    ]
