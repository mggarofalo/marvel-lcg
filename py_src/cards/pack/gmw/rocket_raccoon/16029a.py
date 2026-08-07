from . import *

# * Rocket Raccoon

def GetAbilities() -> Sequence['Ability']:

    def rocket_raccoon(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)


    return [
        AbilityFactory.AfterUnitDealExcessDamage(
            AbilityType.Response,
            "You",
            rocket_raccoon,
            to_target=Enemy
        ).SetName('"Murdered You!"')
        .SetTarget("Initiator"),
    ]

