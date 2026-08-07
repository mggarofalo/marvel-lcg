from . import *

# * Man on the Wall

def GetAbilities() -> Sequence['Ability']:

    def man_on_the_wall(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        player = effect.targets[0].GetControlByPlayer()

        size = len(player.GetEngagedMinions())
        Worlds.UpdateNextCardPlayCost(
            player,
            -size,
            effect,
            in_this="Phase"
        )


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(only_if_your_identity_has_trait="SOLDIER"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            man_on_the_wall
        ).SetTarget("YourIdentity")
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]

